using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ADOFAI;
using CourseMod.DataModel;
using GDMiniJSON;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace CourseMod.Utils {
	public static class ExportTools {
		private static readonly LevelEventType[] LevelEventTypesWithFileRefs = {
			LevelEventType.CustomBackground,
			LevelEventType.ColorTrack,
			// LevelEventType.RecolorTrack,
			LevelEventType.MoveDecorations,
			LevelEventType.AddDecoration,
			LevelEventType.AddParticle
		};

		private static readonly Dictionary<string, LevelEventType> LevelEventTypeNameToTypeWithFileRefs =
			LevelEventTypesWithFileRefs.ToDictionary(type => type.ToString(), type => type);
		
		private static string[] GetLevelFiles(string path) {
			var fileContent = File.ReadAllText(path);
			var lenientlyDeserializedObject = Json.Deserialize(fileContent);

			if (lenientlyDeserializedObject == null)
				goto return_empty;
					
			var rootObject = JObject.FromObject(lenientlyDeserializedObject);
			var settingsObject = rootObject[EditorConstants.key_settings];
			if (settingsObject == null)
				goto return_empty;
					
			if (!rootObject.TryGetValueAs(EditorConstants.key_actions, out JArray actionsArray))
				goto return_empty;

			if (!rootObject.TryGetValueAs(EditorConstants.key_decorations, out JArray decorationsArray))
				decorationsArray = new();

			var fileRefsFromLevelEvents = actionsArray.Concat(decorationsArray)
				.Select(GetFileRefOrNull);

			HashSet<string> fileRefs = new(fileRefsFromLevelEvents) {
				settingsObject[EditorConstants.key_previewIcon]?.Value<string>(),
				settingsObject[EditorConstants.key_previewImage]?.Value<string>(),
				settingsObject[EditorConstants.key_artistPermission]?.Value<string>(),
				settingsObject[EditorConstants.key_bgImage]?.Value<string>(),
				settingsObject[EditorConstants.key_songFilename]?.Value<string>(),
				settingsObject[EditorConstants.key_bgVideo]?.Value<string>(),
			};
					
			fileRefs.Remove(null);

			var parentDirectory = Path.GetDirectoryName(path);
			
			return fileRefs
				.Select(f => StringTools.CombinePathNullable(parentDirectory, f))
				.Concat(new[] {path})
				.ToArray();
			
			return_empty:
			return Array.Empty<string>();

			bool IsTypeAndNonNull([CanBeNull] JToken token, JTokenType tokenType) =>
				token is { HasValues: true } && token.Type == tokenType;

			bool TryGetEventType([CanBeNull] JToken levelEventToken, [CanBeNull] out string eventType) {
				eventType = null;
				
				if (!IsTypeAndNonNull(levelEventToken, JTokenType.Object))
					return false;

				var eventTypeToken = levelEventToken![EditorConstants.key_eventType];
				if (!IsTypeAndNonNull(eventTypeToken, JTokenType.String))
					return false;

				eventType = eventTypeToken.Value<string>();
				return true;
			}

			string GetEventPropertyOrNull(JToken levelEventToken, string key) {
				var propertyToken = levelEventToken[key];

				if (!IsTypeAndNonNull(propertyToken, JTokenType.String))
					return null;
				
				return propertyToken.Value<string>();
			}
			
			string GetFileRefOrNull([CanBeNull] JToken levelEventToken) {
				const string decorationImageKey = "decorationImage";
				if (!TryGetEventType(levelEventToken, out var eventTypeString) || eventTypeString == null)
					return null;

				if (!LevelEventTypeNameToTypeWithFileRefs.TryGetValue(eventTypeString, out var eventType))
					return null;

				return eventType switch {
					LevelEventType.CustomBackground => GetEventPropertyOrNull(levelEventToken, EditorConstants.key_bgImage),
					LevelEventType.ColorTrack => GetEventPropertyOrNull(levelEventToken, EditorConstants.key_trackTexture),
					// LevelEventType.RecolorTrack => GetEventPropertyOrNull(levelEventToken, EditorConstants.key_trackTexture),
					LevelEventType.MoveDecorations => GetEventPropertyOrNull(levelEventToken, decorationImageKey),
					LevelEventType.AddDecoration => GetEventPropertyOrNull(levelEventToken, decorationImageKey),
					LevelEventType.AddParticle => GetEventPropertyOrNull(levelEventToken, decorationImageKey),
					_ => null
				};
			}
		}

		public static void Zip([NotNull] string filename, Course course, [NotNull] string courseDirectory, [CanBeNull] string thumbnailPath) {
			var files = new HashSet<string> {
				course.FilePath
			};

			if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
				files.Add(thumbnailPath);

			foreach(var level in course.Levels) 
				foreach(var file in GetLevelFiles(level.AbsolutePath)) 
					files.Add(file);

			using var stream = new FileStream(Path.Combine(courseDirectory, filename + ".zip"), FileMode.Create);
			using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

			foreach (var file in files)
				archive.CreateEntryFromFile(file, GetTargetFileName(courseDirectory, filename, file));
		}

		public static void CopyFiles([CanBeNull] string targetDirectory, [NotNull] string coursePath) {
			var course = CourseCollection.ReadSingleCourse(coursePath);
			var files = new HashSet<string> {
				course.FilePath
			};

			var courseDirectory = Path.GetDirectoryName(coursePath);
			var relativeThumbnailPath = course.Settings.ThumbnailFile;

			if (!string.IsNullOrEmpty(relativeThumbnailPath)) {
				var thumbnailPath = StringTools.CombinePathNullable(courseDirectory, course.Settings.ThumbnailFile);

				if (File.Exists(thumbnailPath))
					files.Add(thumbnailPath);
			}

			foreach(var level in course.Levels) 
				foreach(var file in GetLevelFiles(level.AbsolutePath)) 
					files.Add(file);

			foreach (var file in files) {
				var dist = GetTargetFileName(courseDirectory, targetDirectory, file);
				File.Copy(file, dist, true);
			}
		}
		
		[NotNull]
		private static string GetTargetFileName([CanBeNull] string relativeRoot, [CanBeNull] string parentDirectory, [NotNull] string file) {
			var targetFileName = Path.GetFileName(file);

			if (targetFileName.EndsWith(".course", StringComparison.OrdinalIgnoreCase))
				file = StringTools.CombinePathNullable(Path.GetDirectoryName(file), "main.course");

			return StringTools.CombinePathNullable(parentDirectory, Path.GetRelativePath(relativeRoot, file));
		}
	}
}