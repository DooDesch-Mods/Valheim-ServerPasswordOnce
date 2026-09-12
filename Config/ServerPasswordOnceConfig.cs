using BepInEx.Configuration;

namespace ServerPasswordOnce.Config
{
	/// <summary>
	/// The settings. Bound once in Awake; read through the static entries afterwards. The file lands in
	/// BepInEx/config/DooDesch.ServerPasswordOnce.cfg on the server.
	///
	/// There is no config sync: the mod runs on the server alone and a client never reads these values.
	/// </summary>
	internal static class ServerPasswordOnceConfig
	{
		internal static ConfigEntry<bool> Enabled;

		internal static void Initialize(ConfigFile config)
		{
			if (Enabled != null)
			{
				return;
			}

			Enabled = config.Bind(
				"General",
				"Enabled",
				true,
				"Enable ServerPasswordOnce. When disabled, every player is asked for the password on every " +
				"connect, exactly as without the mod.");
		}
	}
}
