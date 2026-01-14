using System;
using System.IO;
using CourseMod.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public struct CourseLevel {
		public string Path;
		public string Checksum;
		public string GameplayChecksum;

		public bool Mysterious;

		public bool DisableAccuracyConstraint;
		public bool DisableDeathConstraint;
		public bool DisableLifeConstraint;

		[CanBeNull] public string CutsceneFile;

		[JsonIgnore] public string AbsolutePath;

		[JsonIgnore]
		public LevelMeta LevelMeta =>
			CourseCollection.LevelMetas.TryGetValue(Path, out var meta) ? meta : RefreshLevelMeta();

		public LevelMeta RefreshLevelMeta() {
			Debug.Log($"Attempting to read {AbsolutePath}");
			var result = new LevelMeta(AbsolutePath);

			Checksum = result.Checksum;
			GameplayChecksum = result.GameplayChecksum;

			return result;
		}


		public static CourseLevel FromPath(string path, [CanBeNull] string relativeTo) {
			var relativePath = path;

			if (!string.IsNullOrEmpty(relativeTo)) {
				relativePath = System.IO.Path.GetRelativePath(relativeTo, path);
			}

			var result = new CourseLevel {
				Path = relativePath, AbsolutePath = path
			};

			return result;
		}
	}
}