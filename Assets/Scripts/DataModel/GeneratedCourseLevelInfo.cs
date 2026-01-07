using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public record GeneratedCourseLevelInfo {
		public string Artist;
		public string Song;
		public string Creator;
		public int Tiles;
	}
}