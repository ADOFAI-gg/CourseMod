using System;
using System.IO;
using System.Linq;
using CourseMod.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.Video;

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

		[JsonIgnore] public string AbsoluteFilePath;

		[JsonIgnore]
		public LevelMeta LevelMeta =>
			CourseCollection.LevelMetas.TryGetValue(Path, out var meta) ? meta : RefreshLevelMeta();

		public LevelMeta RefreshLevelMeta() {
			Debug.Log($"Attempting to read {AbsoluteFilePath}");
			var result = new LevelMeta(AbsoluteFilePath);

			Checksum = result.Checksum;
			GameplayChecksum = result.GameplayChecksum;

			return result;
		}

		[CanBeNull]
		private string GetAbsolutePathFor(string localPath) {
			if (string.IsNullOrEmpty(localPath))
				return null;

			var parent = System.IO.Path.GetDirectoryName(AbsoluteFilePath);
			return string.IsNullOrEmpty(parent)
				? localPath
				: System.IO.Path.Combine(parent, localPath);
		}

		[CanBeNull] public string GetCutsceneFileUrl() => GetAbsolutePathFor(CutsceneFile);
		[CanBeNull] public Sprite GetPreviewSprite() => ImageTools.OpenSprite(GetAbsolutePathFor(LevelMeta.PreviewImagePath));


		public static CourseLevel FromPath(string path, [CanBeNull] string relativeTo) {
			var relativePath = path;

			if (!string.IsNullOrEmpty(relativeTo)) {
				relativePath = System.IO.Path.GetRelativePath(relativeTo, path);
			}

			var result = new CourseLevel {
				Path = relativePath, AbsoluteFilePath = path
			};

			return result;
		}
	}
}