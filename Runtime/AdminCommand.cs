using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ServerPasswordOnce.Config;
using ServerPasswordOnce.Domain;

namespace ServerPasswordOnce.Runtime
{
	/// <summary>
	/// The one console command an admin needs, so a player can be thrown out of the circle without changing
	/// the password for everyone else.
	///
	/// The permission check is the one the game already has. A command registered with `onlyAdmin` and
	/// `remoteCommand` is sent to the server when a client cannot run it itself (`Terminal.TryRunCommand`),
	/// and there the game compares the connection against `adminlist.txt` and answers "You are not admin"
	/// on its own (`ZNet.RPC_RemoteCommand`). Nothing of that has to be built here.
	///
	/// What the game does not do is carry a reply back: it runs the command without the connection, so the
	/// output of `status` and `list` goes to the server log and the server console. The guest list is a text
	/// file next to `adminlist.txt` and can be read there.
	/// </summary>
	internal static class AdminCommand
	{
		private const string Name = "serverpasswordonce";

		private static readonly string[] Subcommands = { "status", "list", "forget", "forgetall", "reload" };

		internal static void Register()
		{
			new Terminal.ConsoleCommand(
				Name,
				"Guest list of ServerPasswordOnce: status | list | forget <id or name> | forgetall | reload",
				Run,
				isCheat: false,
				isNetwork: false,
				onlyServer: true,
				isSecret: false,
				allowInDevBuild: false,
				hideBehindDevCommands: false,
				optionsFetcher: () => Subcommands.ToList(),
				alwaysRefreshTabOptions: false,
				remoteCommand: true,
				onlyAdmin: true);
		}

		private static void Run(Terminal.ConsoleEventArgs args)
		{
			string sub = args.Length > 1 ? args[1].ToLowerInvariant() : "status";

			switch (sub)
			{
				case "status":
					Status(args);
					break;
				case "list":
					List(args);
					break;
				case "forget":
					Forget(args);
					break;
				case "forgetall":
					Say(args, $"{GuestRegistry.ForgetAll()} guest(s) were forgotten. Everyone is asked for the password again.");
					break;
				case "reload":
					GuestRegistry.Load();
					Say(args, $"The guest list was read again: {GuestRegistry.Count} entry(s).");
					break;
				default:
					Say(args, $"Unknown subcommand '{sub}'. Use one of: {string.Join(", ", Subcommands)}.");
					break;
			}
		}

		private static void Status(Terminal.ConsoleEventArgs args)
		{
			Say(args, $"ServerPasswordOnce {DooDesch.ModVersion.Full}");
			Say(args, $"  enabled={ServerPasswordOnceConfig.Enabled.Value} armed={GuestRegistry.Armed} passwordSet={GuestRegistry.PasswordKnown}");
			Say(args, $"  role={(ZNet.instance == null ? "no network" : ZNet.instance.IsServer() ? "server or host" : "client")} backend={ZNet.m_onlineBackend}");
			Say(args, $"  guests={GuestRegistry.Count} file={GuestRegistry.Path}");
			Say(args, $"  forgetAfterDays={ServerPasswordOnceConfig.ForgetAfterDays.Value} maxGuests={ServerPasswordOnceConfig.MaxGuests.Value} logJoins={ServerPasswordOnceConfig.LogJoins.Value}");
			if (ServerPasswordOnceConfig.AllowUntrustedBackends.Value && !GuestRegistry.BackendVerifiesPlayers)
			{
				Say(args, $"  AllowUntrustedBackends is on: nothing verifies the player id on the {ZNet.m_onlineBackend} backend.");
			}
		}

		private static void List(Terminal.ConsoleEventArgs args)
		{
			int shown = 0;
			foreach (KeyValuePair<string, GuestEntry> entry in GuestRegistry.Entries)
			{
				// The fingerprint is shortened on purpose. The whole value says nothing about the password,
				// but there is no reason to spread it across a console either.
				string seen = entry.Value.LastSeenUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
				string print = entry.Value.Fingerprint.Length > 8 ? entry.Value.Fingerprint.Substring(0, 8) : entry.Value.Fingerprint;
				Say(args, $"  {GuestIdentity.Describe(entry.Key, entry.Value.PlatformId)}  {print}...  last seen {seen}");
				shown++;
			}

			if (shown == 0)
			{
				Say(args, "No guest has given the current password yet.");
			}
		}

		private static void Forget(Terminal.ConsoleEventArgs args)
		{
			if (args.Length < 3)
			{
				Say(args, "Give a player id or the name of a connected player.");
				return;
			}

			string wanted = string.Join(" ", args.Args.Skip(2).ToArray());
			List<string> userIds = Resolve(wanted);

			if (userIds.Count == 0)
			{
				Say(args, $"No guest found for '{wanted}'. A name only works while that player is connected; otherwise use an id from the guest list.");
				return;
			}

			foreach (string userId in userIds)
			{
				string name = GuestRegistry.Describe(userId);
				Say(args, GuestRegistry.Forget(userId)
					? $"{name} was removed from the guest list and is asked for the password again."
					: $"{name} is not on the guest list.");
			}
		}

		/// <summary>
		/// Turns an argument into the keys of guest list entries. An id is looked up in the guest list, by
		/// key and by platform id. A name is looked up among the connected players, the same way the game
		/// resolves a ban target.
		///
		/// Used by the ban patch as well, so both take the same route.
		/// </summary>
		internal static List<string> Resolve(string wanted)
		{
			List<string> found = GuestRegistry.Find(wanted);
			if (string.IsNullOrEmpty(wanted) || ZNet.instance == null)
			{
				return found;
			}

			foreach (ZNetPeer peer in ZNet.instance.GetPeers())
			{
				if (peer == null || !string.Equals(peer.m_playerName, wanted, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (GuestIdentity.TryRead(peer.m_socket, out string key, out _) && !found.Contains(key))
				{
					found.Add(key);
				}
			}

			return found;
		}

		/// <summary>
		/// Writes to the console this command runs in and to the log. A command that came from a remote admin
		/// runs in the console of the server, so the log is where the answer can be read.
		/// </summary>
		private static void Say(Terminal.ConsoleEventArgs args, string line)
		{
			args.Context?.AddString(line);
			Core.Log.LogInfo(line);
		}
	}
}
