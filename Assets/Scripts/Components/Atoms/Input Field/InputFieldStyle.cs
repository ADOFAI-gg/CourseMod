using System.Diagnostics;
using CourseMod.Utils;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Atoms.InputField {
	[RequireComponent(typeof(TMP_InputField))]
	public class InputFieldStyle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
		private const float TransitionDuration = .2f;

		public TMP_InputField inputField;
		private bool _hasSetup;

		private bool _focused;
		private bool _hovered;

		private Tween _imageTween;

		public bool Disabled {
			get => !inputField.interactable;
			set {
				inputField.interactable = !value;
				UpdateAppearance(false, false);
			}
		}

		private void Awake() {
			inputField.onSelect.AddListener(_ => SetFocused(true));
			inputField.onDeselect.AddListener(_ => SetFocused(false));

			_focused = inputField.isFocused;
			UpdateAppearance(true, false);

			return;

			void SetFocused(bool focused) {
				_focused = focused;
				UpdateAppearance();
			}
		}

		private void UpdateAppearance(bool skipTransition = false, bool assumeInteractable = true) {
			var tempColor = Color.white
				.SetAlpha(
					_focused
						? 1f
						: _hovered
							? .4f
							: .2f);

			if (skipTransition)
				inputField.image.color = tempColor;
			else {
				_imageTween?.Kill();
				_imageTween = inputField.image.DOColor(tempColor, TransitionDuration).SetUpdate(true).Done();
			}

			if (assumeInteractable) return;

			Color tempColor2;
			if (Disabled) {
				tempColor =
					tempColor2 = Color.white.SetAlpha(.1f);
			} else {
				tempColor = Color.white;
				tempColor2 = Color.white.SetAlpha(.6f);
			}

			inputField.textComponent.color = tempColor;
			inputField.placeholder.color = tempColor2;
		}

		public void OnPointerEnter(PointerEventData _) {
			_hovered = true;
			UpdateAppearance();
		}

		public void OnPointerExit(PointerEventData _) {
			_hovered = false;
			UpdateAppearance();
		}
	}
}