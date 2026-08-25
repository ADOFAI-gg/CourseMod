using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public struct CourseSettings {
		[CanBeNull] public string BackgroundSpritePath;

		public int? CountdownSeconds;

		public float? AccuracyConstraint;
		public int? DeathConstraint;
		public int? LifeConstraint;
	}
}