using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace CourseMod.Editor {
	[JsonObject(MemberSerialization.OptIn)]
	public class ModInfo : ScriptableObject {
		private static ModInfo _info;

		public static ModInfo Info {
			get {
				if (_info)
					return _info;
				_info = AssetDatabase.LoadAssetAtPath<ModInfo>("Assets/Editor/Info.asset");
				if (_info) return _info;

				_info = CreateInstance<ModInfo>();
				AssetDatabase.CreateAsset(_info, "Assets/Editor/Info.asset");

				return _info;
			}
		}

#pragma warning disable format // @formatter:off
		[Header("Mod Entry")]
		[JsonProperty] public string AssemblyName;
		[JsonProperty] public string EntryMethod;

		[Header("Metadata")]
		[JsonProperty] public string Id;
		[JsonProperty] public string DisplayName;
		[JsonProperty] public string Author;
		[JsonProperty] public string Version;
		[JsonProperty] public string HomePage;
		[JsonProperty] public string Repository;
		[JsonProperty] public string ContentType;

		[Header("Version Dependency")]
		[JsonProperty] public string ManagerVersion;
		[JsonProperty] public string GameVersion;

		[Header("Dependency")]
		[JsonProperty] public string[] Requirements;
		[JsonProperty] public string[] LoadAfter;
#pragma warning restore format // @formatter:on

		public void WriteToFile(string path) {
			var json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
		}
	}
}