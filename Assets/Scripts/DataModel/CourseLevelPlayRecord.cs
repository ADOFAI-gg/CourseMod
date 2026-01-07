using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public record CourseLevelPlayRecord {
		public string CourseChecksum;
		public string GameplayChecksum;
		public int LevelNumber;

		public double XAccuracy;
		public SerializableHitMargins HitMargins;
		public int TotalFloors;
	}
}