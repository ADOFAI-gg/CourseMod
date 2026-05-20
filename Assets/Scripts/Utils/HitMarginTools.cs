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

		/// <summary>
		/// Weights for each type of hit margin. These are used to calculate the player's X-accuracy.
		/// <br/>
		/// Do <b>NOT</b> remove the key value pair with 0 weights, because they're being used as a key somewhere else.
		/// <br/><br/>
		/// <list type="table">
		///		<listheader>
		///			<term><see cref="HitMargin"/></term>
		///			<description>Weight (%)</description>
		///		</listheader>
		///		<item>
		///			<term>Perfect</term>
		///			<description>100%</description>
		///		</item>
		///		<item>
		///			<term>EPerfect &amp; LPerfect</term>
		///			<description>75%</description>
		///		</item>
		///		<item>
		///			<term>Early &amp; Late</term>
		///			<description>40%</description>
		///		</item>
		///		<item>
		///			<term>Too Early &amp; Too Late</term>
		///			<description>20%</description>
		///		</item>
		///		<item>
		///			<term>Deaths</term>
		///			<description>0%</description>
		///		</item>
		/// </list>
		/// </summary>
		public static readonly IReadOnlyDictionary<HitMargin, double> HitMarginWeights =
			new Dictionary<HitMargin, double> {
				{ HitMargin.Perfect, 1 },
				{ HitMargin.EarlyPerfect, .75 },
				{ HitMargin.LatePerfect, .75 },
				{ HitMargin.VeryEarly, .4 },
				{ HitMargin.VeryLate, .4 },
				{ HitMargin.TooEarly, .2 },
				{ HitMargin.TooLate, .2 },
				{ HitMargin.FailMiss, 0 },
				{ HitMargin.FailOverload, 0 }
			};

		/// <summary>
		/// All hit margins that are meant to be handled as perfect.
		/// </summary>
		public static readonly HitMargin[] PerfectHitMargins = {
			HitMargin.Perfect, HitMargin.Auto, HitMargin.Multipress
		};

		/// <summary>
		/// Semi-perfect hit margins that are considered as perfect in the classic experience.
		/// </summary>
		public static readonly HitMargin[] SemiPerfectHitMargins = { HitMargin.EarlyPerfect, HitMargin.LatePerfect };

		/// <summary>
		/// Counted hit margins that aren't perfect judgments. For definition of "counted", refer to <see cref="IsCounted"/>
		/// </summary>
		public static readonly HitMargin[] NonPerfectAndCountedHitMargins = { HitMargin.VeryEarly, HitMargin.VeryLate };

		/// <summary>
		/// Deaths.
		/// </summary>
		public static readonly HitMargin[] FailHitMargins = { HitMargin.FailMiss, HitMargin.FailOverload, };

		/// <summary>
		/// X-accuracy weight for the given hit margin, or null if the hit margin does not affect player accuracy.
		/// </summary>
		/// <param name="hitMargin">Hit margin to get value from.</param>
		/// <returns>X-accuracy weight or null.</returns>
		public static double? ToXAccWeight(this HitMargin hitMargin) =>
			HitMarginWeights.TryGetValue(hitMargin, out var result) ? result : null;

		/// <summary>
		/// Whether the given hit margin is considered as perfect.
		///	</summary>
		/// <param name="hitMargin">Hit margin to get value from.</param>
		/// <param name="asInClassicExperience">Includes semi-perfects.</param>
		/// <returns><c>True</c> if perfect.</returns>
		public static bool IsPerfect(this HitMargin hitMargin, bool asInClassicExperience = false) =>
			PerfectHitMargins.Contains(hitMargin) ||
			(asInClassicExperience && SemiPerfectHitMargins.Contains(hitMargin));

		/// <summary>
		/// Whether the given hit margin is counted (goes to the next floor) except for fail miss.
		/// </summary>
		/// <param name="hitMargin">Hit margin to get value from.</param>
		/// <param name="hitMarginLimit">Custom hit margin limitation.</param>
		/// <returns><c>True</c> if counted.</returns>
		public static bool IsCounted(this HitMargin hitMargin, HitMarginLimit? hitMarginLimit = null) {
			hitMarginLimit ??= GCS.hitMarginLimit;

			return hitMarginLimit switch {
				HitMarginLimit.None => hitMargin.IsPerfect(true) || NonPerfectAndCountedHitMargins.Contains(hitMargin),
				HitMarginLimit.PerfectsOnly => hitMargin.IsPerfect(true),
				HitMarginLimit.PurePerfectOnly => hitMargin.IsPerfect(),
				_ => true
			};
		}

		/// <summary>
		/// Whether the given hit margin is considered as a fail (death).
		/// </summary>
		/// <param name="hitMargin">Hit margin to get value from.</param>
		/// <returns><c>True</c> if fail hit margin.</returns>
		public static bool IsFail(this HitMargin hitMargin) => FailHitMargins.Contains(hitMargin);

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
			
			var divisor = Math.Max(1, hitMarginsCount.Sum());
			divisor += padFloorDivisor;
			
			return weightSum / divisor;
		}
	}
}