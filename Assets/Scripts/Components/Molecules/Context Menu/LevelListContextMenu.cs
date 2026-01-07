// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourseMod.Components.Atoms;
using CourseMod.Components.Scenes;
using CourseMod.DataModel;
using CourseMod.Utils;
using TMPro;

namespace CourseMod.Components.Molecules.ContextMenu {
	public class LevelListContextMenu : ContextMenu<ActionType.LevelEventListItem, CourseEditorScene> {
		public TextMeshProUGUI itemCountLabel;
		protected override string I18NKeyPrefix => "editor-level-action-";


		private void Awake() {
			OnOpen += RefreshItemCountLabel;
		}

		private void RefreshItemCountLabel() {
			int count = Handler.levelList.SelectedLevels.Length;
			bool isMultiselect = count > 1;
			itemCountLabel.gameObject.SetActive(isMultiselect);

			if (isMultiselect) {
				itemCountLabel.text = I18N.Get("editor-level-action-multiselect-mark",
					new Dictionary<string, object> { { "count", count } });
			}
		}


		protected override void InitializeItems() {
			InitItem(new EnableAccuracyConstraint(Handler, I18NKeyPrefix));
			InitItem(new DisableAccuracyConstraint(Handler, I18NKeyPrefix));
			InitItem(new EnableDeathConstraint(Handler, I18NKeyPrefix));
			InitItem(new DisableDeathConstraint(Handler, I18NKeyPrefix));
			InitItem(new EnableLifeConstraint(Handler, I18NKeyPrefix));
			InitItem(new DisableLifeConstraint(Handler, I18NKeyPrefix));
			InitItem(new AddCutscene(Handler, I18NKeyPrefix));
			InitItem(new ChangeCutscene(Handler, I18NKeyPrefix));
			InitItem(new RemoveCutscene(Handler, I18NKeyPrefix));
			InitItem(new EnableMysterious(Handler, I18NKeyPrefix));
			InitItem(new DisableMysterious(Handler, I18NKeyPrefix));
			InitItem(new ChangeLevel(Handler, I18NKeyPrefix));
			InitItem(new RemoveLevel(Handler, I18NKeyPrefix));
		}

		private class
			EnableAccuracyConstraint : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public EnableAccuracyConstraint(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.EnableAccuracyConstraint, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;
					Assert.True(levelData.DisableAccuracyConstraint, "Enabled accuracy constraint already exists");
					levelData.DisableAccuracyConstraint = false;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return CourseEditorScene.CurrentCourse?.Settings.AccuracyConstraint != null
				       && Handler.levelList.SelectedLevels.All(l => l.levelData.DisableAccuracyConstraint)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class
			DisableAccuracyConstraint : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public DisableAccuracyConstraint(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.DisableAccuracyConstraint, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				bool result = false;
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;
					if (levelData.DisableAccuracyConstraint) continue;
					result = true;
					levelData.DisableAccuracyConstraint = true;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Assert.True(result, "Enabled accuracy constraint doesn't exist");
				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return CourseEditorScene.CurrentCourse?.Settings.AccuracyConstraint != null
				       && Handler.levelList.SelectedLevels.Any(l => !l.levelData.DisableAccuracyConstraint)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class
			EnableDeathConstraint : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public EnableDeathConstraint(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.EnableDeathConstraint, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;
					Assert.True(levelData.DisableDeathConstraint, "Enabled death constraint already exists");

					levelData.DisableDeathConstraint = false;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return CourseEditorScene.CurrentCourse?.Settings.DeathConstraint != null
				       && Handler.levelList.SelectedLevels.All(l => l.levelData.DisableDeathConstraint)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class
			DisableDeathConstraint : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public DisableDeathConstraint(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.DisableDeathConstraint, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				bool result = false;
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;
					if (levelData.DisableDeathConstraint) continue;
					result = true;
					levelData.DisableDeathConstraint = true;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Assert.True(result, "Enabled death constraint doesn't exist");
				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return CourseEditorScene.CurrentCourse?.Settings.DeathConstraint != null &&
				       Handler.levelList.SelectedLevels.Any(l => !l.levelData.DisableDeathConstraint)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class
			EnableLifeConstraint : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public EnableLifeConstraint(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.EnableLifeConstraint, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;

					Assert.True(levelData.DisableLifeConstraint, "Enabled life constraint already exists");
					levelData.DisableLifeConstraint = false;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return CourseEditorScene.CurrentCourse?.Settings.LifeConstraint != null
				       && Handler.levelList.SelectedLevels.All(l => l.levelData.DisableLifeConstraint)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class
			DisableLifeConstraint : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public DisableLifeConstraint(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.DisableLifeConstraint, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				bool result = false;
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;
					if (levelData.DisableLifeConstraint) continue;

					result = true;
					levelData.DisableLifeConstraint = true;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Assert.True(result, "Enabled life constraint doesn't exist");
				Handler.SetDirty(true);
			}


			protected override ItemStatus Validate() {
				return CourseEditorScene.CurrentCourse?.Settings.LifeConstraint != null &&
				       Handler.levelList.SelectedLevels.Any(l => !l.levelData.DisableLifeConstraint)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class AddCutscene : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public AddCutscene(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.AddCutscene, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				var items = Handler.levelList.SelectedLevels;
				Assert.True(items.Length == 1, "Adding cutscene for multiple levels is not allowed");

				var item = items[0];
				CourseLevel levelData = item.levelData;
				string srcPath = FileDialogTools.OpenVideoFileDialog(Handler.LastOpenedCoursePath);
				if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath)) return; //you closed file manually

				string courseDirectory =
					Path.GetDirectoryName(Path.GetFullPath(Handler.LastOpenedCoursePath))!;
				string dstPath = Path.Combine(courseDirectory, Path.GetFileName(srcPath));

				if (!string.IsNullOrEmpty(srcPath) && File.Exists(srcPath) && srcPath != dstPath)
					File.Copy(srcPath, dstPath, true);

				Assert.True(levelData.CutsceneFile == null,
					"Cutscene file already exists. cannot be overwritten. use ChangeCutscene instead.");

				levelData.CutsceneFile = dstPath;
				item.levelData = levelData;
				item.ReloadChips();

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				var items = Handler.levelList.SelectedLevels;

				return items.All(e => e.levelData.CutsceneFile == null)
					? items.Length == 1
						? ItemStatus.Show
						: ItemStatus.Disable
					: ItemStatus.Hide;
			}
		}

		private class ChangeCutscene : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public ChangeCutscene(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.ChangeCutscene, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				var items = Handler.levelList.SelectedLevels;
				Assert.True(items.Length == 1, "Changing cutscene for multiple levels is not allowed");
				var item = items[0];

				CourseLevel levelData = item.levelData;
				string srcPath = FileDialogTools.OpenVideoFileDialog(Handler.LastOpenedCoursePath);
				if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath)) return; //you closed file manually

				string courseDirectory =
					Path.GetDirectoryName(Path.GetFullPath(Handler.LastOpenedCoursePath))!;
				string dstPath = Path.Combine(courseDirectory, Path.GetFileName(srcPath));

				if (!string.IsNullOrEmpty(srcPath) && File.Exists(srcPath) && srcPath != dstPath)
					File.Copy(srcPath, dstPath, true);

				Assert.True(levelData.CutsceneFile != null,
					"Cutscene file item is null; use AddCutscene instead of this");

				levelData.CutsceneFile = dstPath;

				item.levelData = levelData;
				item.ReloadChips();

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				var items = Handler.levelList.SelectedLevels;

				return items.Any(e => e.levelData.CutsceneFile != null)
					? items.Length == 1
						? ItemStatus.Show
						: ItemStatus.Disable
					: ItemStatus.Hide;
			}
		}

