using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ServerPasswordOnce.Config;

namespace ServerPasswordOnce
{
	/// <summary>
	/// A server only mod. Players install nothing, and a player without it notices nothing except that the
	/// password window stops appearing once they have given the password.
	///
	/// There is deliberately no config sync and no version demand on connecting clients. A demand of that
	/// kind would disconnect exactly the vanilla players this mod is meant to wave through.
	/// </summary>
	[BepInPlugin(PluginGuid, PluginName, DooDesch.ModVersion.Current)]
	public sealed class Core : BaseUnityPlugin
	{
		// The GUID is what BepInEx and the mod managers key on. Letters, digits, dot, underscore and hyphen
		// only - the chainloader rejects anything else. Never change it after the first release.
		public const string PluginGuid = "DooDesch.ServerPasswordOnce";
		public const string PluginName = "ServerPasswordOnce";

		internal static ManualLogSource Log;
		private Harmony _harmony;

		private void Awake()
		{
			Log = Logger;
			ServerPasswordOnceConfig.Initialize(Config);

			// Unlike MelonLoader, BepInEx applies nothing on its own: this PatchAll is the one and only
			// place patches are applied. Do not add a second one in a patch class.
			_harmony = new Harmony(PluginGuid);
			_harmony.PatchAll(typeof(Core).Assembly);

#if DEBUG
			Debug.DevCommands.Register();
#endif

			Log.LogInfo($"{PluginName} {DooDesch.ModVersion.Full} initialized. Enabled={ServerPasswordOnceConfig.Enabled.Value}");
		}

		private void OnDestroy()
		{
			_harmony?.UnpatchSelf();
		}
	}
}
