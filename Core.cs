using BepInEx;
using BepInEx.Logging;
using ServerPasswordOnce.Config;
using HarmonyLib;
using ServerSync;

namespace ServerPasswordOnce
{
	[BepInPlugin(PluginGuid, PluginName, DooDesch.ModVersion.Current)]
	public sealed class Core : BaseUnityPlugin
	{
		// The GUID is what BepInEx, the mod managers and ServerSync key on. Letters, digits, dot,
		// underscore and hyphen only - the chainloader rejects anything else. Never change it after the
		// first release.
		public const string PluginGuid = "DooDesch.ServerPasswordOnce";
		public const string PluginName = "ServerPasswordOnce";

		internal static ManualLogSource Log;
		private Harmony _harmony;

		// ServerSync is merged into this DLL (Workspace/build/ServerSync.targets). CurrentVersion is the
		// attribute version, so a 0.0.0 local build and a 1.2.3 server are told apart on connect; with
		// MinimumRequiredVersion set, an older client is disconnected with a readable message.
		internal static readonly ConfigSync ConfigSync = new ConfigSync(PluginGuid)
		{
			DisplayName = PluginName,
			CurrentVersion = DooDesch.ModVersion.Current,
			MinimumRequiredVersion = DooDesch.ModVersion.Current
		};

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
