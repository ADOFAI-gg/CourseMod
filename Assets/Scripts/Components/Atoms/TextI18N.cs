using System;
using System.Collections.Generic;
using CourseMod.Utils;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace CourseMod.Components.Atoms {
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class TextI18N : MonoBehaviour {
		public enum I18NArgumentPreset {
			None,
			DeathDescription,
			LifeDescription,
			MissingLevelNumber
		}

		public string key;
		public I18NArgumentPreset argumentPreset;

		public Dictionary<string, object> Arguments;

		private TextMeshProUGUI _text;

		private void Awake() {
			Setup();
			UpdateString();
		}

#if UNITY_EDITOR
		private void OnValidate() => Awake();
#endif

		private void Setup() {
			_text = GetComponent<TextMeshProUGUI>();
		}

		[UsedImplicitly]
		public void UpdateString() {
			if (string.IsNullOrEmpty(key))
				return;

			if (!_text)
				Awake();

			_text.text = I18N.Get(key, Arguments ?? ObtainArgs());
		}

		public void UpdateArguments(Dictionary<string, object> args) {
			Arguments = args;
			UpdateString();
		}

		private Dictionary<string, object> ObtainArgs() {
			return argumentPreset switch {
				I18NArgumentPreset.DeathDescription => new() {
					["miss"] = I18N.GetFromGame("HitMargin.FailMiss").WrapColorTag("da59ff"),
					["overload"] = I18N.GetFromGame("HitMargin.FailOverload").WrapColorTag("da59ff")
				},
				I18NArgumentPreset.LifeDescription => new() {
					["perfect"] = I18N.GetFromGame("HitMargin.Perfect").WrapColorTag("5fff4e")
				},
				I18NArgumentPreset.MissingLevelNumber => new() { ["count"] = 1 },
				_ => null
			};
		}
	}
}