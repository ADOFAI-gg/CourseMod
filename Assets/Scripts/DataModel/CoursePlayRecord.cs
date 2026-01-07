using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
	public record CoursePlayRecord {
		public CourseLevelPlayRecord[] Records;

		[JsonIgnore]
		public double TotalAccuracy => Records.Length == 0
			? 0
			: Records.Sum(record => record.XAccuracy);

		[JsonIgnore]
		public SerializableHitMargins TotalHitMargins => Records.Length == 0
			? new()
			: Records.Select(r => r.HitMargins).Aggregate((a, b) => a + b);

		public int TotalFloors => Records.Length == 0
			? 0
			: Records.Sum(record => record.TotalFloors);
	}
}