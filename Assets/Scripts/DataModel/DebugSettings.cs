using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public class DebugSettings {
		public string CoursePath;
		public bool DisableNoFail;
		public bool ForceEnableTransitionSidebarMenu;
	}
}