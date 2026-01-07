using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace CourseMod.Editor {
	public class ProjectToolsConfig : ScriptableObject {
		private static ProjectToolsConfig _config;

		public static ProjectToolsConfig Config {
			get {
				if (_config)
					return _config;
				_config = AssetDatabase.LoadAssetAtPath<ProjectToolsConfig>("Assets/Editor/Config.asset");
				if (_config) return _config;

				_config = CreateInstance<ProjectToolsConfig>();
				AssetDatabase.CreateAsset(_config, "Assets/Editor/Config.asset");

				return _config;
			}
		}

		public string adofaiPath;
		public bool openModInfoFoldout;
		public bool skipAssetBundleBuild;
		public bool createZip;
		public bool developmentBuild = true;
		public bool generateDebugSymbols;
		public bool buildEveryPlatform;
		public bool copyToDirectory;
		public bool runApplication;
		public bool runApplicationThroughSteam;
		public int deleteBuildsExceptLastN;
		public bool automaticallyDeleteBuilds;

		public readonly GameImporter Importer = new();
		public readonly ModBuilder ModBuilder = new();

		public void BuildMod(string copyDestination) {
			ModBuilder.SkipAssetBundleBuild = skipAssetBundleBuild;
			ModBuilder.DevelopmentBuild = developmentBuild;
			ModBuilder.GenerateDebugSymbols = generateDebugSymbols;

			ModBuilder.Build(copyDestination, buildEveryPlatform)
				.ContinueWith(task => {
					if (createZip) {
						using var stream =
							new FileStream(Path.Combine(Path.GetDirectoryName(task.Result)!, ModInfo.Info.Id + ".zip"),
								FileMode.Create);
						using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

						foreach (var file in Directory.GetFiles(task.Result, "*", SearchOption.AllDirectories)) {
							archive.CreateEntryFromFile(file,
								Path.Combine(ModInfo.Info.Id, Path.GetRelativePath(task.Result, file)));
						}
					}

					if (automaticallyDeleteBuilds)
						DeleteBuilds(1);

					if (runApplication) {
						if (runApplicationThroughSteam) {
							Process.Start("steam://rungameid/977950");
						} else {
							Process.Start(adofaiPath);
						}
					}
				});
		}

		public void DeleteBuilds(int? saveLeast = null) {
			var buildDir = ProjectToolsWindow.BuildDirectory;

			if (Directory.Exists(buildDir)) {
				var except = deleteBuildsExceptLastN;

				if (except == 0) {
					Directory.Delete(buildDir, true);
					Directory.CreateDirectory(buildDir);

					var zipPath = Path.Combine(buildDir, ModInfo.Info.Id + ".zip");

					if (File.Exists(zipPath))
						File.Delete(zipPath);
				} else {
					var buildDirectories = new DirectoryInfo(buildDir).GetDirectories()
						.OrderByDescending(d => d.CreationTimeUtc)
						.ToList();

					for (var i = Math.Max(saveLeast ?? 0, Math.Max(0, except)); i < buildDirectories.Count; i++) {
						buildDirectories[i].Delete(true);
					}
				}
			}
		}
	}
}