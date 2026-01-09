using System;
using System.Collections.Generic;
using CourseMod.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public record SerializableHitMargins {
		public int TooLate;
		public int VeryLate;
		public int LatePerfect;
		public int Perfect;
		public int Auto;
		public int EarlyPerfect;
		public int VeryEarly;
		public int TooEarly;

		public int Miss;
		public int Overload;

		public int TotalCount;

		private bool NonPerfectsDontExist() => TooLate == 0 && VeryLate == 0 && LatePerfect == 0 &&
		                                      EarlyPerfect == 0 && VeryEarly == 0 && TooEarly == 0 &&
		                                      Miss == 0 && Overload == 0;

		public bool IsPurePerfect(int totalFloors) =>
			NonPerfectsDontExist() &&
			totalFloors == TotalCount &&
			TotalCount != 0;

		[JsonIgnore] public static SerializableHitMargins Default => FromHitMarginsCount(Array.Empty<int>(), 0);

		public static SerializableHitMargins FromHitMarginsCount(int[] hitMarginsCount, int totalCount) {
			var result = new SerializableHitMargins();

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.TooLate, out result.TooLate);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.VeryLate, out result.VeryLate);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.LatePerfect, out result.LatePerfect);

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.Perfect, out result.Perfect);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.Auto, out result.Auto);

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.EarlyPerfect, out result.EarlyPerfect);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.VeryEarly, out result.VeryEarly);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.TooEarly, out result.TooEarly);

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.FailMiss, out result.Miss);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.FailOverload, out result.Overload);

			result.TotalCount = totalCount;

			return result;
		}

		public static SerializableHitMargins operator +(SerializableHitMargins a, SerializableHitMargins b) {
			var result = new SerializableHitMargins();

			result.TooLate += a.TooLate + b.TooLate;
			result.VeryLate += a.VeryLate + b.VeryLate;
			result.LatePerfect += a.LatePerfect + b.LatePerfect;
			result.Perfect += a.Perfect + b.Perfect;
			result.Auto += a.Auto + b.Auto;
			result.EarlyPerfect += a.EarlyPerfect + b.EarlyPerfect;
			result.VeryEarly += a.VeryEarly + b.VeryEarly;
			result.TooEarly += a.TooEarly + b.TooEarly;
			result.Miss += a.Miss + b.Miss;
			result.Overload += a.Overload + b.Overload;
			result.TotalCount += a.TotalCount + b.TotalCount;

			return result;
		}
	}
}