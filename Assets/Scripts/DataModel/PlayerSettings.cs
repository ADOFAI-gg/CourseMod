using System;
using System.IO;
using CourseMod.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public class PlayerSettings {
		public const string FILE_NAME = "PlayerSettings.json";

		public bool UseNoFail = true;
		public string LastOpenedMainFilePath;

		public DateTime LastCopyrightNoticeCloseTime;
		public string LastOpenedEditorFilePath;

#if DEBUG
		[JsonIgnore] public DebugSettings DebugSettings => _debugSettings ??= new();
		[JsonProperty("debugSettings")] private DebugSettings _debugSettings;
#endif

		public void Save() {
			var dir = Path.GetDirectoryName(ModDataStorage.PlayerSettingsPath);

			if (!Directory.Exists(dir) && !string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);

			File.WriteAllText(ModDataStorage.PlayerSettingsPath,
				JsonConvert.SerializeObject(this, Formatting.Indented));
		}
	}
}