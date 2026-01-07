using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using CourseMod.Components.Scenes;
using CourseMod.DataModel;
using CourseMod.Patches;
using CourseMod.Utils;
using DG.Tweening;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace CourseMod {
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class Entry {
		public static UnityModManager.ModEntry ModEntry;

		private static readonly Harmony Harmony = new("COURSE-HARMONY");
		private static string _initialDisplayName;

#if DEBUG
		private static DebugSettings _debugSettings;
#endif

		public static void Load(UnityModManager.ModEntry modEntry) {
			ModEntry = modEntry;
			modEntry.OnToggle = OnToggle;
#if DEBUG
			modEntry.OnGUI = OnSettingsGUI;
			modEntry.OnSaveGUI = OnSaveSettings;
			_debugSettings = ModDataStorage.PlayerSettings.DebugSettings;
#endif

			_initialDisplayName = modEntry.Info.DisplayName;

			var ns = Application.platform switch {
				RuntimePlatform.WindowsPlayer => "win",
				RuntimePlatform.OSXPlayer => "mac",
				RuntimePlatform.LinuxPlayer => "linux",
				_ => throw new ArgumentOutOfRangeException(nameof(Application.platform))
			};

			Assets.SetAssetBundle(AssetBundle.LoadFromFile(Path.Combine(modEntry.Path, ns) + "/course_assets.bundle"));
			AssetBundle.LoadFromFile(Path.Combine(modEntry.Path, ns) + "/course_scenes.bundle");

			foreach (var file in Directory.GetFiles(modEntry.Path, "*.dll")) {
				if (file.EndsWith("CourseMod.dll"))
					continue;

				Assembly.LoadFrom(file);
			}

			// Set main thread name for easier logging identification
			Thread.CurrentThread.Name ??= "Main Thread";

			I18N.Setup();
		}

		private static bool OnToggle(UnityModManager.ModEntry _, bool value) {
			if (value) {
				Harmony.PatchAll();
				ModEntry.Info.DisplayName += " <b>(Ctrl + Shift + , (Comma))</b>";
			} else {
				Harmony.UnpatchAll();
				ModEntry.Info.DisplayName = _initialDisplayName;
			}

			return true;
		}

#if DEBUG

		private static void OnSettingsGUI(UnityModManager.ModEntry _) {
			using (new GUILayout.HorizontalScope()) {
				GUILayout.Label("Course Path");

				if (GUILayout.Button("Find in dialog...")) {
					var coursePath = FileDialogTools.OpenCourseFileDialog(Persistence.GetLastUsedFolder());

					if (!string.IsNullOrEmpty(coursePath) && File.Exists(coursePath)) {
						if (coursePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
							string destinationDirectory = CourseCollection.ParseDestinationDirectory(
								Path.GetFileNameWithoutExtension(coursePath), Path.GetDirectoryName(coursePath));
							Directory.CreateDirectory(destinationDirectory);
							Encoding enc = Encoding.GetEncoding(949);
							ZipFile.ExtractToDirectory(coursePath, destinationDirectory, enc);
							string[] courseFiles = CourseCollection.GetCoursePaths(destinationDirectory);
							if (courseFiles.Length == 0)
								throw new FileNotFoundException("No course file found in the extracted zip archive.");
							_debugSettings.CoursePath = courseFiles[0];
						} else _debugSettings.CoursePath = coursePath;
					}
				}

				_debugSettings.CoursePath = GUILayout.TextField(_debugSettings.CoursePath, GUILayout.Width(400));

				if (GUILayout.Button("Play Course")) {
					var coursePath = _debugSettings.CoursePath;
					if (!string.IsNullOrEmpty(coursePath) && File.Exists(coursePath)) {
						LogTools.Log($"Attempting to read {coursePath}");

						GameplayPatches.CourseState.Reset();
						GameplayPatches.CourseState.SelectedCourse = CourseCollection.ReadSingleCourse(coursePath);
						CourseTransitionScene.BeginCourse();
					}
				}
			}

			GUILayout.Space(12);

			_debugSettings.DisableNoFail = GUILayout.Toggle(_debugSettings.DisableNoFail, "Always disable No Fail");
			_debugSettings.ForceEnableTransitionSidebarMenu =
				GUILayout.Toggle(_debugSettings.ForceEnableTransitionSidebarMenu, "Enable sidebar menu on play screen");

			GUILayout.Space(12);
			using (new GUILayout.HorizontalScope()) {
				GUILayout.Label("<b>Fast Travel</b>");
				GUILayout.FlexibleSpace();

				if (GUILayout.Button(CourseSelectScene.SCENE_NAME)) {
					DOTween.KillAll();
					SceneManager.LoadScene(CourseSelectScene.SCENE_NAME);
				}

				if (GUILayout.Button(CourseEditorScene.SCENE_NAME)) {
					DOTween.KillAll();
					SceneManager.LoadScene(CourseEditorScene.SCENE_NAME);
				}
			}
		}

		private static void OnSaveSettings(UnityModManager.ModEntry _) {
			ModDataStorage.PlayerSettings.Save();
		}

#endif
	}
}