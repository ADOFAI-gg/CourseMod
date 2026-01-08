using System;
using System.Reflection;
using UnityEngine;

namespace CourseMod.Utils {
	public static class Assets {
		private static AssetBundle Bundle { get; set; }

		public static void SetAssetBundle(AssetBundle bundle) {
			Bundle = bundle;
		}

		public static T LoadAsset<T>(string path) where T : UnityEngine.Object {
#if UNITY_EDITOR
			if (Application.isEditor)
				return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#endif
			if (!Bundle)
				throw new Exception("AssetBundle is not set");
			T asset = Bundle.LoadAsset<T>(path);
			return !asset ? throw new ArgumentException($"No asset found at \"{path}\"") : asset;
		}
	}
}