using System.Collections.Generic;
using System.Threading.Tasks;
using Fluent.Net;
using JetBrains.Annotations;
using UnityEngine;

namespace CourseMod.Utils {
	public static class I18N {
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		private record I18NObject {
			public I18NObject(string langCode) {
				Context = new MessageContext(langCode, new MessageContextOptions { UseIsolating = false });

				Errors = Context.AddMessages(Assets.LoadAsset<TextAsset>($"Assets/Resources/{langCode}.ftl").text);

				if (Errors.Count > 0) {
					Debug.LogWarning("Following FTL parsing errors occurred:");

					foreach (var error in Errors)
						Debug.LogError(error);
				}
			}

			public readonly MessageContext Context;
			public readonly IList<ParseException> Errors;
		}

		private enum I18NLanguage {
			Korean,
			English
		}

		private static readonly Dictionary<I18NLanguage, I18NObject> I18NObjects = new();

		private static I18NLanguage _selectedLanguage;

#if UNITY_EDITOR
		static I18N() {
			Setup();
		}
#endif

		public static void Setup() {
			I18NObjects[I18NLanguage.Korean] = new I18NObject("ko-KR");
			I18NObjects[I18NLanguage.English] = new I18NObject("en-US");

			if (ADOBase.platform == Platform.None) {
				_selectedLanguage = I18NLanguage.English;
				Task.Yield().GetAwaiter().OnCompleted(SetupLanguage); // Delay 1 fps
			} else SetupLanguage();
		}

		public static void SetupLanguage() {
#if UNITY_EDITOR
			_selectedLanguage = Random.Range(0, 1) >= .5f ? I18NLanguage.Korean : I18NLanguage.English;
#else
			var usingKorean = Persistence.language == SystemLanguage.Korean;
			_selectedLanguage = usingKorean ? I18NLanguage.Korean : I18NLanguage.English;
#endif
		}

		public static string Get(string key, [CanBeNull] Dictionary<string, object> parameters = null) {
			if (!I18NObjects.TryGetValue(_selectedLanguage, out var obj)) {
#if UNITY_EDITOR
				Debug.LogWarning($"No such language {_selectedLanguage}");
#endif

				return key;
			}

			var message = obj.Context.GetMessage(key);

			if (message == null) {
#if UNITY_EDITOR
				Debug.LogWarning($"No such message {key}");
				Setup();
				if (!I18NObjects.TryGetValue(_selectedLanguage, out obj)) return key;
				message = obj.Context.GetMessage(key);
				if (message == null)
#endif

					return key;
			}

			return obj.Context.Format(message, parameters);
		}

		public static string GetFromGame(string key) {
#if UNITY_EDITOR
			return $"[adofai] {key}";
#else
			return RDString.Get(key);
#endif
		}

		public static bool IsKorean() {
			return _selectedLanguage == I18NLanguage.Korean;
		}
	}
}