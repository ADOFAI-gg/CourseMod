using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CourseMod.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Atoms.InputField {
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_InputField))]
	[RequireComponent(typeof(LayoutElement))]
	public class InputFieldVerticalSizeIncrementer : MonoBehaviour {
		public float trueHeightPerLine = 24;
		public float desiredHeightPerLine = 26;

		private TMP_InputField _field;
		private LayoutElement _layout;
		private string _previousText;

#if UNITY_EDITOR
		private string _lastEditorFieldTextContent;
#endif

		private void Awake() {
			_field = GetComponent<TMP_InputField>();
			_field.onValueChanged.AddListener(OnFieldValueChanged);

			_layout = GetComponent<LayoutElement>();
		}

		private void OnFieldValueChanged(string value) {
			_layout.preferredHeight = desiredHeightPerLine;

			var targetGraphic = string.IsNullOrEmpty(value)
				? _field.placeholder
				: _field.textComponent;

			if (targetGraphic is TMP_Text text)
				text.enableWordWrapping = true;
			Task.Yield().GetAwaiter().OnCompleted(() => {
				try {
					var height = Mathf.RoundToInt(
						Mathf.FloorToInt(LayoutUtility.GetPreferredHeight(targetGraphic.rectTransform)) /
						trueHeightPerLine) * desiredHeightPerLine;

					if (height > desiredHeightPerLine * 6) _field.text = _previousText;
					else _previousText = value;
					_layout.preferredHeight = Mathf.Max(desiredHeightPerLine, height);
				} catch (Exception e) {
					LogTools.LogException("Error on FieldValueChanged", e);
				}
			});
		}

#if UNITY_EDITOR
		private void Update() {
			if (!Application.isPlaying && _lastEditorFieldTextContent != _field.text)
				OnFieldValueChanged(_lastEditorFieldTextContent = _field.text);
		}
#endif
	}
}