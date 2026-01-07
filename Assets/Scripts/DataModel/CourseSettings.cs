using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public struct CourseSettings {
		[CanBeNull] public string ThumbnailFile;

		public int? CountdownSeconds;

		public double? AccuracyConstraint;
		public int? DeathConstraint;
		public int? LifeConstraint;
	}
}