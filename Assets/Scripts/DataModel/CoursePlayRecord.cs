using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
	public struct CoursePlayRecord : IEquatable<CoursePlayRecord> {
		public CourseLevelPlayRecord[] Records;

		[JsonIgnore]
		private bool? _cleared;

		[JsonIgnore]
		public bool Cleared => _cleared ??= Records.All(r => !r.Failed);

		[JsonIgnore]
		public float TotalAccuracy => Records.Length == 0
			? 0
			: Records.Sum(record => record.XAccuracy);

		[JsonIgnore]
		public SerializableHitMargins TotalHitMargins => Records.Length == 0
			? new()
			: Records.Select(r => r.HitMargins).Aggregate((a, b) => a + b);

		[JsonIgnore]
		public int TotalFloors => Records.Length == 0
			? 0
			: Records.Sum(record => record.TotalFloors);

		public bool Equals(CoursePlayRecord other) => Equals(Records, other.Records);
		public override bool Equals(object obj) => obj is CoursePlayRecord other && Equals(other);
		public override int GetHashCode() => (Records != null ? Records.GetHashCode() : 0);
	}
}