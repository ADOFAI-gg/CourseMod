using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourseMod.Editor {
	public class ProjectToolsWindow : EditorWindow {
		private enum Tab {
			Build,
			Shortcut,
			Test
		}

		private const string RepositoryLink = "https://src.afg.ink/modding/course-mod"; // disabled if null
		private const string I18NLink = null; // disabled if null

		public static string BuildDirectory => Path.Combine(Directory.GetCurrentDirectory(), "Builds");

		private static readonly Color BrandColor;
		private static readonly Color OpenLinkColor;
		private static readonly Color OpenLinkSubColor;
		private static readonly Color OrangeColor;
		private static readonly Color PinkColor;
		private static readonly Color SkyBlueColor;
		private static readonly Color OpenAssetColor;

		private Tab _currentTab = Tab.Build;
		private static readonly IEnumerable<Tab> AllTabs = Enum.GetValues(typeof(Tab)).Cast<Tab>();

		private bool _firstRender;
		private SerializedObject _modInfo;
		private readonly Dictionary<FieldInfo, SerializedProperty> _modInfoSps = new();

		private static readonly FieldInfo[] ModInfoFields = typeof(ModInfo).GetFields();

		private static FileSystemWatcher _scenesDirectoryWatcher;
		private static FileSystemWatcher _assemblyDefinitionsDirectoryWatcher;
		private static FileSystemWatcher _fluentDirectoryWatcher;
		private static FileSystemWatcher _buildDirectoryWatcher;

		private static readonly HashSet<string> Scenes = new();
		private static readonly HashSet<string> AssemblyDefinitions = new();
		private static readonly Dictionary<string, string> AssemblyDefinitionRelativePaths = new();
		private static readonly HashSet<string> FluentFiles = new();
		private static readonly HashSet<string> BuildFiles = new();

		private static long _buildSize;
		private static string _buildSizeString;
		private static int _buildCount;

		static ProjectToolsWindow() {
			ColorUtility.TryParseHtmlString("#0b192b", out BrandColor);
			OpenLinkColor = BrandColor * 14;
			OpenLinkSubColor = BrandColor * 10;

			ColorUtility.TryParseHtmlString("#ff8000", out OrangeColor);
			ColorUtility.TryParseHtmlString("#ff00bb", out PinkColor);
			ColorUtility.TryParseHtmlString("#00aaff", out SkyBlueColor);

			ColorUtility.TryParseHtmlString("#a7a7ff", out OpenAssetColor);
		}

		[MenuItem("Modding/Project Tools")]
		private static void ShowWindow() {
			var window = GetWindow<ProjectToolsWindow>();

			window.titleContent.text = "Project Tools";
			window.titleContent.tooltip = "The tool that builds your mod.";

			window._firstRender = true;

			window.SetupDirectoryWatchers();
		}

		private void OnEnable() {
			if (_firstRender) return;

			SetupDirectoryWatchers();

			ReleaseResources();

			_modInfo = new SerializedObject(ModInfo.Info);

			foreach (var field in ModInfoFields)
				_modInfoSps[field] = _modInfo.FindProperty(field.Name);
		}

		private void SetupDirectoryWatchers() {
			ReleaseStaticResources();
			ReloadFileCache();

			_scenesDirectoryWatcher =
				CreateWatcherForFile(Path.Combine(Application.dataPath, "Scenes"), "*.unity", false);

			_assemblyDefinitionsDirectoryWatcher =
				CreateWatcherForFile(Application.dataPath, "*.asmdef", true);

			_fluentDirectoryWatcher =
				CreateWatcherForFile(Path.Combine(Application.dataPath, "Resources"), "*.ftl", false);

			_buildDirectoryWatcher =
				CreateWatcherForDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Builds"), "*", false);

			return;

			FileSystemWatcher CreateWatcherForFile(string path, string filter, bool includeSubdirectories)
				=> CreateWatcherInternal(path, filter, includeSubdirectories,
					NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime);

			FileSystemWatcher CreateWatcherForDirectory(string path, string filter, bool includeSubdirectories)
				=> CreateWatcherInternal(path, filter, includeSubdirectories,
					NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime);

			FileSystemWatcher CreateWatcherInternal(string path, string filter, bool includeSubdirectories,
				NotifyFilters notifyFilter) {
				var watcher = new FileSystemWatcher(path, filter) {
					EnableRaisingEvents = true,
					IncludeSubdirectories = includeSubdirectories,
					NotifyFilter = notifyFilter
				};
				watcher.Created += OnWatcherUpdate;
				watcher.Deleted += OnWatcherUpdate;
				watcher.Renamed += OnWatcherUpdate;
				return watcher;
			}
		}

		private void OnWatcherUpdate(object sender, FileSystemEventArgs e) {
			try {
				if (sender is not FileSystemWatcher watcher) return;

				var set = 0 switch {
					_ when watcher == _scenesDirectoryWatcher => Scenes,
					_ when watcher == _assemblyDefinitionsDirectoryWatcher => AssemblyDefinitions,
					_ when watcher == _fluentDirectoryWatcher => FluentFiles,
					_ when watcher == _buildDirectoryWatcher => BuildFiles,
					_ => throw new InvalidOperationException("Unregistered watcher triggered the event."),
				};
				var relativePathRecord = 0 switch {
					_ when watcher == _assemblyDefinitionsDirectoryWatcher => AssemblyDefinitionRelativePaths,
					_ => null,
				};

				switch (e.ChangeType) {
					case WatcherChangeTypes.Created:
						UpdateCache(set, add: e.FullPath, relativePathRecordToUpdate: relativePathRecord);
						break;

					case WatcherChangeTypes.Renamed when e is RenamedEventArgs re:
						UpdateCache(set, add: re.FullPath, remove: re.OldFullPath,
							relativePathRecordToUpdate: relativePathRecord);
						break;

					case WatcherChangeTypes.Deleted:
						UpdateCache(set, remove: e.FullPath, relativePathRecordToUpdate: relativePathRecord);
						break;
				}

				if (watcher == _buildDirectoryWatcher) {
					ReloadBuildCache();
				}
			} catch (Exception exception) {
				Debug.Log(exception);
			}
		}

		private static void UpdateCache(
			HashSet<string> setToUpdate,
			[CanBeNull] string add = null,
			[CanBeNull] string remove = null,
			[CanBeNull] Dictionary<string, string> relativePathRecordToUpdate = null) {
			if (add != null) {
				var processedPath = ProcessPath.Invoke(add);
				setToUpdate.Add(processedPath);

				if (relativePathRecordToUpdate != null)
					relativePathRecordToUpdate[processedPath] = ProcessRelativePath.Invoke(add);
			}

			if (remove != null) {
				setToUpdate.Remove(ProcessPath.Invoke(remove));
				relativePathRecordToUpdate?.Remove(ProcessRelativePath.Invoke(remove));
			}
		}

		private static void ReloadFileCache() {
			Scenes.UnionWith(Directory.GetFiles(Path.Combine(Application.dataPath, "Scenes"),
					"*.unity", SearchOption.TopDirectoryOnly)
				.Select(ProcessPath));

			var assemblyDefinitionPaths = Directory.GetFiles(Application.dataPath,
				"*.asmdef", SearchOption.AllDirectories);
			var assemblyDefinitionRelativePaths = assemblyDefinitionPaths
				.ToDictionary(ProcessPath, ProcessRelativePath);

			AssemblyDefinitions.UnionWith(assemblyDefinitionRelativePaths.Keys.ToArray());

			foreach (var v in assemblyDefinitionRelativePaths)
				AssemblyDefinitionRelativePaths.Add(v.Key, v.Value);

			FluentFiles.UnionWith(Directory.GetFiles(Path.Combine(Application.dataPath, "Resources"),
					"*.ftl", SearchOption.TopDirectoryOnly)
				.Select(ProcessPath));

			ReloadBuildCache();
		}

		private static void ReloadBuildCache() {
			var buildDir = BuildDirectory;
			var buildDirExists = Directory.Exists(buildDir);

			_buildCount = buildDirExists ? Directory.GetDirectories(buildDir).Length : 0;
			_buildSize = 0;

			if (buildDirExists) {
				var dirInfo = new DirectoryInfo(buildDir);
				_buildSize += dirInfo.GetDirectories()
					.Sum(dir => dir.EnumerateFiles("*", SearchOption.AllDirectories)
						.Sum(file => file.Length));
			}

			_buildSizeString = _buildSize switch {
				< 1024 => _buildSize + " B",
				< 1024 * 1024 => (_buildSize / 1024.0).ToString("F2") + " KiB",
				< 1024 * 1024 * 1024 => (_buildSize / (1024.0 * 1024.0)).ToString("F2") + " MiB",
				_ => (_buildSize / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " GiB"
			};
		}

		private static readonly Func<string, string> ProcessPath = Path.GetFileNameWithoutExtension;

		private static readonly Func<string, string> ProcessRelativePath =
			path => Path.GetRelativePath(Application.dataPath, path);

		private void OnDestroy() {
			ReleaseStaticResources();
			ReleaseResources();
		}

		private static void ReleaseStaticResources() {
			_scenesDirectoryWatcher?.Dispose();
			_assemblyDefinitionsDirectoryWatcher?.Dispose();
			_fluentDirectoryWatcher?.Dispose();
			_buildDirectoryWatcher?.Dispose();

			Scenes.Clear();
			AssemblyDefinitions.Clear();
			AssemblyDefinitionRelativePaths.Clear();
			FluentFiles.Clear();
			BuildFiles.Clear();

			_buildCount = 0;
			_buildSize = 0;
			_buildSizeString = "0 B";
		}

		private void ReleaseResources() {
			_modInfo?.Dispose();
			_modInfo = null;

			_modInfoSps?.Clear();
		}

		// NOTE: **NEVER** use return unless it is for assertion
		private void OnGUI() {
			ExtendedGUILayout.SetupGUIStyles();

			ExtendedGUILayout.SetGUIBackgroundColor(Color.gray);
			using (new GUILayout.HorizontalScope()) {
				foreach (var tab in AllTabs) {
					var thisTab = tab == _currentTab;
					if (thisTab) ExtendedGUILayout.SetGUIBackgroundColor(BrandColor);

					if (GUILayout.Button(tab.ToString()))
						_currentTab = tab;

					if (thisTab) ExtendedGUILayout.SetGUIBackgroundColor(Color.gray);
				}
			}

			ExtendedGUILayout.SetGUIBackgroundColor(Color.white);

			switch (_currentTab) {
				case Tab.Build:
					var config = ProjectToolsConfig.Config;
					if (!config) {
						GUILayout.Label(
							"<color=#ffff00><b>⚠️ Configuration isn't loaded yet, it should only take a few seconds...</b></color>");
						return;
					}

					var isBuilding = config.ModBuilder.IsBuilding;
					if (isBuilding) GUI.enabled = false;

					ExtendedGUILayout.SectionTitle("Game Importer");

					GUILayout.BeginHorizontal();
					var adofaiPath = EditorGUILayout.TextField("ADOFAI Executable Path", config.adofaiPath);
					if (GUILayout.Button("Find...", GUILayout.Width(60))) {
						// ReSharper disable JoinDeclarationAndInitializer
						string initialDirectory;
						string extension;
						// ReSharper restore JoinDeclarationAndInitializer
						
						#if UNITY_EDITOR_WIN
						initialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\A Dance of Fire and Ice";
						extension = "exe";
						#elif UNITY_EDITOR_OSX
						initialDirectory = "~/Library/Application Support/Steam/steamapps/common/ADanceOfFireAndIce";
						extension = "app";
						#elif UNITY_EDITOR_LINUX
						initialDirectory = "~/.local/share/Steam/steamapps/common/A Dance of Fire and Ice";
						extension = string.Empty;
						#else
						initialDirectory = Application.dataPath;
						extension = string.Empty;
						#endif

						var executablePath = EditorUtility.OpenFilePanel("Find Game Executable", initialDirectory, extension);
						
						if (!string.IsNullOrEmpty(executablePath))
							adofaiPath = executablePath;
					}
					GUILayout.EndHorizontal();
					
					var modifiedPath = config.adofaiPath != adofaiPath;
					var modified = modifiedPath;

					if (string.IsNullOrEmpty(adofaiPath)) {
						GUILayout.Label(
							"<color=red><b>⚠️ This field is required for importing or copying the mod files.</b></color>");
					} else if (modifiedPath || _firstRender) {
						if (!File.Exists(config.adofaiPath = adofaiPath)) {
							GUILayout.Label("<color=red><b>⚠️ File not found.</b></color>");
						}
					}

					ExtendedGUILayout.SetGUIBackgroundColor(Color.green);

					if (GUILayout.Button("<b>Import ADOFAI</b>", GUILayout.Height(32))) {
						var continueImporting = EditorUtility.DisplayDialog(
							"Are you sure?",
							"Importing the game assembly may take a while, and you will likely be asked to restart the Unity Editor.",
							"Yes, continue",
							"No");

						if (!continueImporting)
							return;

						if (string.IsNullOrEmpty(config.adofaiPath)) {
							EditorUtility.DisplayDialog("Error", "Please set the ADOFAI Executable Path first.", "OK");
							return;
						}

						config.Importer.SetGamePath(config.adofaiPath);
						config.Importer.Import();
					}

					ExtendedGUILayout.SetGUIBackgroundColor(Color.white);
					ExtendedGUILayout.SectionTitle("Mod Information");

					if (_modInfo == null) {
						GUILayout.Label(
							"<color=#ffff00><b>⚠️ Info.json SerializedObject isn't loaded yet, it should only take a few seconds...</b></color>");
					} else {
						using (new ExtendedGUILayout.IndentScope()) {
							var openModInfoFoldout = EditorGUILayout.Foldout(config.openModInfoFoldout,
								"   <b>Display Info.json</b>");

							if (openModInfoFoldout != config.openModInfoFoldout) {
								config.openModInfoFoldout = openModInfoFoldout;
								modified = true;
							}

							if (openModInfoFoldout) {
								using (new ExtendedGUILayout.IndentScope(24)) {
									foreach (var fieldInfo in ModInfoFields) {
										if (_modInfoSps.TryGetValue(fieldInfo, out var modInfoProperty))
											EditorGUILayout.PropertyField(modInfoProperty);
									}

									// immediately save changes
									if (_modInfo.ApplyModifiedProperties()) {
										EditorUtility.SetDirty(_modInfo.targetObject);
										AssetDatabase.SaveAssetIfDirty(_modInfo.targetObject);
									}
								}
							}
						}
					}

					ExtendedGUILayout.SectionTitle("Builder");

					ExtendedGUILayout.SetGUIBackgroundColor(OrangeColor);

					GUILayout.Label("<color=#999999><b>Asset Bundles</b></color>");
					using (new ExtendedGUILayout.IndentScope()) {
						var skipAssetBundleBuild =
							GUILayout.Toggle(config.skipAssetBundleBuild, "don't build asset bundles");

						if (skipAssetBundleBuild != config.skipAssetBundleBuild) {
							config.skipAssetBundleBuild = skipAssetBundleBuild;
							modified = true;
						}

						GUILayout.Space(4);

						var buildEveryPlatform = GUILayout.Toggle(config.buildEveryPlatform,
							"build asset bundles for all platforms");

						if (buildEveryPlatform != config.buildEveryPlatform) {
							config.buildEveryPlatform = buildEveryPlatform;
							modified = true;
						}
					}

					GUILayout.Space(8);

					ExtendedGUILayout.SetGUIBackgroundColor(PinkColor);

					GUILayout.Label("<color=#999999><b>Build Options</b></color>");
					using (new ExtendedGUILayout.IndentScope()) {
						var developmentBuild = GUILayout.Toggle(config.developmentBuild, "debug build");

						if (developmentBuild != config.developmentBuild) {
							config.developmentBuild = developmentBuild;
							modified = true;
						}

						GUILayout.Space(4);

						var generateDebugSymbols =
							GUILayout.Toggle(config.generateDebugSymbols, "generate debug symbols (.pdb)");

						if (generateDebugSymbols != config.generateDebugSymbols) {
							config.generateDebugSymbols = generateDebugSymbols;
							modified = true;
						}
					}

					GUILayout.Space(8);
					ExtendedGUILayout.SetGUIBackgroundColor(SkyBlueColor);

					GUILayout.Label("<color=#999999><b>Post Build Event</b></color>");
					using (new ExtendedGUILayout.IndentScope()) {
						var copyToDirectory =
							GUILayout.Toggle(config.copyToDirectory, "copy to mod path");

						if (copyToDirectory != config.copyToDirectory) {
							config.copyToDirectory = copyToDirectory;
							modified = true;
						}

						GUILayout.Space(4);

						var zip = GUILayout.Toggle(config.createZip, "create a .zip file in build directory");

						if (zip != config.createZip) {
							config.createZip = zip;
							modified = true;
						}

						GUILayout.Space(4);

						var runApplication =
							GUILayout.Toggle(config.runApplication, "run application after building mod");

						if (runApplication != config.runApplication) {
							config.runApplication = runApplication;
							modified = true;
						}

						GUILayout.Space(4);

						using (new GUILayout.HorizontalScope()) {
							GUILayout.Label("<color=#00aaff>└</color>", GUILayout.Width(16));

							var runAppThruSteam =
								GUILayout.Toggle(config.runApplicationThroughSteam,
									"run with steam instead of directly executing binary");

							if (runAppThruSteam != config.runApplicationThroughSteam) {
								config.runApplicationThroughSteam = runAppThruSteam;
								modified = true;
							}
						}
					}

					GUILayout.Space(12);

					ExtendedGUILayout.SetGUIBackgroundColor(Color.white);
					GUILayout.Label("<color=#999999><b>Build Option Presets</b></color>");
					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("Debug")) {
							config.buildEveryPlatform = false;
							config.copyToDirectory = true;
							config.createZip = false;
							config.runApplication = true;

							config.developmentBuild = true;
							config.generateDebugSymbols = true;

							modified = true;
						}

						if (GUILayout.Button("Release")) {
							config.buildEveryPlatform = true;
							config.createZip = true;

							config.developmentBuild = false;
							config.generateDebugSymbols = false;

							modified = true;
						}

						ExtendedGUILayout.SetGUIBackgroundColor(Color.red + Color.white * .6f);
						if (GUILayout.Button("Clear All", GUILayout.Width(72))) {
							config.skipAssetBundleBuild = false;
							config.buildEveryPlatform = false;
							config.copyToDirectory = false;
							config.createZip = false;
							config.runApplication = false;
							config.runApplicationThroughSteam = false;
							config.developmentBuild = false;
							config.generateDebugSymbols = false;

							modified = true;
						}
					}

					ExtendedGUILayout.SetGUIBackgroundColor(Color.green);
					if (GUILayout.Button("<b>Build Mod</b>", GUILayout.Height(32))) {
						config.BuildMod(config.copyToDirectory
							? Path.Combine(Path.GetDirectoryName(config.adofaiPath)!, "Mods", "CourseMod")
							: null);
					}

					ExtendedGUILayout.SetGUIBackgroundColor(Color.white);
					if (isBuilding) GUI.enabled = true;

					ExtendedGUILayout.SectionTitle("Build Management");
					var buildDir = BuildDirectory;

					ExtendedGUILayout.SetGUIBackgroundColor(OpenLinkColor);
					if (GUILayout.Button("Open Build Directory")) {
						Application.OpenURL(buildDir);
					}

					GUILayout.Space(12);

					GUILayout.Label($"Cached Builds: {_buildCount} (Using <b>{_buildSizeString}</b> of Storage)");

					ExtendedGUILayout.SetGUIBackgroundColor(Color.red);
					if (isBuilding) GUI.enabled = false;

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("Delete All Builds")) {
							config.DeleteBuilds();
						}

						ExtendedGUILayout.SetGUIBackgroundColor(Color.white);

						GUILayout.Label("Except for ", GUILayout.MaxWidth(60));
						var exceptString = config.deleteBuildsExceptLastN.ToString();
						var exceptStringNew = GUILayout.TextField(exceptString);

						if (exceptString != exceptStringNew) {
							if (int.TryParse(exceptStringNew, out var exceptNew)) {
								config.deleteBuildsExceptLastN = Math.Abs(exceptNew);
								modified = true;
							}
						}

						GUILayout.Label(" Build(s)");
					}

					if (isBuilding) GUI.enabled = true;

					GUILayout.Space(12);

					ExtendedGUILayout.SetGUIBackgroundColor(Color.red);
					using (new ExtendedGUILayout.IndentScope()) {
						var autoDeleteBuilds =
							GUILayout.Toggle(config.automaticallyDeleteBuilds, "Automatically Delete Builds");

						if (autoDeleteBuilds != config.automaticallyDeleteBuilds) {
							config.automaticallyDeleteBuilds = autoDeleteBuilds;
							modified = true;
						}
					}

					ExtendedGUILayout.SetGUIBackgroundColor(Color.white);

					if (modified) {
						EditorUtility.SetDirty(config);
					}

					ExtendedGUILayout.SectionTitle("Notes");
					GUILayout.Label(
						"· Put the scenes in <b>course_scenes.bundle</b>, and other assets in <b>course_assets.bundle</b>");
					GUILayout.Label("· DO NOT add the test scenes in the bundle.");
					GUILayout.Label("· When fixing patches for future updates, use the frontline beta");
					break;

				case Tab.Shortcut:
					ExtendedGUILayout.SectionTitle("Project", bottomMargin: 0, bottomLineMargin: 4);

					ExtendedGUILayout.SetGUIBackgroundColor(OpenLinkColor);
					using (new GUILayout.HorizontalScope()) {
						// ReSharper disable once ConditionIsAlwaysTrueOrFalse
						if (string.IsNullOrEmpty(RepositoryLink)) {
							GUILayout.Label(
								"<color=#ffff00><b>⚠️ Repository Shortcut is disabled.\n     Edit ProjectToolsWindow.cs#L20 to enable it.</b></color>");

							ExtendedGUILayout.SetGUIBackgroundColor(Color.yellow);
							if (GUILayout.Button("Go to line")) {
								OpenScriptInEditor("Assets/Editor/ProjectToolsWindow.cs", 20);
							}
						} else {
							if (GUILayout.Button("Open Repository")) {
								Application.OpenURL(RepositoryLink);
							}

							ExtendedGUILayout.SetGUIBackgroundColor(OpenLinkSubColor);
							if (GUILayout.Button("Issues", GUILayout.MaxWidth(88))) {
								Application.OpenURL($"{RepositoryLink}/issues");
							}

							if (GUILayout.Button("Pull Requests", GUILayout.MaxWidth(88))) {
								Application.OpenURL($"{RepositoryLink}/pulls");
							}
						}
					}

					GUILayout.Space(12);

					// ReSharper disable once ConditionIsAlwaysTrueOrFalse
					if (string.IsNullOrEmpty(I18NLink)) {
						GUILayout.Label(
							"<color=#ffff00><b>⚠️ I18N Shortcut is disabled.\n     Edit ProjectToolsWindow.cs#L21 to enable it.</b></color>");

						ExtendedGUILayout.SetGUIBackgroundColor(Color.yellow);
						if (GUILayout.Button("Go to line")) {
							OpenScriptInEditor("Assets/Editor/ProjectToolsWindow.cs", 21);
						}
					} else {
						ExtendedGUILayout.SetGUIBackgroundColor(OpenLinkColor);
						if (GUILayout.Button("Open I18N URL")) {
							Application.OpenURL(I18NLink);
						}
					}

					GUILayout.Space(8);

					ExtendedGUILayout.SetGUIBackgroundColor(Color.white);
					using (new GUILayout.HorizontalScope()) {
						GUILayout.Label("<color=#999999><b>I18N Assets: </b></color>");

						foreach (var fluentFile in FluentFiles)
							if (GUILayout.Button(fluentFile, GUILayout.Width(72)))
								OpenScriptInEditor(Path.Combine("Assets/Resources", fluentFile + ".ftl"));
					}

					ExtendedGUILayout.SectionTitle("Scenes", bottomMargin: 0, bottomLineMargin: 4);
					ExtendedGUILayout.SetGUIBackgroundColor(OpenAssetColor);

					foreach (var scene in Scenes)
						if (GUILayout.Button(scene))
							SwitchToScene(scene);

					ExtendedGUILayout.SectionTitle("Assembly Definitions", bottomMargin: 0, bottomLineMargin: 4);

					foreach (var asmdef in AssemblyDefinitions)
						if (GUILayout.Button(asmdef))
							SelectAsset(AssemblyDefinitionRelativePaths[asmdef]);

					ExtendedGUILayout.SetGUIBackgroundColor(Color.white);

					break;

				case Tab.Test:
					ExtendedGUILayout.SectionTitle("TBD");
					GUILayout.Label("· TBD");
					break;

				default:
					_currentTab = Tab.Build;
					break;
			}

			_firstRender = false;
		}

		private static void SwitchToScene(string sceneName) // copied from adofai code haha
		{
			if (Application.isPlaying) {
				if (sceneName.Contains('/'))
					sceneName = sceneName.Split('/').Last();

				SceneManager.LoadScene(sceneName);
			} else if (!SceneManager.GetActiveScene().name.Contains(sceneName)) {
				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
				EditorSceneManager.OpenScene(Path.Combine("Assets/Scenes", sceneName) + ".unity");
			}
		}

		private static void SelectAsset(string path) {
			Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
			FocusInspectorWindow();
		}

		private static void OpenAsset(string path) {
			// PrefabUtility.LoadPrefabContentsIntoPreviewScene(path, );
			AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(path));
		}

		private static void FocusInspectorWindow() {
			var inspectorWindow =
				GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow"));

			inspectorWindow.Show();
			inspectorWindow.Focus();
		}

		private static void OpenScriptInEditor(string path, int? line = null) {
			UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, line ?? 1);
		}
	}
}