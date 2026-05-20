using System;
using JetBrains.Annotations;

namespace CourseMod.DataModel {
	public struct CourseResult : IEquatable<CourseResult> {
		public CourseLevelPlayRecord[] Records;
		[CanBeNull] public CourseFailReason[] FailReasons;

		public CoursePlayRecord ToStoredValue() =>
			new() { Records = Records };

		public bool Equals(CourseResult other) => Equals(Records, other.Records) && Equals(FailReasons, other.FailReasons);
		public override bool Equals(object obj) => obj is CourseResult other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(Records, FailReasons);
	}
}