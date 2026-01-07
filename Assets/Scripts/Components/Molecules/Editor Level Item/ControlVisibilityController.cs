using CourseMod.Utils;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.EditorLevelItem {
	public class ControlVisibilityController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
		private const float TransitionDuration = .2f;

		public CanvasGroup canvasGroup;

		private bool _stayVisible;
		private bool _hovered;

		private Tween _opacityTween;

		public void SetPersistingVisibility(bool visible) {
			_stayVisible = visible;
			UpdateVisibility();
		}

		private void UpdateVisibility() {
			float targetAlpha;

			if (_stayVisible || _hovered)
				targetAlpha = 1;
			else
				targetAlpha = 0;

			_opacityTween?.Kill();
			_opacityTween = DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, targetAlpha,
					TransitionDuration)
				.SetUpdate(true);
		}

		public void OnPointerEnter(PointerEventData _) {
			_hovered = true;
			UpdateVisibility();
		}

		public void OnPointerExit(PointerEventData _) {
			_hovered = false;
			UpdateVisibility();
		}
	}
}