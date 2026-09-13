using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ServerPasswordOnce.Domain
{
	/// <summary>One guest: the password they gave, and when they were last here.</summary>
	internal sealed class GuestEntry
	{
		internal string Fingerprint;

		/// <summary>
		/// Kept in UTC, so expiry and ordering do not jump when the clock of the server changes for daylight
		/// saving. The file and the admin command show it in the local time of the server.
		/// </summary>
		internal DateTime LastSeenUtc;
	}

	/// <summary>
	/// Who has already given the current password, kept across restarts.
	///
	/// An entry is a platform user id together with a fingerprint of the password that was in force when
	/// they gave it, and the time of their last visit. Change the server password and every fingerprint
	/// stops matching, so everyone is asked once more. That is the whole rule.
	///
	/// The password itself is never written down. The fingerprint is a hash of the password with a salt that
	/// belongs to this file, so the file is useless anywhere else and says nothing about the password. The
	/// hash the game itself keeps cannot be used for this: its salt is drawn fresh on every server start
	/// (`ZNet.ServerPasswordSalt`), so the same password looks different after a restart.
	/// </summary>
	internal sealed class GuestBook
	{
		private const string HeaderName = "ServerPasswordOnce";
		private const int Format = 2;

		private readonly string _path;
		private readonly Dictionary<string, GuestEntry> _entries = new Dictionary<string, GuestEntry>(StringComparer.Ordinal);
		private string _salt = string.Empty;

		internal GuestBook(string path)
		{
			_path = path;
		}

		internal int Count => _entries.Count;
		internal string Path => _path;
		internal IEnumerable<KeyValuePair<string, GuestEntry>> Entries => _entries;

		/// <summary>The fingerprint of a password under the salt of this file.</summary>
		internal string Fingerprint(string password)
		{
			if (string.IsNullOrEmpty(password))
			{
				return string.Empty;
			}

			using (SHA256 sha = SHA256.Create())
			{
				byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(_salt + "\u0000" + password));
				StringBuilder text = new StringBuilder(hash.Length * 2);
				foreach (byte value in hash)
				{
					text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
				}
				return text.ToString();
			}
		}

		internal bool Knows(string userId, string fingerprint)
		{
			if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fingerprint))
			{
				return false;
			}
			return _entries.TryGetValue(userId, out GuestEntry entry) && entry.Fingerprint == fingerprint;
		}

		/// <summary>
		/// Records a visit. The time is written on every visit, not only the first: an expiry has to measure
		/// how long someone has been away, not how old their entry is.
		///
		/// Returns false when nothing changed, so the caller can skip a write.
		/// </summary>
		internal bool Remember(string userId, string fingerprint, DateTime nowUtc, int maxGuests, Action<string> report)
		{
			if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fingerprint))
			{
				return false;
			}

			if (_entries.TryGetValue(userId, out GuestEntry entry))
			{
				// A guest who is already on the list still moves their date, so the file is always written.
				// That is what lets an expiry measure absence instead of the age of the entry.
				entry.Fingerprint = fingerprint;
				entry.LastSeenUtc = nowUtc;
				return true;
			}

			_entries[userId] = new GuestEntry { Fingerprint = fingerprint, LastSeenUtc = nowUtc };
			Trim(maxGuests, report);
			return true;
		}

		internal bool Forget(string userId) => _entries.Remove(userId);

		internal void Clear() => _entries.Clear();

		/// <summary>
		/// Drops the guests who have been away for too long. Returns how many went, so the caller knows
		/// whether the file has to be written.
		/// </summary>
		internal int Expire(int afterDays, DateTime nowUtc, Action<string> report)
		{
			if (afterDays <= 0)
			{
				return 0;
			}

			DateTime limit = nowUtc.AddDays(-afterDays);
			List<string> gone = _entries.Where(pair => pair.Value.LastSeenUtc < limit).Select(pair => pair.Key).ToList();
			foreach (string userId in gone)
			{
				_entries.Remove(userId);
			}

			if (gone.Count > 0)
			{
				report($"{gone.Count} guest(s) were away for more than {afterDays} day(s) and were forgotten. They are asked for the password again.");
			}
			return gone.Count;
		}

		/// <summary>Keeps the list at its limit by dropping whoever was here longest ago.</summary>
		internal void Trim(int maxGuests, Action<string> report)
		{
			if (maxGuests <= 0)
			{
				return;
			}

			while (_entries.Count > maxGuests)
			{
				KeyValuePair<string, GuestEntry> oldest = _entries.OrderBy(pair => pair.Value.LastSeenUtc).First();
				_entries.Remove(oldest.Key);
				report($"The guest list is at its limit of {maxGuests}, so {oldest.Key} was dropped. They are asked for the password again.");
			}
		}

		/// <summary>
		/// Reads the file, or starts a new one. A file that cannot be read is reported and then replaced: the
		/// worst it costs is that everyone is asked for the password once more.
		///
		/// Returns true when the file on disk is not what is now in memory, so the caller writes it back.
		/// </summary>
		internal bool Load(DateTime nowUtc, Action<string> warn)
		{
			_entries.Clear();

			if (!File.Exists(_path))
			{
				_salt = NewSalt();
				return false;
			}

			string[] lines;
			try
			{
				lines = File.ReadAllLines(_path);
			}
			catch (Exception error)
			{
				warn($"The guest list at {_path} could not be read ({error.Message}). Everyone is asked for the password once more.");
				_salt = NewSalt();
				return false;
			}

			if (lines.Length < 2 || !lines[0].StartsWith(HeaderName + " ", StringComparison.Ordinal))
			{
				warn($"The guest list at {_path} is not in a format this version reads. It starts again.");
				_salt = NewSalt();
				return false;
			}

			if (!int.TryParse(lines[0].Substring(HeaderName.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int format) || format < 1 || format > Format)
			{
				warn($"The guest list at {_path} is written in format {lines[0].Substring(HeaderName.Length + 1)}, this version reads up to {Format}. It starts again.");
				_salt = NewSalt();
				return false;
			}

			_salt = lines[1];
			if (string.IsNullOrEmpty(_salt))
			{
				warn($"The guest list at {_path} carries no salt. It starts again.");
				_salt = NewSalt();
				return false;
			}

			int skipped = 0;
			int undated = 0;
			for (int i = 2; i < lines.Length; i++)
			{
				string line = lines[i].Trim();
				if (line.Length == 0 || line[0] == '#')
				{
					continue;
				}

				string[] parts = line.Split(' ');
				if (parts.Length < 2 || parts[0].Length == 0 || parts[1].Length == 0)
				{
					skipped++;
					continue;
				}

				DateTime lastSeen = nowUtc;
				if (parts.Length >= 3)
				{
					if (!DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out lastSeen))
					{
						// A date nobody can read counts as a visit right now. An expiry must not throw
						// somebody out over a line this mod itself failed to write properly.
						warn($"The visit time of {parts[0]} in {_path} could not be read. It counts as a visit now.");
						lastSeen = nowUtc;
					}
					else
					{
						lastSeen = lastSeen.ToUniversalTime();
					}
				}
				else
				{
					undated++;
				}

				_entries[parts[0]] = new GuestEntry { Fingerprint = parts[1], LastSeenUtc = lastSeen };
			}

			if (skipped > 0)
			{
				warn($"{skipped} line(s) in {_path} were not readable and were left out.");
			}

			if (undated > 0)
			{
				warn($"{undated} entry(s) in {_path} come from an older format and carry no visit time. They count as visited now, so an expiry does not throw them out at once.");
			}

			// Anything the reader had to repair is written back, so the file stops being repaired on every start.
			return format != Format || skipped > 0 || undated > 0;
		}

		/// <summary>
		/// Writes the file through a temporary copy, so a crash halfway leaves the old list intact rather
		/// than a half written one.
		/// </summary>
		internal bool Save(Action<string> warn)
		{
			StringBuilder text = new StringBuilder();
			text.Append(HeaderName).Append(' ').Append(Format.ToString(CultureInfo.InvariantCulture)).AppendLine();
			text.AppendLine(_salt);
			text.AppendLine("# One line per guest: player id, the fingerprint of the password they gave, and their last visit.");
			foreach (KeyValuePair<string, GuestEntry> entry in _entries)
			{
				text.Append(entry.Key).Append(' ')
					.Append(entry.Value.Fingerprint).Append(' ')
					.AppendLine(entry.Value.LastSeenUtc.ToLocalTime().ToString("O", CultureInfo.InvariantCulture));
			}

			string temporary = _path + ".new";
			try
			{
				string folder = System.IO.Path.GetDirectoryName(_path);
				if (!string.IsNullOrEmpty(folder))
				{
					Directory.CreateDirectory(folder);
				}

				File.WriteAllText(temporary, text.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				if (File.Exists(_path))
				{
					File.Delete(_path);
				}
				File.Move(temporary, _path);
				return true;
			}
			catch (Exception error)
			{
				warn($"The guest list at {_path} could not be written ({error.Message}). This visit is not remembered.");
				try
				{
					if (File.Exists(temporary))
					{
						File.Delete(temporary);
					}
				}
				catch (Exception cleanup)
				{
					warn($"The half written guest list at {temporary} could not be removed ({cleanup.Message}).");
				}
				return false;
			}
		}

		private static string NewSalt()
		{
			byte[] bytes = new byte[24];
			using (RandomNumberGenerator random = RandomNumberGenerator.Create())
			{
				random.GetBytes(bytes);
			}
			return Convert.ToBase64String(bytes);
		}
	}
}
