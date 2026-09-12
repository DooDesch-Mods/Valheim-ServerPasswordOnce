using HarmonyLib;

namespace ServerPasswordOnce.Patches
{
	/// <summary>
	/// Example Harmony patch. Replace it with real patches.
	///
	/// Find the class and method in Workspace/decompiled/assembly_valheim (the game code), then
	/// [HarmonyPatch(typeof(Class), nameof(Class.Method))] with a Prefix (return false replaces the
	/// original) or a Postfix (runs after it). __instance is the object, __result the return value.
	/// assembly_valheim is publicized by the build, so private members are reachable by name.
	/// </summary>
	// [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
	// internal static class ExamplePatch
	// {
	//     private static void Postfix(Player __instance)
	//     {
	//         if (!Config.ServerPasswordOnceConfig.Enabled.Value)
	//         {
	//             return;
	//         }
	//         Core.Log.LogInfo($"{__instance.GetPlayerName()} spawned");
	//     }
	// }
}
