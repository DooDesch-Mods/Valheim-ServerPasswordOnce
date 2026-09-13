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
	/// Identity comes from the socket of the connection, which is what the game itself uses for its ban and
	/// admin lists. On Steam that value comes from the transport and the handshake verifies a session ticket
	/// for it. On the crossplay backend it is a string the client sends and the check of the game accepts
	/// every value, so a remembered guest could be impersonated there. The mod therefore keeps out of the way
	/// on any backend but Steam, unless an admin has turned that off in the Risk section.
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
			Core.Log.LogInfo($"The server password is in force. {_book.Count} guest(s) gave it already.");

			if (ServerPasswordOnceConfig.AllowUntrustedBackends.Value)
			{
				Core.Log.LogWarning("AllowUntrustedBackends is on. On any backend but Steam the player id is a string the client sends and the game accepts it unchecked, so anyone who learns the id of a returning player joins without the password. Turn it off unless you know that is what you want.");
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
		internal static void Remember(string userId)
		{
			if (!Armed || string.IsNullOrEmpty(userId))
			{
				return;
			}

			bool isNew = !_book.Knows(userId, _fingerprint);
			if (!_book.Remember(userId, _fingerprint, DateTime.UtcNow, ServerPasswordOnceConfig.MaxGuests.Value, Warn))
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
				Core.Log.LogInfo($"{userId} gave the password and will not be asked again while it stands.");
			}
		}

		internal static bool Forget(string userId)
		{
			if (_book == null || !_book.Forget(userId))
			{
				return false;
			}
			_book.Save(Warn);
			Core.Log.LogInfo($"{userId} was removed from the guest list and is asked again.");
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
		/// Whether the identity behind a connection is worth trusting on this backend. Reported once, because
		/// an admin who installed this mod on a crossplay server needs to know it does nothing there.
		/// </summary>
		private static bool BackendTrusted()
		{
			if (!ServerPasswordOnceConfig.Enabled.Value)
			{
				return false;
			}

			if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks || ServerPasswordOnceConfig.AllowUntrustedBackends.Value)
			{
				return true;
			}

			if (!_backendReported)
			{
				_backendReported = true;
				Core.Log.LogWarning($"This server runs on the {ZNet.m_onlineBackend} backend, where the player id is a value the client sends and the game accepts without checking. Skipping the password there would let anyone take the place of a returning player, so the password is asked as usual. Risk/AllowUntrustedBackends turns that off.");
			}
			return false;
		}

		private static void Warn(string message) => Core.Log.LogWarning(message);
	}
}
