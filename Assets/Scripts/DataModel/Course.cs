using System.Collections.Generic;
using System.IO;
using System.Linq;
using CourseMod.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), ItemNullValueHandling = NullValueHandling.Ignore)]
	public struct Course {
		public string Name;
		public string Description;
		public string Creator;

		public CourseSettings Settings;

		public List<CourseLevel> Levels;
		[CanBeNull] public List<GeneratedCourseLevelInfo> ReadonlyLevelsInfo;

		[JsonIgnore] public string FilePath;
		[JsonIgnore] public static Course Default => _default;
		[JsonIgnore] private static Course _default;

		[JsonIgnore] public string Id => ComputeId();

		// TODO this method of obtaining ID is exceedingly fragile to changes made in hashing business logic
		private string ComputeId() {
			var metaSum = string.Join("\x1B", Name, Description, Creator);
			var levelSum = string.Join("\x1B", Levels.Select(l => l.GameplayChecksum));

			return ChecksumTools.ComputeChecksum(Levels.Count > 0 ? levelSum : metaSum).Hash;
		}

		[CanBeNull]
		public CoursePlayRecord GetPlayRecord() {
			if (CourseCollection.CourseRecords.TryGetValue(Id, out var record))
				return record;
			var recordPath = GetPlayRecordPath();

			if (!File.Exists(recordPath))
				return null;

			return JsonConvert.DeserializeObject<CoursePlayRecord>(File.ReadAllText(recordPath));
		}

		public string GetPlayRecordPath() {
			var recordsPath = ModDataStorage.CourseRecordDirectory;

			if (!Directory.Exists(recordsPath))
				Directory.CreateDirectory(recordsPath);

			return Path.Combine(recordsPath, Id);
		}

		public string GetDefaultExportFilename() =>
			$"{string.Join("_", Name.Split(Path.GetInvalidFileNameChars()))}";

		public void GenerateReadonlyLevelsInfo() {
			ReadonlyLevelsInfo = (from level in Levels
				let meta = level.LevelMeta
				select new GeneratedCourseLevelInfo {
					Artist = meta.Artist, Song = meta.Song, Creator = meta.Creator, Tiles = meta.Tiles
				}).ToList();
		}

		static Course() {
			_default = new Course {
				Levels = new List<CourseLevel>(),
				Name = "",
				Description = "",
				Creator = "",
				Settings = new CourseSettings()
			};
		}
	}
}