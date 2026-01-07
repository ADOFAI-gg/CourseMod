//Resharper disable CheckNamespace

namespace CourseMod.Components.Molecules.ContextMenu {
	public static class ActionType {
		public enum File {
			NewCourse,
			OpenCourse,
			SaveCourse,
			SaveCourseAs,
			ExportCourse,
			Quit
		}

		public enum LevelEventListItem {
			EnableAccuracyConstraint,
			DisableAccuracyConstraint,
			EnableDeathConstraint,
			DisableDeathConstraint,
			EnableLifeConstraint,
			DisableLifeConstraint,
			AddCutscene,
			ChangeCutscene,
			RemoveCutscene,
			EnableMysterious,
			DisableMysterious,
			ChangeLevel,
			RemoveLevel
		}
	}
}