using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CourseMod.Utils;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.DragHandle {
	public class DragHandleStyle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
		IPointerUpHandler {
		private enum DragHandleState {
			Default,
			Hover,
			Active
		}

		private record StyleData {
			public Color BackgroundColor;
			public Color DotColor;
		}

		private static readonly IReadOnlyDictionary<DragHandleState, StyleData> Styles =
			new Dictionary<DragHandleState, StyleData> {
				[DragHandleState.Default] =
					new() { BackgroundColor = Color.white.SetAlpha(0), DotColor = Color.white.SetAlpha(.2f) },
				[DragHandleState.Hover] =
					new() { BackgroundColor = Color.white.SetAlpha(.1f), DotColor = Color.white.SetAlpha(.4f) },
				[DragHandleState.Active] =
					new() { BackgroundColor = Color.white.SetAlpha(.2f), DotColor = Color.white.SetAlpha(.6f) },
			}.ToImmutableDictionary();

		private const float TransitionDuration = .2f;

		public Image background;
		public Image[] dots;

		private bool _hovered;
		private bool _active;

		private DragHandleState _lastState;

		private Tween _backgroundTween;
		private Tween[] _dotTweens;

		private void UpdateHandleAppearance(bool skipTransition = true) {
			var state = _active
				? DragHandleState.Active
				: _hovered
					? DragHandleState.Hover
					: DragHandleState.Default;

			if (_lastState != state) {
				ChangeHandleAppearance(state, skipTransition);
			}
		}

		private void ChangeHandleAppearance(DragHandleState state, bool skipTransition) {
			if (!Styles.TryGetValue(state, out var style)) {
				Debug.LogWarning(
					$"Handle Style Type '{state}' does not exist. (From object {name}/{GetInstanceID()})");
				return;
			}

			if (skipTransition)
				background.color = style.BackgroundColor;
			else {
				_backgroundTween?.Kill();
				_backgroundTween = DOTween.To(() => style.BackgroundColor, c => background.color = c,
					style.BackgroundColor, TransitionDuration).SetUpdate(true).Done();
			}

			if (skipTransition)
				dots?.ForEach(i => i.color = style.DotColor);
			else {
				_dotTweens?.ForEach(t => t.Kill());
				_dotTweens = dots?.Select(i => DOTween.To(() => i.color, c => i.color = c,
					style.DotColor, TransitionDuration).SetUpdate(true).Done()).ToArray<Tween>();
			}

			_lastState = state;
		}

		public void OnPointerEnter(PointerEventData _) {
			_hovered = true;
			UpdateHandleAppearance();
		}

		public void OnPointerExit(PointerEventData _) {
			_hovered = false;
			UpdateHandleAppearance();
		}

		public void OnPointerDown(PointerEventData _) {
			_active = true;
			UpdateHandleAppearance();
		}

		public void OnPointerUp(PointerEventData _) {
			_active = false;
			UpdateHandleAppearance();
		}
	}
}