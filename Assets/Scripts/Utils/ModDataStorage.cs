using System.Collections.Generic;
using System.IO;
using CourseMod.DataModel;
using Newtonsoft.Json;
using UnityEngine;

namespace CourseMod.Utils {
	public static class ModDataStorage {
		public static PlayerSettings PlayerSettings {
			get {
				if (_playerSettings is { } result)
					return result;

				if (File.Exists(PlayerSettingsPath))
					return _playerSettings =
						JsonConvert.DeserializeObject<PlayerSettings>(File.ReadAllText(PlayerSettingsPath));

				_playerSettings = new();
				_playerSettings.Save();

				return _playerSettings;
			}
		}

		private static PlayerSettings _playerSettings;

		public static string PlayerSettingsPath => Path.Combine(CourseModDirectory, PlayerSettings.FILE_NAME);
		public static string CourseDirectory => Path.Combine(CourseModDirectory, "Courses");
		public static string CourseRecordDirectory => Path.Combine(CourseModDirectory, "Records");

		private static string CourseModDirectory => Path.Combine(Application.persistentDataPath, "CourseMod");
	}
}