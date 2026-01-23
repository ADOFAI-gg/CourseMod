#if DEBUG

using CourseMod.DataModel;
using CourseMod.Utils;
using HarmonyLib;

namespace CourseMod.Patches {
	internal static class DebugPatches {
		private static DebugSettings DebugSettings => ModDataStorage.PlayerSettings.DebugSettings;
		
		[HarmonyPatch(typeof(scnGame), "LoadLevel")]
		private static class ShowChecksumInfo {
			private static void Postfix(scnGame __instance) {
				var checksum = ChecksumTools.ComputeFileChecksum(__instance.levelPath)?.Hash ?? "null";
				var gameplayChecksum = ChecksumTools.ComputeGameplayChecksum(__instance.levelData)?.Hash ?? "null";
				LogTools.Log(
					$"File '{__instance.levelPath}'\nFile Checksum: {checksum}\nGameplay Checksum: {gameplayChecksum}");
			}
		}
	}
}

#endif