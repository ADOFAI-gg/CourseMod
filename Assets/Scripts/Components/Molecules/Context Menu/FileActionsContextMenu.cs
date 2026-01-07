//Resharper disable CheckNamespace

using System.IO;
using CourseMod.Components.Scenes;
using CourseMod.DataModel;
using CourseMod.Utils;
using Newtonsoft.Json;

namespace CourseMod.Components.Molecules.ContextMenu {
	public class FileActionsContextMenu : ContextMenu<ActionType.File, CourseEditorScene> {
		protected override string I18NKeyPrefix => "editor-file-action-";

		protected override void InitializeItems() {
			InitItem(new NewCourse(Handler, I18NKeyPrefix));
			InitItem(new OpenCourse(Handler, I18NKeyPrefix));
			InitItem(new SaveCourse(Handler, I18NKeyPrefix));
			InitItem(new SaveCourseAs(Handler, I18NKeyPrefix));
			InitItem(new ExportCourse(Handler, I18NKeyPrefix));
			InitItem(new Quit(Handler, I18NKeyPrefix));
		}

		private class NewCourse : ContextMenuItemGenerator<ActionType.File, CourseEditorScene> {
			public NewCourse(CourseEditorScene handler, string i18NKeyPrefix) : base(handler, ActionType.File.NewCourse,
				i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				string path = FileDialogTools.SaveCourseFileDialog(Path.GetDirectoryName(Handler.LastOpenedCoursePath));
				if (string.IsNullOrEmpty(path)) return;

				Handler.LastOpenedCoursePath = path;
				Handler.PlayerSettings.LastOpenedEditorFilePath = path;

				CourseEditorScene.CurrentCourse = Course.Default;
				File.WriteAllText(path,
					JsonConvert.SerializeObject(CourseEditorScene.CurrentCourse, Formatting.Indented));
				Handler.ApplyCourseToUI();
				Handler.SetDirty(false);
			}

			protected override ItemStatus Validate() {
				return ItemStatus.Show;
			}
		}

		private class OpenCourse : ContextMenuItemGenerator<ActionType.File, CourseEditorScene> {
			public OpenCourse(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.File.OpenCourse, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				Handler.RunWithUnsavedCheck(CourseEditorScene.EditorPopupType.UnsavedContinue,
					() => Handler.Open(true));
			}


			protected override ItemStatus Validate() {
				return ItemStatus.Show;
			}
		}

		private class SaveCourse : ContextMenuItemGenerator<ActionType.File, CourseEditorScene> {
			public SaveCourse(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.File.SaveCourse, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				Handler.Save();
			}

			protected override ItemStatus Validate() {
				return CourseEditorScene.CurrentCourse == null ? ItemStatus.Disable : ItemStatus.Show;
			}
		}

		private class SaveCourseAs : ContextMenuItemGenerator<ActionType.File, CourseEditorScene> {
			public SaveCourseAs(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.File.SaveCourseAs, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				Handler.Save(true);
			}

			protected override ItemStatus Validate() {
				return CourseEditorScene.CurrentCourse == null ? ItemStatus.Disable : ItemStatus.Show;
			}
		}

		private class ExportCourse : ContextMenuItemGenerator<ActionType.File, CourseEditorScene> {
			public ExportCourse(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.File.ExportCourse, i18NKeyPrefix, false) {
			}

			protected override void OnClick() => Handler.Export();

			protected override ItemStatus Validate() =>
				CourseEditorScene.CurrentCourse == null ? ItemStatus.Disable : ItemStatus.Show;
		}

		private class Quit : ContextMenuItemGenerator<ActionType.File, CourseEditorScene> {
			public Quit(CourseEditorScene handler, string i18NKeyPrefix) : base(handler, ActionType.File.Quit,
				i18NKeyPrefix, true) {
			}

			protected override void OnClick() {
				Handler.RunWithUnsavedCheck(CourseEditorScene.EditorPopupType.UnsavedQuit, () => Handler.Quit());
				;
			}

			protected override ItemStatus Validate() {
				return ItemStatus.Show;
			}
		}
	}
}