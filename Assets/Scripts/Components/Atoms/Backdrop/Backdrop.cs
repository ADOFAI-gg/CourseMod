using System;
using CourseMod.Components.Molecules.ContextMenu;
using CourseMod.Components.Scenes;
using CourseMod.Utils;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CourseMod.Components.Atoms.Backdrop {
	public class Backdrop : MonoBehaviour {
		public class BackdropEndEvent : UnityEvent {
		}

		public Image image;
		public UnityEngine.UI.Button button;

		[NonSerialized] private BackdropSettings _backdropSettings;

		public readonly BackdropEndEvent OnBackdropEnd = new();

		private Tween _tween;


		public void ResetBackdrop(BackdropSettings backdropSettings) {
			_backdropSettings = backdropSettings;
			image.color = image.color.SetAlpha(backdropSettings.Opacity);
		}

		private void Awake() {
			button.onClick.AddListener(() => {
				if (!_backdropSettings.AllowBackdropClickToEnd) return;
				End();
			});
		}

		public void Start() {
			_tween?.Kill();

			gameObject.SetActive(true);

			_tween = DOTween
				.To(() => image.color.a, x => image.color = image.color.SetAlpha(x), _backdropSettings.Opacity,
					_backdropSettings.TransitionDuration)
				.OnComplete(() => {
					_tween.Kill();
					_tween = null;
				}).SetUpdate(true).Done();
		}

		public void End() {
			_tween?.Kill();
			_tween = DOTween
				.To(() => image.color.a, x => image.color = image.color.SetAlpha(x), 0,
					_backdropSettings.TransitionDuration)
				.OnComplete(() => {
					_tween.Kill();
					_tween = null;

					gameObject.SetActive(false);

					OnBackdropEnd.Invoke();
				}).SetUpdate(true).Done();
		}


		private void Update() {
			if (_backdropSettings.UseKeyboardControl) {
				UpdateKeyboardControl();
			}

			//Do something...
		}

		private void UpdateKeyboardControl() {
			EventSystem eventSystem = EventSystem.current;
			GameObject selectedGameObject = eventSystem?.currentSelectedGameObject;

			if (selectedGameObject)
				return;

			if (Input.GetKeyDown(KeyCode.Escape))
				End();
		}
	}
}