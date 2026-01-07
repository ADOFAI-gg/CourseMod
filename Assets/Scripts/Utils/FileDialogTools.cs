using System.IO;
using System.Linq;
using CourseMod.Components.Scenes;
using JetBrains.Annotations;
using SFB;
using UnityModManagerNet;

namespace CourseMod.Utils {
	public static class FileDialogTools {
		private static ExtensionFilter[] _courseFilter;
		private static ExtensionFilter[] _courseAndZipFilter;
		private static ExtensionFilter[] _customLevelFilter;
		private static ExtensionFilter[] _imageFilter;
		private static ExtensionFilter[] _videoFilter;

		private static bool _hasSetup;

		private static void Setup() {
			Assert.False(_hasSetup, "Extension filters are already been setup");

			_courseFilter = new[] { new ExtensionFilter(I18N.Get("general-file-dialog-course-description"), "course") };

			_courseAndZipFilter = new[] {
				new ExtensionFilter(I18N.Get("general-file-dialog-course-description"), "course", "zip")
			};

			_customLevelFilter = new[] {
				new ExtensionFilter(I18N.GetFromGame("editor.dialog.adofaiLevelDescription"), GCS.levelExtensions)
			};

			_imageFilter = new[] {
				new ExtensionFilter(I18N.GetFromGame("editor.dialog.imageFileFormat"), "jpg", "jpeg", "png")
			};

			_videoFilter = new[] { new ExtensionFilter(I18N.GetFromGame("editor.dialog.videoFileFormat"), "webm") };

			_hasSetup = true;
		}

		public static string OpenThumbnailFileDialog(string initialPath) {
			if (!_hasSetup) Setup();

			var courseDirectory =
				initialPath.IsNullOrEmpty() ? "" : Path.GetDirectoryName(Path.GetFullPath(initialPath))!;

			var result = StandaloneFileBrowser.OpenFilePanel(
				I18N.GetFromGame("editor.dialog.selectImage"),
				courseDirectory,
				_imageFilter,
				false).FirstOrDefault();

			return result;
		}

		public static string OpenVideoFileDialog(string initialPath) {
			if (!_hasSetup) Setup();

			var result = StandaloneFileBrowser.OpenFilePanel(
				I18N.GetFromGame("editor.dialog.selectVideo"),
				initialPath == null ? "" : Path.GetDirectoryName(Path.GetFullPath(initialPath)),
				_videoFilter,
				false).FirstOrDefault();

			return result;
		}

		public static string OpenCourseFileDialog(string initialPath) {
			if (!_hasSetup) Setup();
			var result = StandaloneFileBrowser.OpenFilePanel(
				I18N.Get("editor-file-dialog-open-course"),
				initialPath ?? "",
				_courseAndZipFilter,
				false).FirstOrDefault();

			return result;
		}

		public static string SaveCourseFileDialog(string initialPath) {
			if (!_hasSetup) Setup();
			var result = StandaloneFileBrowser.SaveFilePanel(
				I18N.Get("editor-file-dialog-save-course"),
				initialPath ?? "",
				initialPath.IsNullOrEmpty() ? "" : Path.GetFileName(initialPath),
				_courseFilter);
			return result;
		}

		public static string OpenLevelFileDialog() {
			if (!_hasSetup) Setup();


			string result = StandaloneFileBrowser.OpenFilePanel(
				I18N.GetFromGame("editor.dialog.openFile"),
				Persistence.GetLastUsedFolder(),
				_customLevelFilter,
				false).FirstOrDefault();

			if (string.IsNullOrEmpty(result) || !File.Exists(result)) return null; //you closed the window manually.

			return result;
		}
	}
}