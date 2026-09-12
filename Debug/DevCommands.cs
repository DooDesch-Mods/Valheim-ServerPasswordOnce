#if DEBUG
namespace ServerPasswordOnce.Debug
{
	/// <summary>
	/// Developer commands for the in-game console (F5, the test copy starts with -console). Debug builds
	/// only, so a Release DLL carries none of this. Console commands instead of hotkeys: the MCP bridge can
	/// run a command and read the answer, it cannot press a key.
	///
	/// Terminal.ConsoleCommand registers itself in the game's static command table when constructed;
	/// the table survives Terminal.InitTerminal, so registering in Awake is enough.
	/// </summary>
	internal static class DevCommands
	{
		internal static void Register()
		{
			new Terminal.ConsoleCommand("serverpasswordoncehelp", "Lists the ServerPasswordOnce dev commands", args =>
			{
				args.Context.AddString("serverpasswordoncehelp    this list");
				args.Context.AddString("serverpasswordoncestatus  version and config values");
			});

			new Terminal.ConsoleCommand("serverpasswordoncestatus", "Prints the ServerPasswordOnce version and config", args =>
			{
				// No game version here: the game keeps it on the internal Version class, and this template
				// must also compile against the plain, unpublicized assembly_valheim (PublicizeGameAssembly=false).
				string line = $"ServerPasswordOnce {DooDesch.ModVersion.Full} Enabled={Config.ServerPasswordOnceConfig.Enabled.Value} locked={Config.ServerPasswordOnceConfig.ServerConfigLocked.Value}";
				args.Context.AddString(line);
				Core.Log.LogInfo(line);
			});
		}
	}
}
#endif
