using System;
using System.Collections.Generic;
using System.IO;
using ADOFAI;
using CourseMod.Utils;
using GDMiniJSON;

namespace CourseMod.DataModel {
	public record LevelMeta {
		public LevelMeta(string path) {
			if (File.Exists(path)) {
				var json = RDFile.ReadAllText(path);
				var levelDict = Json.Deserialize(json) as Dictionary<string, object>;
				var settings = (levelDict!["settings"] as Dictionary<string, object>)!;

				Artist = ((string) settings["artist"]).SanitizeForUI();
				Creator = ((string) settings["author"]).SanitizeForUI();
				Song = ((string) settings["song"]).SanitizeForUI();

				Tiles = levelDict.TryGetValue("pathData", out var value)
					? RDEditorUtils.DecodeString(value).Length
					: RDEditorUtils.DecodeFloatArray(levelDict["angleData"]).Length;
				Checksum = ChecksumTools.ComputeFileChecksum(path)?.Hash;
				try {
					GameplayChecksum = ChecksumTools.ComputeGameplayChecksum(levelDict)?.Hash;
				} catch (Exception e) {
					LogTools.LogException("Error computing gameplay checksum for level at " + path, e);
				}
			}
		}

		public readonly string Checksum;
		public readonly string GameplayChecksum;

		public readonly string Artist;
		public readonly string Creator;
		public readonly string Song;
		public readonly int Tiles;
	}
}