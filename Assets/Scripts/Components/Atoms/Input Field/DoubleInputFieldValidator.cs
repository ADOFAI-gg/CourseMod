//Resharper disable CheckNamespace

using System;

namespace CourseMod.Components.Atoms.InputField {
	public class DoubleInputFieldValidator : InputFieldValidator<double> {
		public string format;

		protected override bool TryCast(string value, out double result) {
			return double.TryParse(value, out result);
		}

		protected override double MinValueInType() {
			return double.NegativeInfinity;
		}

		protected override double MaxValueInType() {
			return double.PositiveInfinity;
		}

		protected override double Clamp(double value, double min, double max) {
			return Math.Clamp(value, min, max);
		}

		protected override string ValueToString(double value) {
			return value.ToString(format);
		}
	}
}