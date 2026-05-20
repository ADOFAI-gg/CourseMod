using System;
using CourseMod.DataModel;

namespace CourseMod.Utils {
	public static class CourseTools {
		public static CourseFailReason ToFailReason(this ConstraintType constraintType)
			=> constraintType switch {
				ConstraintType.Accuracy => CourseFailReason.AccuracyConstraint,
				ConstraintType.Death => CourseFailReason.DeathConstraint,
				ConstraintType.Life => CourseFailReason.LifeConstraint,
				_ => throw new ArgumentOutOfRangeException(nameof(constraintType), constraintType, null)
			};

		public static string ToFailMessage(this CourseFailReason failReason)
			=> failReason switch {
				CourseFailReason.AccuracyConstraint => I18N.Get("general-fail-Accuracy"),
				CourseFailReason.DeathConstraint => I18N.Get("general-fail-Death"),
				CourseFailReason.LifeConstraint => I18N.Get("general-fail-Life"),
				CourseFailReason.PlayerIntent => null,
				CourseFailReason.VanillaGameMechanics => null,
				_ => throw new ArgumentOutOfRangeException(nameof(CourseFailReason), failReason, null)
			};
	}
}