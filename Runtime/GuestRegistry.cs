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
	/// every value, so a remembered guest could be impersonated there. The mod therefore does nothing on any
	/// backend but Steam, and says so once.
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

		internal static void Load()
		{
			string folder = Utils.GetSaveDataPath(FileHelpers.FileSource.Local);
			_book = new GuestBook(System.IO.Path.Combine(folder, "serverpasswordonce.txt"));
			_book.Load(message => Core.Log.LogWarning(message));
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
		}

		internal static bool Knows(string userId)
		{
			if (!Armed)
			{
				return false;
			}
			return _book.Knows(userId, _fingerprint);
		}

		/// <summary>Records a guest who has just been let in. Quiet when the guest was already on the list.</summary>
		internal static void Remember(string userId)
		{
			if (!Armed || string.IsNullOrEmpty(userId))
			{
				return;
			}

			if (!_book.Remember(userId, _fingerprint))
			{
				return;
			}

			if (_book.Save(message => Core.Log.LogWarning(message)))
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
			_book.Save(message => Core.Log.LogWarning(message));
			Core.Log.LogInfo($"{userId} was removed from the guest list and is asked again.");
			return true;
		}

		internal static void ForgetAll()
		{
			if (_book == null)
			{
				return;
			}
			int count = _book.Count;
			_book.Clear();
			_book.Save(message => Core.Log.LogWarning(message));
			Core.Log.LogInfo($"The guest list was cleared, {count} entry(s) removed. Everyone is asked again.");
		}

		internal static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> Entries
			=> _book != null ? _book.Entries : new System.Collections.Generic.Dictionary<string, string>();

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

			if (ZNet.m_onlineBackend == OnlineBackendType.Steamworks)
			{
				return true;
			}

			if (!_backendReported)
			{
				_backendReported = true;
				Core.Log.LogWarning($"This server runs on the {ZNet.m_onlineBackend} backend, where the player id is a value the client sends and the game accepts without checking. Skipping the password there would let anyone take the place of a returning player, so the password is asked as usual.");
			}
			return false;
		}
	}
}
