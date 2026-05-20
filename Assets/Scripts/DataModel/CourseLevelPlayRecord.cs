using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public struct CourseLevelPlayRecord : IEquatable<CourseLevelPlayRecord> {
		public string CourseChecksum;
		public string GameplayChecksum;
		public int LevelNumber;

		public float XAccuracy;
		public SerializableHitMargins HitMargins;
		public int TotalFloors;

		public string GetLogString() {
			var s = new StringBuilder();
			s.AppendLine("Object <CourseLevelPlayRecord>:");
			s.Append("\tㄴ CourseChecksum: ").AppendLine(CourseChecksum);
			s.Append("\tㄴ GameplayChecksum: ").AppendLine(GameplayChecksum);
			s.Append("\tㄴ LevelNumber: ").Append(LevelNumber).AppendLine();
			s.Append("\tㄴ XAccuracy: ").Append(XAccuracy).AppendLine();
			s.Append("\tㄴ HitMargins: ").AppendLine(JsonConvert.SerializeObject(HitMargins));
			s.Append("\tㄴ TotalFloors: ").Append(TotalFloors).AppendLine();

			return s.ToString();
		}

		public bool Equals(CourseLevelPlayRecord other) => CourseChecksum == other.CourseChecksum && GameplayChecksum == other.GameplayChecksum && LevelNumber == other.LevelNumber && XAccuracy.Equals(other.XAccuracy) && Equals(HitMargins, other.HitMargins) && TotalFloors == other.TotalFloors;
		public override bool Equals(object obj) => obj is CourseLevelPlayRecord other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(CourseChecksum, GameplayChecksum, LevelNumber, XAccuracy, HitMargins, TotalFloors);
	}
}