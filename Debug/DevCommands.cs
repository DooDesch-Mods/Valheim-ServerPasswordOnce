#if DEBUG
using System.Collections.Generic;
using ServerPasswordOnce.Config;
using ServerPasswordOnce.Runtime;

namespace ServerPasswordOnce.Debug
{
	/// <summary>
	/// Developer commands for the console. Debug builds only, so a Release DLL carries none of this.
	/// Console commands instead of hotkeys: the bridge can run a command and read the answer, it cannot
	/// press a key, and a dedicated server has no keyboard at all.
	/// </summary>
	internal static class DevCommands
	{
		internal static void Register()
		{
			new Terminal.ConsoleCommand("serverpasswordoncestatus", "Version, backend and guest list", args =>
			{
				Say(args, $"ServerPasswordOnce {DooDesch.ModVersion.Full}");
				Say(args, $"  enabled={ServerPasswordOnceConfig.Enabled.Value} armed={GuestRegistry.Armed} passwordSet={GuestRegistry.PasswordKnown}");
				Say(args, $"  role={(ZNet.instance == null ? "no network" : ZNet.instance.IsServer() ? "server or host" : "client")} backend={ZNet.m_onlineBackend}");
				Say(args, $"  guests={GuestRegistry.Count} file={GuestRegistry.Path}");
			});

			new Terminal.ConsoleCommand("serverpasswordoncelist", "Lists the remembered guests", args =>
			{
				int shown = 0;
				foreach (KeyValuePair<string, string> entry in GuestRegistry.Entries)
				{
					// The fingerprint is shortened on purpose. The full value says nothing about the password,
					// but there is no reason to spread it across a console either.
					Say(args, $"  {entry.Key}  {entry.Value.Substring(0, 8)}...");
					shown++;
				}
				if (shown == 0)
				{
					Say(args, "No guest has given the current password yet.");
				}
			});

			new Terminal.ConsoleCommand("serverpasswordonceforget", "Forgets one guest, so the password is asked again", args =>
			{
				if (args.Length < 2)
				{
					Say(args, "Give a player id. serverpasswordoncelist prints them.");
					return;
				}
				Say(args, GuestRegistry.Forget(args[1])
					? $"{args[1]} was removed and is asked for the password again."
					: $"{args[1]} is not on the guest list.");
			});

			new Terminal.ConsoleCommand("serverpasswordonceforgetall", "Forgets every guest", args =>
			{
				GuestRegistry.ForgetAll();
				Say(args, "The guest list was cleared. Everyone is asked for the password again.");
			});
		}

		private static void Say(Terminal.ConsoleEventArgs args, string line)
		{
			args.Context.AddString(line);
			Core.Log.LogInfo(line);
		}
	}
}
#endif
