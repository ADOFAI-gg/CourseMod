using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace CourseMod.Editor {
	public class GameImporter {
		private readonly string _packagePath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "adofai");

		public string GamePath;
		public string GameExecutable;
		public string ModdingRoot;
		public string DataPath;

		private const string GameImportScheduledKey = "ADOFAI_ModdingToolkit:ImportScheduled";

		private static readonly string[] BlacklistedAssemblyNames = {
			"BepInEx.Harmony.dll", "RuntimeUnityEditor.Core.dll", "RuntimeUnityEditor.Bepin5.dll"
		};

		static GameImporter() {
			EditorApplication.delayCall += () => {
				var gameSettings = ProjectToolsConfig.Config;
				if (EditorPrefs.GetBool(GameImportScheduledKey)) {
					gameSettings.Importer.SetGamePath(gameSettings.adofaiPath);
					gameSettings.Importer.Import();
				}
			};
		}

		public static bool ValidateGamePath(string gamePath) {
			var filename = Path.GetFileName(gamePath);
#if UNITY_EDITOR_OSX
            if (filename == "ADanceOfFireAndIce.app" && Directory.Exists(gamePath))
            {
                return true;
            }
#elif UNITY_EDITOR_LINUX
			if (filename == "ADanceOfFireAndIce" && File.Exists(gamePath)) {
				return true;
			}
#else
            if (filename == "A Dance of Fire and Ice.exe" && File.Exists(gamePath))
            {
                return true;
            }
#endif
			EditorUtility.DisplayDialog("Invalid file", "Please select valid adofai executable file", "OK");

			return false;
		}

		[UsedImplicitly]
		public void SetGamePath(string gamePath) {
			if (!ValidateGamePath(gamePath)) {
				throw new ArgumentException("Invalid game path");
			}

#if UNITY_EDITOR_OSX
            GamePath = gamePath;
            GameExecutable = gamePath;
            ModdingRoot = Path.GetDirectoryName(gamePath);
            DataPath = Path.Combine(gamePath, "Contents", "Resources", "Data");
#elif UNITY_EDITOR_LINUX
			GamePath = Path.GetDirectoryName(gamePath)!;
			GameExecutable = gamePath;
			ModdingRoot = GamePath;
			DataPath = Path.Combine(GamePath, "ADanceOfFireAndIce_Data");
#else
            GamePath = Path.GetDirectoryName(gamePath)!;
            GameExecutable = gamePath;
            ModdingRoot = GamePath;
            DataPath = Path.Combine(GamePath, "A Dance of Fire and Ice_Data");
#endif
		}

		public void Import() {
			var scheduled = EditorPrefs.GetBool(GameImportScheduledKey);
			EditorPrefs.SetBool(GameImportScheduledKey, false);
			if (Directory.Exists(_packagePath) && !scheduled) {
				if (EditorUtility.DisplayDialog("Package already exists",
					    "The package is already created. You should restart the editor after deleting the package to properly load it. Do you want to restart unity editor?",
					    "OK",
					    "Cancel")) {
					Directory.Delete(_packagePath, true);
					EditorPrefs.SetBool(GameImportScheduledKey, true);
					AssetDatabase.SaveAssets();

					// restart editor
					EditorApplication.OpenProject(Directory.GetCurrentDirectory());
					return;
				}
			}

			CreatePackage();
			CopyAssemblies();
			AssetDatabase.Refresh();

			Debug.Log("");
		}

		private void CreatePackage() {
			Directory.CreateDirectory(_packagePath);
			File.WriteAllText(Path.Combine(_packagePath, "package.json"),
				EditorJsonUtility.ToJson(
					new PackageManifest { name = "adofai", version = "0.0.1", displayName = "A Dance of Fire and Ice" },
					true));
		}

		private static readonly string[] PluginDirectoryNames = { "AnyCPU", "x86_64" };
		private static readonly string[] UfbLibraryFilenames = { "libufb.so", "ufb.dll" };
		private void CopyAssemblies() {
			var assemblyPaths = new List<string>();
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
				try {
					assemblyPaths.Add(asm.Location);
				} catch (Exception) {
					// ignored
				}
			}

			var assemblyNames = (from path in assemblyPaths select Path.GetFileName(path)).ToList();
			var managedDir = Path.Combine(DataPath, "Managed");
			var bepInExDir = Path.Combine(ModdingRoot, "BepInEx");

			var dirs = new[] { managedDir, bepInExDir };

			foreach (var dir in dirs) {
				if (!Directory.Exists(dir)) continue;

				var files = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories);
				foreach (var file in files) {
					CopyFile(file, assemblyNames);
				}
			}

			var pluginsDir = Path.Combine(DataPath, "Plugins");
			foreach (var pluginsNestedDir in PluginDirectoryNames) {
				var dir = Path.Combine(pluginsDir, pluginsNestedDir);
				if (!Directory.Exists(dir)) continue;

				var files = Directory.GetFiles(dir, "*.dll");
				foreach (var file in files) {
					if (UfbLibraryFilenames.Contains(Path.GetFileName(file)))
						CopyFile(file, assemblyNames);
				}
			}
		}

		private void CopyFile(string file, List<string> assemblyNames)
		{
			var name = Path.GetFileName(file);
			if (assemblyNames.Contains(name)) return;
			if (BlacklistedAssemblyNames.Contains(name)) return;

			var dest = Path.Combine(_packagePath, name);
			File.Copy(file, dest, true);
			File.WriteAllText(dest + ".meta", GetMetaFileContent(file));
		}

		private static string GetMetaFileContent(string filename)
		{
			StringBuilder b = new($@"
fileFormatVersion:
    guid: {GUID.Generate()}
PluginImporter:
    isExplicitlyReferenced: 1
    validateReferences: 1");

			if (filename.Contains("UniTask"))
			{
				b.Append(@"
	platformData:
	    Any:
	      enabled: 0
	      settings:
	        Exclude Android: 1
	        Exclude Editor: 1
	        Exclude Linux64: 1
	        Exclude OSXUniversal: 1
	        Exclude Win: 1
	        Exclude Win64: 1
	        Exclude iOS: 1");
			}

			return b.ToString();
		}
	}
}