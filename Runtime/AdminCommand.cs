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
			if (ServerPasswordOnceConfig.AllowUntrustedBackends.Value)
			{
				Say(args, "  AllowUntrustedBackends is on: on a backend other than Steam the player id is not checked by the game.");
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
				Say(args, $"  {entry.Key}  {entry.Value.Fingerprint.Substring(0, 8)}...  last seen {seen}");
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
			string userId = Resolve(wanted);

			if (userId == null)
			{
				Say(args, $"No player id found for '{wanted}'. A name only works while that player is connected; otherwise use the id from the guest list.");
				return;
			}

			Say(args, GuestRegistry.Forget(userId)
				? $"{userId} was removed from the guest list and is asked for the password again."
				: $"{userId} is not on the guest list.");
		}

		/// <summary>
		/// Turns an argument into a player id. A run of digits is taken as an id; anything else is looked up
		/// among the connected players by name, the same way the game resolves a ban target.
		///
		/// Used by the ban patch as well, so both take the same route.
		/// </summary>
		internal static string Resolve(string wanted)
		{
			if (string.IsNullOrEmpty(wanted))
			{
				return null;
			}

			if (wanted.All(char.IsDigit))
			{
				return wanted;
			}

			// An id with its platform in front, the form adminlist.txt uses.
			int underscore = wanted.LastIndexOf('_');
			if (underscore > 0 && underscore + 1 < wanted.Length && wanted.Substring(underscore + 1).All(char.IsDigit))
			{
				return wanted.Substring(underscore + 1);
			}

			if (ZNet.instance == null)
			{
				return null;
			}

			foreach (ZNetPeer peer in ZNet.instance.GetPeers())
			{
				if (peer == null || peer.m_socket == null)
				{
					continue;
				}
				if (string.Equals(peer.m_playerName, wanted, StringComparison.OrdinalIgnoreCase))
				{
					return peer.m_socket.GetHostName();
				}
			}

			return null;
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
