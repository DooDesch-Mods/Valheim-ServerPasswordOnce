using BepInEx.Configuration;

namespace ServerPasswordOnce.Config
{
	/// <summary>
	/// The plugin's settings. Bound once in Awake; read through the static entries afterwards.
	/// The file lands in BepInEx/config/DooDesch.ServerPasswordOnce.cfg and ConfigurationManager (F1) edits it live.
	/// </summary>
	internal static class ServerPasswordOnceConfig
	{
		internal static ConfigEntry<bool> ServerConfigLocked;
		internal static ConfigEntry<bool> Enabled;

		internal static void Initialize(ConfigFile config)
		{
			if (Enabled != null)
			{
				return;
			}

			// The locking entry decides whether the server's values overrule the clients'. Registered
			// through AddLockingConfigEntry, once; everything else through AddConfigEntry.
			ServerConfigLocked = config.Bind(
				"General",
				"LockConfiguration",
				true,
				"When enabled on the server, clients use the server's values and cannot change them.");
			Core.ConfigSync.AddLockingConfigEntry(ServerConfigLocked);

			Enabled = config.Bind(
				"General",
				"Enabled",
				true,
				"Enable ServerPasswordOnce. When disabled, the mod does not modify game behavior.");
			Core.ConfigSync.AddConfigEntry(Enabled);
		}
	}
}
