using System;
using System.Collections.Generic;
using ServerPasswordOnce.Config;
using ServerPasswordOnce.Domain;

namespace ServerPasswordOnce.Runtime
{
	/// <summary>
	/// The decision the whole mod exists for: has this connection already given the password that is in
	/// force right now.
	///
	/// A guest is known by an id that the client cannot choose (`GuestIdentity`): the Steam id on a Steam
	/// server, the PlayFab player id on a crossplay server. Every other backend has no such id, so the mod
	/// keeps out of the way there, unless an admin has turned that off in the Risk section.
	/// </summary>
	internal static class GuestRegistry
	{
		private static GuestBook _book;
		private static string _fingerprint = string.Empty;
		private static bool _backendReported;

		/// <summary>True when the mod may skip the password for a known guest.</summary>
		internal static bool Armed => _book != null && _fingerprint.Length > 0 && BackendTrusted();

		internal static int Count => _book?.Count ?? 0;
		internal static string Path => _book?.Path ?? "(no file)";
		internal static bool PasswordKnown => _fingerprint.Length > 0;

		/// <summary>True on a backend where the connection carries an id the client cannot choose.</summary>
		internal static bool BackendVerifiesPlayers
			=> ZNet.m_onlineBackend == OnlineBackendType.Steamworks || ZNet.m_onlineBackend == OnlineBackendType.PlayFab;

		internal static IEnumerable<KeyValuePair<string, GuestEntry>> Entries
			=> _book != null ? _book.Entries : new Dictionary<string, GuestEntry>();

		/// <summary>
		/// Reads the guest list, drops whoever has been away too long, and writes the file back when the
		/// reader had to repair anything. Used at start and by the reload command.
		/// </summary>
		internal static void Load()
		{
			string folder = Utils.GetSaveDataPath(FileHelpers.FileSource.Local);
			_book = new GuestBook(System.IO.Path.Combine(folder, "serverpasswordonce.txt"));

			DateTime now = DateTime.UtcNow;
			bool rewrite = _book.Load(now, Warn);
			rewrite |= _book.Expire(ServerPasswordOnceConfig.ForgetAfterDays.Value, now, Warn) > 0;

			// The limit is applied here too. Otherwise lowering it would sit idle until the next player
			// happened to join, which is not what an admin means by setting it.
			int before = _book.Count;
			_book.Trim(ServerPasswordOnceConfig.MaxGuests.Value, Warn);
			rewrite |= _book.Count != before;

			if (rewrite)
			{
				_book.Save(Warn);
			}

			Core.Log.LogInfo($"The guest list holds {_book.Count} entry(s): {_book.Path}");
		}

		/// <summary>
		/// Takes the password the server was started with. The game hashes it at once with a salt it draws
		/// fresh on every start, so the plain value has to be caught here or the fingerprint could not
		/// survive a restart.
		/// </summary>
		internal static void UsePassword(string password)
		{
			if (_book == null)
			{
				Load();
			}

			if (string.IsNullOrEmpty(password))
			{
				_fingerprint = string.Empty;
				Core.Log.LogInfo("This server runs without a password, so there is nothing to remember.");
				return;
			}

			_fingerprint = _book.Fingerprint(password);

			// The start is where an admin looks first. A server that never skips the password must say so
			// here, not only when the first player joins.
			if (!ServerPasswordOnceConfig.Enabled.Value)
			{
				Core.Log.LogWarning("ServerPasswordOnce is not active: General/Enabled is off. Every player is asked for the password on every join.");
				return;
			}

			if (!BackendTrusted())
			{
				return;
			}

			Core.Log.LogInfo($"The server password is in force. {_book.Count} guest(s) gave it already.");

			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				// The ids in the log and in the guest list look different on a crossplay server. An admin who
				// compares them with adminlist.txt needs to know which one is which.
				Core.Log.LogInfo("This is a crossplay server. A guest is identified by the PlayFab player id of the connection; the platform id is stored with it for the admin command.");
			}

			if (!BackendVerifiesPlayers)
			{
				Core.Log.LogWarning($"AllowUntrustedBackends is on and this server runs on the {ZNet.m_onlineBackend} backend. There the player id is a value the client sends and nothing verifies it, so anyone who learns the id of a returning player joins without the password. Turn it off unless you know that is what you want.");
			}
		}

		internal static bool Knows(string userId)
		{
			if (!Armed)
			{
				return false;
			}
			return _book.Knows(userId, _fingerprint);
		}

		/// <summary>Records a guest who has just been let in.</summary>
		internal static void Remember(string userId, string platformId)
		{
			if (!Armed || string.IsNullOrEmpty(userId))
			{
				return;
			}

			bool isNew = !_book.Knows(userId, _fingerprint);
			if (!_book.Remember(userId, platformId, _fingerprint, DateTime.UtcNow, ServerPasswordOnceConfig.MaxGuests.Value, Warn))
			{
				return;
			}

			if (!_book.Save(Warn))
			{
				return;
			}

			// The first time somebody is added is worth a line whatever LogJoins says: it is the moment the
			// mod took on responsibility for that player.
			if (isNew)
			{
				Core.Log.LogInfo($"{GuestIdentity.Describe(userId, platformId)} gave the password and will not be asked again while it stands.");
			}
		}

		/// <summary>The keys of the entries an id stands for. See `GuestBook.Find`.</summary>
		internal static List<string> Find(string wanted)
			=> _book != null ? _book.Find(wanted) : new List<string>();

		/// <summary>The name of an entry for a log line or an answer of the admin command.</summary>
		internal static string Describe(string userId)
			=> GuestIdentity.Describe(userId, _book?.PlatformIdOf(userId));

		internal static bool Forget(string userId)
		{
			if (_book == null)
			{
				return false;
			}

			string name = Describe(userId);
			if (!_book.Forget(userId))
			{
				return false;
			}
			_book.Save(Warn);
			Core.Log.LogInfo($"{name} was removed from the guest list and is asked again.");
			return true;
		}

		internal static int ForgetAll()
		{
			if (_book == null)
			{
				return 0;
			}
			int count = _book.Count;
			_book.Clear();
			_book.Save(Warn);
			Core.Log.LogInfo($"The guest list was cleared, {count} entry(s) removed. Everyone is asked again.");
			return count;
		}

		/// <summary>
		/// Whether the identity behind a connection is worth trusting on this backend. Reported once, at the
		/// start of the server, because an admin who installed this mod needs to know when it does nothing.
		/// </summary>
		private static bool BackendTrusted()
		{
			if (!ServerPasswordOnceConfig.Enabled.Value)
			{
				return false;
			}

			if (BackendVerifiesPlayers || ServerPasswordOnceConfig.AllowUntrustedBackends.Value)
			{
				return true;
			}

			if (!_backendReported)
			{
				_backendReported = true;
				Core.Log.LogWarning($"ServerPasswordOnce is not active: this server runs on the {ZNet.m_onlineBackend} backend. There the player id is a value the client sends and nothing verifies it. To skip the password would let anyone take the place of a returning player, so every player is asked for the password on every join. Risk/AllowUntrustedBackends in the config skips the password without that check.");
			}
			return false;
		}

		private static void Warn(string message) => Core.Log.LogWarning(message);
	}
}
