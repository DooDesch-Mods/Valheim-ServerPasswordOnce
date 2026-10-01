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
		internal static ConfigEntry<bool> LogJoins;
		internal static ConfigEntry<int> ForgetAfterDays;
		internal static ConfigEntry<int> MaxGuests;
		internal static ConfigEntry<bool> AllowUntrustedBackends;

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

			LogJoins = config.Bind(
				"General",
				"LogJoins",
				true,
				"Write a line for every player who joins without the password window. The first time a player " +
				"is added to the guest list is always written, whatever this is set to.");

			ForgetAfterDays = config.Bind(
				"Guests",
				"ForgetAfterDays",
				0,
				"Days without a visit until a guest is forgotten and asked for the password again. 0 keeps " +
				"them for as long as the password stands.");

			MaxGuests = config.Bind(
				"Guests",
				"MaxGuests",
				0,
				"Largest number of guests to keep. Over that, whoever visited longest ago is dropped and asked " +
				"again. 0 sets no limit.");

			// The section is called Risk and not General on purpose. An admin who flips this should have read
			// what it gives away, and the description is the last place to say it.
			AllowUntrustedBackends = config.Bind(
				"Risk",
				"AllowUntrustedBackends",
				false,
				"Skip the password on backends other than Steam and crossplay (PlayFab) as well. Not " +
				"recommended: there the player id is a value the client sends and nothing verifies it, so " +
				"anyone who learns the id of a returning player joins without the password. This setting has " +
				"no effect on a Steam server or a crossplay server, where the id comes from the connection.");
		}
	}
}
