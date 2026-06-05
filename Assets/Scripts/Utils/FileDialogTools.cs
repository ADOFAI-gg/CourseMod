using System.IO;
using UnityFileDialog;

namespace CourseMod.Utils {
	public static class FileDialogTools {
		private static string[] _courseExtensions;
		private static string[] _courseAndZipExtensions;
		private static string[] _zipExtensions;
		private static string[] _imageExtensions;
		private static string[] _videoExtensions;

		private static bool _hasSetup;

		private static void Setup() {
			Assert.False(_hasSetup, "Extension filters are already been setup");

			_courseExtensions = new[] { "course" };

			_courseAndZipExtensions = new[] { "course", "zip" };

			_zipExtensions = new[] { "zip" };

			_imageExtensions = new[] { "jpg", "jpeg", "png" };

			_videoExtensions = new[] { "webm" };

			_hasSetup = true;
		}

		public static string OpenThumbnailFileDialog(string initialPath) {
			if (!_hasSetup) Setup();

			var courseDirectory =
				initialPath.IsNullOrEmpty() ? "" : Path.GetDirectoryName(Path.GetFullPath(initialPath))!;

			var result = FileBrowser.PickFile(
				courseDirectory, 
				I18N.GetFromGame("editor.dialog.imageFileFormat"),
				_imageExtensions, 
				I18N.GetFromGame("editor.dialog.selectImage"));

			return result;
		}

		public static string OpenVideoFileDialog(string initialPath) {
			if (!_hasSetup) Setup();

			var result = FileBrowser.PickFile(
				initialPath == null ? "" : Path.GetDirectoryName(Path.GetFullPath(initialPath)),
				I18N.GetFromGame("editor.dialog.videoFileFormat"),
				_videoExtensions,
				I18N.GetFromGame("editor.dialog.selectVideo"));

			return result;
		}

		public static string OpenCourseFileDialog(string initialPath) {
			if (!_hasSetup) Setup();

			var result = FileBrowser.PickFile(
				initialPath ?? "",
				I18N.Get("general-file-dialog-course-description"),
				_courseAndZipExtensions,
				I18N.Get("editor-file-dialog-open-course"));

			return result;
		}

		public static string SaveCourseFileDialog(string initialPath) {
			if (!_hasSetup) Setup();

			var result = FileBrowser.SaveFile(
				initialPath ?? "",
				initialPath.IsNullOrEmpty() ? "" : Path.GetFileName(initialPath),
				I18N.Get("general-file-dialog-course-description"),
				_courseExtensions,
				I18N.Get("editor-file-dialog-save-course"));

			if (!result.EndsWith(".course")) {
				result += ".course";
			}
			
			return result;
		}

		public static string ExportCourseFileDialog(string initialPath) {
			if (!_hasSetup) Setup();

			var result = FileBrowser.SaveFile(
				initialPath ?? "",
				initialPath.IsNullOrEmpty() ? "" : Path.GetFileName(initialPath),
				I18N.Get("general-file-dialog-course-description"),
				_zipExtensions,
				I18N.Get("editor-file-dialog-save-course"));

			if (!result.EndsWith(".zip")) {
				result += ".zip";
			}
			
			return result;
		}

		public static string OpenLevelFileDialog() {
			if (!_hasSetup) Setup();

			var result = FileBrowser.PickFile(
				Persistence.GetLastUsedFolder(),
				I18N.GetFromGame("editor.dialog.adofaiLevelDescription"),
				GCS.levelExtensions,
				I18N.GetFromGame("editor.dialog.openFile"));

			if (string.IsNullOrEmpty(result) || !File.Exists(result)) return null; // you closed the window manually.

			return result;
		}
	}
}