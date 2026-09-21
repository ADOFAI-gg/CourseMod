using System;
using System.Collections.Generic;
using System.Linq;

namespace CourseMod.Utils {
	public static class HitMarginTools {
		public static int[] DefaultHitMarginsCount => new int[Enum.GetValues(typeof(HitMargin)).Length];
		public static bool TryGetHitMarginCount(int[] hitMargins, HitMargin hitMargin, out int count) {
			count = 0;

			var index = (int) hitMargin;
			if (hitMargins.Length <= index)
				return false;

			count = hitMargins[index];
			return true;
		}

		public static float EvaluateXAccuracy(this int[] hitMarginsCount) =>
			EvaluateXAccuracyInternal(hitMarginsCount);
		public static float EvaluateAbsoluteXAccuracy(this int[] hitMarginsCount, int unhitFloors) =>
			EvaluateXAccuracyInternal(hitMarginsCount, 0, unhitFloors);
		public static float EvaluateMaxPossibleXAccuracy(this int[] hitMarginsCount, int unhitFloors) =>
			EvaluateXAccuracyInternal(hitMarginsCount, unhitFloors, unhitFloors);

		private static float EvaluateXAccuracyInternal(int[] hitMarginsCount, int padFloorSum = 0, int padFloorDivisor = 0) {
			var weightSum = hitMarginsCount
				.Select((c, i) => (float?) (c * ((HitMargin) i).ToXAccWeight()))
				.Sum() ?? 0f;

			weightSum += padFloorSum;
			
			var divisor = Math.Max(0, hitMarginsCount.Sum());
			divisor += padFloorDivisor;
			
			return divisor == 0 ? 
				       weightSum == 0 ? 1 : 0
				       : weightSum / divisor;
		}

		/// <summary>
		/// Changes the preset value to a simple value with no variance.
		/// </summary>
		/// <param name="preset">Preset to change.</param>
		/// <returns>
		/// <see cref="HitMarginPerfectTextPreset.Default"/>, <see cref="HitMarginPerfectTextPreset.ShowXPerfect"/>,
		/// <see cref="HitMarginPerfectTextPreset.ShowXPerfectAndSignedPerfects"/>, <see cref="HitMarginPerfectTextPreset.ShowSignedPerfects"/>
		/// </returns>
		public static HitMarginPerfectTextPreset RemoveVariance(this HitMarginPerfectTextPreset preset) =>
			preset switch
			{
				HitMarginPerfectTextPreset.ShowXPerfectWithParticle => HitMarginPerfectTextPreset.ShowXPerfect,
				HitMarginPerfectTextPreset.ShowXPerfectWithParticleAndSignedPerfects => HitMarginPerfectTextPreset.ShowXPerfectAndSignedPerfects,
				// HitMarginPerfectTextPreset.ShowXPerfect => HitMarginPerfectTextPreset.ShowXPerfect,
				// HitMarginPerfectTextPreset.ShowXPerfectAndSignedPerfects => HitMarginPerfectTextPreset.ShowXPerfectAndSignedPerfects,
				// HitMarginPerfectTextPreset.ShowSignedPerfects => HitMarginPerfectTextPreset.ShowSignedPerfects,
				_ => preset
			};

		/// <summary>
		/// Determines whether the given preset includes the display of signed perfect margins.
		/// </summary>
		/// <param name="preset">The preset to evaluate.</param>
		/// <returns>
		/// True if the preset includes <see cref="HitMarginPerfectTextPreset.ShowSignedPerfects"/>
		/// or <see cref="HitMarginPerfectTextPreset.ShowXPerfectAndSignedPerfects"/>; otherwise, false.
		/// </returns>
		public static bool IsShowSignedPerfects(this HitMarginPerfectTextPreset preset) =>
			preset.RemoveVariance() switch
			{
				HitMarginPerfectTextPreset.ShowSignedPerfects => true,
				HitMarginPerfectTextPreset.ShowXPerfectAndSignedPerfects => true,
				_ => false
			};
	}
}