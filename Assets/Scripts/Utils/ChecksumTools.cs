using System;
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

		private static readonly HashSet<string> GameplayAffectingEvents = new() {
			"SetSpeed", "Twirl", "Pause", "ScaleMargin", "Hold", "FreeRoam", "FreeRoamTwirl", "FreeRoamRemove",
			"MultiPlanet", "AutoPlayTiles"
		};

		private static readonly HashSet<LevelEventType> GameplayAffectingEventsType = GameplayAffectingEvents.Select(Enum.Parse<LevelEventType>).ToHashSet();

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
			var sb = new StringBuilder();

			var settings = (levelJson["settings"] as Dictionary<string, object>)!;
			var angles = levelJson.GetValueOrDefault("angleData") is List<object> angleList
				             ? string.Join(",", angleList)
				             : "";
			sb.Append(angles).Append('\x1B');

			var rawActions = (levelJson["actions"] as List<object>)!;
			foreach (Dictionary<string, object> rawAction in rawActions) {
				var eventTypeStr = rawAction["eventType"].ToString();

				if (!GameplayAffectingEvents.Contains(eventTypeStr)) continue;
				var eventInfo = GCS.levelEventsInfo[eventTypeStr];
				
				// These events are unaffected.
				// var eventType = RDUtils.ParseEnum<LevelEventType>(eventTypeStr);
				//
				// if (eventType == LevelEventType.AddDecoration && !rawAction.ContainsKey("decorationImage")) {
				// 	rawAction["decorationImage"] = rawAction["decText"];
				// 	rawAction.Remove("decText");
				// }
				//
				// if (eventType is LevelEventType.AddDecoration or LevelEventType.AddText && !rawAction.ContainsKey("parallax")) {
				// 	var num = (int) rawAction["depth"];
				// 	num = num != 1 && num != -1 ? num : 0;
				// 	rawAction["parallax"] = num;
				// }
				//
				// if (eventType is LevelEventType.CustomBackground or LevelEventType.BackgroundSettings && !rawAction.ContainsKey("scalingRatio")) {
				// 	var flag = rawAction.TryGetValue("bgDisplayMode", out var value) && RDUtils.ParseEnum(value as string, BgDisplayMode.FitToScreen) == BgDisplayMode.Unscaled;
				// 	rawAction["scalingRatio"] = flag ? (int) rawAction["unscaledSize"] : 100;
				// 	rawAction.Remove("unscaledSize");
				// }
				//
				// if (eventType == LevelEventType.AddDecoration && rawAction.TryGetValue("failHitbox", out var value2)) {
				// 	var flag2 = value2 switch {
				// 		string text => text == "Enabled",
				// 		bool flag3 => flag3,
				// 		_ => false
				// 	};
				//
				// 	rawAction.TryAdd("hitbox", (flag2 ? HitboxType.Kill : HitboxType.None).ToString());
				// }
				// if (eventType == LevelEventType.ScalePlanets && rawAction.TryGetValue("targetPlanet", out var value3) && value3 is "Both")
				// 	rawAction["targetPlanet"] = "All";

				var active = (bool) rawAction.GetValueOrDefault("active", true);

				if (rawAction.TryGetValue("floor", out var floor)) // string.Join(",", x.Encode().Select(kvp => $"{kvp.Key}:{kvp.Value}")));
					sb.Append("floor").Append(':').Append(floor).Append(',');
				sb.Append("eventType").Append(':').Append(eventTypeStr).Append(',');
				if (!active)
					sb.Append("active:false,");
				if (rawAction.TryGetValue("visible", out var visible))
					sb.Append("visible").Append(':').Append(visible).Append(',');
				if (rawAction.TryGetValue("locked", out var locked))
					sb.Append("locked").Append(':').Append(locked).Append(',');

				foreach (var (key, propertyInfo) in eventInfo.propertiesInfo) {
					if (propertyInfo.pro) continue;
					if (key == "floor" || !propertyInfo.encode) continue;
					if (!rawAction.TryGetValue(key, out var obj10)) {
						if(propertyInfo.value_default == null) continue; // Compatibility with other mods
						sb.Append(key).Append(':').Append(GetValueString(propertyInfo.value_default)).Append(',');
					} else {
						if (propertyInfo.type == PropertyType.Bool && obj10 is string str2)
							obj10 = str2 == "Enabled";
						sb.Append(key).Append(':').Append(GetValueString(obj10)).Append(',');
					}
				}

				sb[^1] = '\xA7';
			}

			if (sb[^1] == '\xA7')
				sb.Length--;

			sb.Append('\x1B').Append(settings["bpm"]).Append('\x1B').Append(settings["pitch"]);
			return ComputeChecksum(sb.ToString());
		}

		public static ChecksumResult ComputeGameplayChecksum(LevelData levelData) {
			var angles = string.Join(',', levelData.angleData);

			var rawEvents = levelData.levelEvents.Where(x => GameplayAffectingEventsType.Contains(x.eventType))
				.Select(x => string.Join(',', x.Encode().Select(kvp => $"{kvp.Key}:{GetValueString(kvp.Value)}")));

			var events = string.Join('\xA7', rawEvents);

			var sum = string
				.Join("\x1B", angles, events, levelData.bpm, levelData.pitch);
			return ComputeChecksum(sum);
		}

		private static string GetValueString(object obj) {
			return obj switch {
				object[][] doubleArray => '[' + string.Join(',', doubleArray.Select(inner => '[' + string.Join(',', inner.Select(GetValueString)) + ']')) + ']',
				object[] array => '[' + string.Join(',', array.Select(GetValueString)) + ']',
				List<object> list => '[' + string.Join(',', list.Select(GetValueString)) + ']',
				Dictionary<string, object> dict => '{' + string.Join(',', dict.Select(kvp => $"{kvp.Key}:{GetValueString(kvp.Value)}")) + '}',
				_ => obj.ToString()
			};
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