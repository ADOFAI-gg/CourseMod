//Resharper disable CheckNamespace

using TMPro;
using UnityEngine;

namespace CourseMod.Components.Atoms.InputField {
	public abstract class InputFieldValidator<T> : MonoBehaviour {
		public TMP_InputField inputField;

		public bool useMin;
		public bool useMax;
		public T minValue;
		public T maxValue;

		private void Awake() {
			inputField.onEndEdit.AddListener(Validate);
		}

		protected abstract bool TryCast(string value, out T result);

		protected abstract T MinValueInType();

		protected abstract T MaxValueInType();

		protected abstract T Clamp(T value, T min, T max);

		private void Validate(string value) {
			if (value.IsNullOrEmpty()) return;
			if (!TryCast(value, out T result)) inputField.SetTextWithoutNotify("");

			inputField.SetTextWithoutNotify(ValueToString(ClampCurrentRange(result)));
		}

		private T ClampCurrentRange(T value) {
			return Clamp(value, useMin ? minValue : MinValueInType(), useMax ? maxValue : MaxValueInType());
		}

		protected abstract string ValueToString(T value);
	}
}