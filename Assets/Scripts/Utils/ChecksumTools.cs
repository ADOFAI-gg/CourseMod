using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ADOFAI;
using ADOFAI.Editor.Models;
using CourseMod.DataModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

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

				var eventType = RDUtils.ParseEnum<LevelEventType>(eventTypeStr);

				if (eventType == LevelEventType.AddDecoration && !rawAction.ContainsKey("decorationImage")) {
					rawAction["decorationImage"] = rawAction["decText"];
					rawAction.Remove("decText");
				}

				if (eventType is LevelEventType.AddDecoration or LevelEventType.AddText && !rawAction.ContainsKey("parallax")) {
					var num = (int) rawAction["depth"];
					num = num != 1 && num != -1 ? num : 0;
					rawAction["parallax"] = num;
				}

				if (eventType is LevelEventType.CustomBackground or LevelEventType.BackgroundSettings && !rawAction.ContainsKey("scalingRatio")) {
					var flag = rawAction.TryGetValue("bgDisplayMode", out var value) && RDUtils.ParseEnum(value as string, BgDisplayMode.FitToScreen) == BgDisplayMode.Unscaled;
					rawAction["scalingRatio"] = flag ? (int) rawAction["unscaledSize"] : 100;
					rawAction.Remove("unscaledSize");
				}

				if (eventType == LevelEventType.AddDecoration && rawAction.TryGetValue("failHitbox", out var value2)) {
					var flag2 = value2 switch {
						string text => text == "Enabled",
						bool flag3 => flag3,
						_ => false
					};

					rawAction.TryAdd("hitbox", (flag2 ? HitboxType.Kill : HitboxType.None).ToString());
				}
				if (eventType == LevelEventType.ScalePlanets && rawAction.TryGetValue("targetPlanet", out var value3) && value3 is "Both")
					rawAction["targetPlanet"] = "All";

				var active = (bool) rawAction.GetValueOrDefault("active", true);

				var count = eventInfo.propertiesInfo.Count(e => !e.Value.pro && e.Value.encode && e.Key != "floor");

				if (rawAction.TryGetValue("floor", out var floor))
					sb.Append(RDEditorUtils.EncodeInt("floor", (int) floor));
				sb.Append(RDEditorUtils.EncodeString("eventType", eventTypeStr, count == 0 && active));
				if (!active)
					sb.Append(RDEditorUtils.EncodeBool("active", active, count == 0));
				if (rawAction.TryGetValue("visible", out var visible))
					sb.Append(RDEditorUtils.EncodeBool("visible", (bool) visible, count == 0));
				if (rawAction.TryGetValue("locked", out var locked))
					sb.Append(RDEditorUtils.EncodeBool("locked", (bool) locked, count == 0));

				foreach (var (key, propertyInfo) in eventInfo.propertiesInfo) {
					if (propertyInfo.pro) continue;
					if (key != "floor" && propertyInfo.encode) {
						if (!rawAction.TryGetValue(key, out var obj10)) {
							var value = propertyInfo.value_default;
							switch (propertyInfo.type) {
								case PropertyType.Bool:
									sb.Append(RDEditorUtils.EncodeBool(key, (bool) value));
									break;
								case PropertyType.Int:
								case PropertyType.Rating:
									sb.Append(RDEditorUtils.EncodeInt(key, (int) value));
									break;
								case PropertyType.Float:
									sb.Append(RDEditorUtils.EncodeFloat(key, Convert.ToSingle(value)));
									break;
								case PropertyType.String:
								case PropertyType.LongString:
								case PropertyType.File:
									sb.Append(RDEditorUtils.EncodeString(key, LevelEvent.EscapeTextForJSON((string) value)));
									break;
								case PropertyType.Color:
									sb.Append(RDEditorUtils.EncodeString(key, (string) value));
									break;
								case PropertyType.Enum:
									sb.Append(RDEditorUtils.EncodeString(key, value.ToString()));
									break;
								case PropertyType.Vector2:
									sb.Append(RDEditorUtils.EncodeVector2(key, (Vector2) value));
									break;
								case PropertyType.Tile:
									sb.Append(RDEditorUtils.EncodeTile(key, value as Tuple<int, TileRelativeTo>));
									break;
								case PropertyType.Export:
								case PropertyType.ParticlePlayback:
									break;
								case PropertyType.Array:
									sb.Append(RDEditorUtils.EncodeModsArray(key, (object[]) value));
									break;
								case PropertyType.FloatPair:
									sb.Append(RDEditorUtils.EncodeFloatPair(key, value as Tuple<float, float>));
									break;
								case PropertyType.Vector2Range:
									sb.Append(RDEditorUtils.EncodeVector2Range(key, value as Tuple<Vector2, Vector2>));
									break;
								case PropertyType.MinMaxGradient:
									sb.Append(RDEditorUtils.EncodeRaw(key, ((SerializedMinMaxGradient) value).Encode()));
									break;
								case PropertyType.FilterProperties:
									sb.Append(RDEditorUtils.EncodeFilterProperties(key, GDMiniJSON.Json.Deserialize($"{{{value}}}") as Dictionary<string, object>, new Dictionary<string, bool>()));
									break;
								default:
									Debug.LogWarning($"{key} not parsed! it is type: {propertyInfo.type.ToString()}");
									break;
							}
						} else {
							switch (propertyInfo.type) {
								case PropertyType.Enum:
									sb.Append(RDEditorUtils.EncodeString(key, (obj10 is int num1 ? Enum.ToObject(propertyInfo.enumType, num1) : Enum.Parse(propertyInfo.enumType, obj10.ToString())).ToString()));
									break;
								case PropertyType.Bool:
									if (obj10 is string str3)
										obj10 = str3 == "Enabled";
									sb.Append(RDEditorUtils.EncodeBool(key, (bool) obj10));
									break;
								case PropertyType.Float:
									sb.Append(RDEditorUtils.EncodeFloat(key, Convert.ToSingle(obj10)));
									break;
								case PropertyType.Int or PropertyType.Rating:
									sb.Append(RDEditorUtils.EncodeInt(key, Convert.ToInt32(obj10)));
									break;
								case PropertyType.Vector2:
									Vector2 vector2;
									switch (obj10) {
										case float num2:
											vector2 = new Vector2(num2, num2);
											break;
										case int num3:
											vector2 = new Vector2(num3, num3);
											break;
										default:
											var objectList1 = obj10 as List<object>;
											var single1 = Convert.ToSingle(objectList1[0] ?? float.NaN);
											var single2 = Convert.ToSingle(objectList1[1] ?? float.NaN);
											vector2 = new Vector2(single1, single2);
											break;
									}
									sb.Append(RDEditorUtils.EncodeVector2(key, vector2));
									break;
								case PropertyType.Tile:
									var objectList2 = obj10 as List<object>;
									var int32 = Convert.ToInt32(objectList2[0]);
									var tileRelativeTo = (TileRelativeTo) Enum.Parse(typeof(TileRelativeTo), objectList2[1].ToString());
									sb.Append(RDEditorUtils.EncodeTile(key, new Tuple<int, TileRelativeTo>(int32, tileRelativeTo)));
									break;
								case PropertyType.Array:
									sb.Append(RDEditorUtils.EncodeModsArray(key, RDEditorUtils.DecodeModsArray(obj10)));
									break;
								case PropertyType.FilterProperties:
									sb.Append(RDEditorUtils.EncodeFilterProperties(key, GDMiniJSON.Json.Deserialize($"{{{obj10}}}") as Dictionary<string, object>, new Dictionary<string, bool>()));
									break;
								case PropertyType.FloatPair:
									var objectList4 = obj10 as List<object>;
									var single3 = Convert.ToSingle(objectList4[0] ?? float.NaN);
									var single4 = Convert.ToSingle(objectList4[1] ?? float.NaN);
									sb.Append(RDEditorUtils.EncodeFloatPair(key, new Tuple<float, float>(single3, single4)));
									break;
								case PropertyType.MinMaxGradient:
									sb.Append(RDEditorUtils.EncodeRaw(key, obj10.ToString()));
									break;
								case PropertyType.Vector2Range:
									var objectList5 = obj10 as List<object>;
									var objectList6 = (List<object>) objectList5[0];
									var objectList7 = (List<object>) objectList5[1];
									var vector2_1 = new Vector2(Convert.ToSingle(objectList6[0] ?? float.NaN), Convert.ToSingle(objectList6[1] ?? float.NaN));
									var vector2_2 = new Vector2(Convert.ToSingle(objectList7[0] ?? float.NaN), Convert.ToSingle(objectList7[1] ?? float.NaN));
									sb.Append(RDEditorUtils.EncodeVector2Range(key, new Tuple<Vector2, Vector2>(vector2_1, vector2_2)));
									break;
								case PropertyType.String:
								case PropertyType.LongString:
								case PropertyType.File:
									sb.Append(RDEditorUtils.EncodeString(key, LevelEvent.EscapeTextForJSON(obj10.ToString())));
									break;
								case PropertyType.Color:
									sb.Append(RDEditorUtils.EncodeString(key, obj10.ToString()));
									break;
								case PropertyType.Export:
								case PropertyType.ParticlePlayback:
									break;
								default:
									Debug.LogWarning($"{key} not parsed! it is type: {propertyInfo.type.ToString()}");
									break;
							}
						}
					}
				}

				if (sb[^1] == ' ' && sb[^2] == ',')
					sb.Length -= 2;

				sb.Append(',');
			}

			if (sb[^1] == ',')
				sb.Length--;

			sb.Append('\x1B').Append(settings["bpm"]).Append('\x1B').Append(settings["pitch"]).Replace("\t", "");
			return ComputeChecksum(sb.ToString());
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