		private class RemoveCutscene : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public RemoveCutscene(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.RemoveCutscene, i18NKeyPrefix, true) {
			}

			protected override void OnClick() {
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;

					levelData.CutsceneFile = null;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				var items = Handler.levelList.SelectedLevels;

				return items.Any(e => e.levelData.CutsceneFile != null)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class EnableMysterious : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public EnableMysterious(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.EnableMysterious, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;
					Assert.False(levelData.Mysterious, "having mysterious levels is disallowed in this state");
					levelData.Mysterious = true;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return Handler.levelList.SelectedLevels.All(e => !e.levelData.Mysterious)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class DisableMysterious : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public DisableMysterious(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.DisableMysterious, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				bool result = false;
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;

					if (!levelData.Mysterious) continue;

					result = true;
					levelData.Mysterious = false;
					item.levelData = levelData;
					item.ReloadChips();
				}

				Assert.True(result, "mysterious levels in selection doesn't exist");

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return Handler.levelList.SelectedLevels.Any(e => e.levelData.Mysterious)
					? ItemStatus.Show
					: ItemStatus.Hide;
			}
		}

		private class ChangeLevel : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public ChangeLevel(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.ChangeLevel, i18NKeyPrefix, false) {
			}

			protected override void OnClick() {
				foreach (var item in Handler.levelList.SelectedLevels) {
					CourseLevel levelData = item.levelData;
					string levelPath = FileDialogTools.OpenLevelFileDialog();
					if (string.IsNullOrEmpty(levelPath) || !File.Exists(levelPath)) return;

					string dstPath = FileTools.CopyLevelToCourseDir(Handler.LastOpenedCoursePath, levelPath);
					levelData.AbsolutePath = dstPath;
					item.levelData = levelData;
					item.FillLevelInfo(CourseLevel.FromPath(dstPath, CourseEditorScene.CurrentCourse!.Value.FilePath));
					item.ReloadChips();
				}

				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return Handler.levelList.SelectedLevels.Length == 1
					? ItemStatus.Show
					: ItemStatus.Disable;
			}
		}

		private class RemoveLevel : ContextMenuItemGenerator<ActionType.LevelEventListItem, CourseEditorScene> {
			public RemoveLevel(CourseEditorScene handler, string i18NKeyPrefix) : base(handler,
				ActionType.LevelEventListItem.RemoveLevel, i18NKeyPrefix, true) {
			}

			protected override void OnClick() {
				Handler.levelList.RemoveLevels(Handler.levelList.SelectedLevels);
				Handler.SetDirty(true);
			}

			protected override ItemStatus Validate() {
				return ItemStatus.Show;
			}
		}
	}
}