using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Plastic.Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace CourseMod.Editor {
	[JsonObject]
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

		[Header("Mod Entry")] public string AssemblyName;
		public string EntryMethod;

		[Header("Metadata")] public string Id;
		public string DisplayName;
		public string Author;
		public string Version;
		public string HomePage;
		public string Repository;
		public string ContentType;

		[Header("Version Dependency")] public string ManagerVersion;
		public string GameVersion;

		[Header("Dependency")] public string[] Requirements;
		public string[] LoadAfter;

		public void WriteToFile(string path) {
			var json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
		}
	}
}