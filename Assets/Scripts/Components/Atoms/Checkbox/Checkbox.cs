using System;
using System.Collections.Generic;
using CourseMod.Exceptions;
using CourseMod.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CourseMod.Components.Atoms.Checkbox {
	public class Checkbox : MonoBehaviour {
		public enum State {
			Unchecked,
			Checked,
			Intermediate
		}

		public class OnStateChangedEvent : UnityEvent<State> {
		}

		public class OnClickedEvent : UnityEvent {
		}

		public UnityEngine.UI.Button button;
		public Image image;
		public State state;

		public readonly OnStateChangedEvent onStateChanged = new();
		public readonly OnClickedEvent onClick = new();

		private static IReadOnlyDictionary<State, Sprite> _stateSprites;
		private static bool _initialized;

		private static void SetupStateSprites() {
			Assert.False(_initialized, "SetupStateSprites can only be called once");

			_stateSprites = new Dictionary<State, Sprite> {
				{
					State.Unchecked,
					Assets.LoadAsset<Sprite>("Assets/Scripts/Components/Atoms/Checkbox/CheckboxEmpty.png")
				}, {
					State.Checked,
					Assets.LoadAsset<Sprite>("Assets/Scripts/Components/Atoms/Checkbox/CheckboxChecked.png")
				}, {
					State.Intermediate,
					Assets.LoadAsset<Sprite>("Assets/Scripts/Components/Atoms/Checkbox/CheckboxDashed.png")
				}
			};
			_initialized = true;
		}

		private void Awake() {
			button.onClick.AddListener(OnButtonClick);
			UpdateAppearance();
		}

#if UNITY_EDITOR
		private void OnValidate() {
			UpdateAppearance();
		}
#endif

		private void UpdateAppearance() {
			if (!_initialized) SetupStateSprites();
			if (_stateSprites.TryGetValue(state, out var sprite))
				image.sprite = sprite;
		}

		private void OnButtonClick() {
			Toggle();
			onClick.Invoke();
		}

		public void SetState(State newState) {
			if (state == newState) return;
			SetStateEventless(newState);
			onStateChanged.Invoke(state);
		}

		public void SetStateEventless(State newState) {
			if (state == newState) return;
			state = newState;
			UpdateAppearance();
		}

		public void Toggle() {
			SetState(state != State.Checked ? State.Checked : State.Unchecked);
		}
	}
}