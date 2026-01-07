//Resharper disable CheckNamespace

using System;

namespace CourseMod.Components.Atoms.InputField {
	public class IntInputFieldValidator : InputFieldValidator<int> {
		protected override bool TryCast(string value, out int result) {
			return int.TryParse(value, out result);
		}

		protected override int MinValueInType() {
			return int.MinValue;
		}

		protected override int MaxValueInType() {
			return int.MaxValue;
		}

		protected override int Clamp(int value, int min, int max) {
			return Math.Clamp(value, min, max);
		}

		protected override string ValueToString(int value) {
			return value.ToString();
		}
	}
}