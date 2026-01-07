using System;
using CourseMod.Components.Atoms.Backdrop;
using CourseMod.Components.Atoms.Button;
using CourseMod.Components.Molecules.ContextMenu;
using CourseMod.Components.Scenes;
using UnityEngine;
using UnityEngine.Events;

namespace CourseMod.Components.Molecules.Popup {
	public class Popup : BackdropContainer {
		public enum PopupActionType {
			None,
			Cancel,
			Confirm,
			Save,
			Discard,
		}

		public class PopupStateChangeEvent : UnityEvent<bool> {
		}

		public class PopupActionEvent : UnityEvent<PopupActionType> {
		}

		public PopupActionType backdropActionValue = PopupActionType.Cancel;
		public ButtonStyle[] noneActionButtons;
		public ButtonStyle[] cancelActionButtons;
		public ButtonStyle[] confirmActionButtons;
		public ButtonStyle[] saveActionButtons;
		public ButtonStyle[] discardActionButtons;

		public readonly PopupActionEvent OnceAction = new();
		public readonly PopupActionEvent OnAction = new();

		public readonly PopupStateChangeEvent OnPopupStateChanged = new();

		private void Awake() {
			foreach (var b in noneActionButtons)
				b.button.onClick.AddListener(() => InvokeAction(PopupActionType.None));

			foreach (var b in cancelActionButtons)
				b.button.onClick.AddListener(() => InvokeAction(PopupActionType.Cancel));

			foreach (var b in confirmActionButtons)
				b.button.onClick.AddListener(() => InvokeAction(PopupActionType.Confirm));

			foreach (var b in saveActionButtons)
				b.button.onClick.AddListener(() => InvokeAction(PopupActionType.Save));

			foreach (var b in discardActionButtons)
				b.button.onClick.AddListener(() => InvokeAction(PopupActionType.Discard));
		}

		public void Init(BackdropScene scene) {
			Init(scene, new BackdropSettings(0.2f, 0.4f, false, false));
		}

		private void InvokeAction(PopupActionType type) {
			OnAction.Invoke(type);

			OnceAction.Invoke(type);
			OnceAction.RemoveAllListeners();

			if (type != PopupActionType.None)
				CloseBackdropManually();
		}

		public void Open() {
			OpenBackdrop();
			gameObject.SetActive(true);
			OnPopupStateChanged.Invoke(true);
		}

		protected override void OnBackdropClose() {
			gameObject.SetActive(false);
			OnPopupStateChanged.Invoke(false);
		}
	}
}