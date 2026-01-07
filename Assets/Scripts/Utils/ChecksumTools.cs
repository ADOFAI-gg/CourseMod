using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ADOFAI;
using CourseMod.DataModel;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace CourseMod.Utils {
	public static class ChecksumTools {
		public record ChecksumResult {
			public ChecksumResult(string content, string hash, string hashMethod) {
				Content = content;
				Hash = hash;
				HashMethod = hashMethod;
			}

			public readonly string Content;
			public readonly string Hash;
			public readonly string HashMethod;

			public override string ToString() => ToString(false);

			public string ToString(bool includeOriginalContent) {
				StringBuilder sb = new("<ChecksumTools.ChecksumResult>\n");

				if (includeOriginalContent) {
					sb.Append("Original Content: ")
						.Append(Content)
						.AppendLine();
				}

				sb.Append("Hash: ")
					.Append(Hash)
					.AppendLine()
					.Append("Hash Method: ")
					.Append(HashMethod)
					.AppendLine();

				return sb.ToString();
			}
		}

		private static readonly string[] GameplayAffectingEvents = {
			"SetSpeed", "Twirl", "Pause", "ScaleMargin", "Hold", "FreeRoam", "FreeRoamTwirl", "FreeRoamRemove",
			"MultiPlanet", "AutoPlayTiles"
		};

		private const string ContentHashMethod = "sha256";

		[CanBeNull]
		public static ChecksumResult ComputeFileChecksum(string filePath) {
			if (!File.Exists(filePath))
				return null;

			using var stream = File.OpenRead(filePath);
			using var sha256 = System.Security.Cryptography.SHA256.Create();
			var hashBytes = sha256.ComputeHash(stream);
			var hash = System.BitConverter
				.ToString(hashBytes)
				.Replace("-", "")
				.ToLowerInvariant();

			return new(hashBytes.ToString(), hash, ContentHashMethod);
		}

		public static ChecksumResult ComputeChecksum(string content) {
			using var sha256 = System.Security.Cryptography.SHA256.Create();
			var hashBytes = sha256.ComputeHash(Encoding.Unicode.GetBytes(content));
			var hash = System.BitConverter
				.ToString(hashBytes)
				.Replace("-", "")
				.ToLowerInvariant();

			return new(content, hash, ContentHashMethod);
		}

		[CanBeNull]
		public static ChecksumResult ComputeGameplayChecksum(Dictionary<string, object> levelJson) {
			var settings = (levelJson["settings"] as Dictionary<string, object>)!;
			var angles = levelJson.GetValueOrDefault("angleData") is List<object> angleList
				? string.Join(",", angleList)
				: "";
			var rawActions = (levelJson["actions"] as List<object>)!;

			var events = string.Join(",", rawActions.Select(x => new LevelEvent(x as Dictionary<string, object>))
				.Where(x => GameplayAffectingEvents.Contains(x.eventType.ToString())).Select(x => x.Encode()));

			var sum = string.Join("\x1B", angles, events, settings["bpm"], settings["pitch"]).Replace("\t", "");
			return ComputeChecksum(sum);
		}

		public static ChecksumResult ComputeGameplayChecksum(LevelData levelData) {
			var angles = string.Join(",", levelData.angleData);
			var events = string.Join(",",
				levelData.levelEvents.Where(x => GameplayAffectingEvents.Contains(x.eventType.ToString()))
					.Select(x => x.Encode()));

			var sum = string
				.Join("\x1B", angles, events, levelData.songSettings["bpm"], levelData.songSettings["pitch"])
				.Replace("\t", "");
			return ComputeChecksum(sum);
		}

		public static ChecksumResult ComputeCourseChecksum(Course course) {
			var settingsExceptThumbnail = JsonConvert.SerializeObject(course.Settings);

			if (course.Settings.ThumbnailFile is { } thumbnailFile)
				settingsExceptThumbnail = settingsExceptThumbnail.Replace($"\"{thumbnailFile}\"", "null");

			var sum = string.Join("\x1B", JsonConvert.SerializeObject(course.Levels), settingsExceptThumbnail);
			return ComputeChecksum(sum);
		}
	}
}