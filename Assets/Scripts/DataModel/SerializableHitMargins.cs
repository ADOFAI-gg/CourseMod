using System;
using System.Collections.Generic;
using System.Linq;
using CourseMod.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CourseMod.DataModel {
	[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
	public record SerializableHitMargins {
		public int TooLate;
		public int VeryLate;
		public int LatePerfect;
		public int PerfectPlus;
		public int XPerfect;
		public int PerfectMinus;
		public int EarlyPerfect;
		public int VeryEarly;
		public int TooEarly;

		public int TotalPerfects;

		public int Miss;
		public int Overload;

		public int Auto;
		public int Overpress;
		public int Midspin;
		public int FailedFloor;

		public int TotalCount;

		private bool NonPerfectsDontExist() => AllZeros(TooLate, VeryLate, LatePerfect, EarlyPerfect, VeryEarly, TooEarly, Miss, Overload, FailedFloor);
		private static bool AllZeros(params int[] values) => values.All(value => value == 0);

		public bool IsPurePerfect(int totalFloors) =>
			NonPerfectsDontExist() &&
			totalFloors == TotalCount &&
			TotalCount != 0;

		[JsonIgnore] public static SerializableHitMargins Default => FromHitMarginsCount(Array.Empty<int>());

		public static SerializableHitMargins FromHitMarginsCount(int[] hitMarginsCount) {
			var result = new SerializableHitMargins();

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.TooLate, out result.TooLate);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.VeryLate, out result.VeryLate);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.LatePerfect, out result.LatePerfect);

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.PerfectPlus, out result.PerfectPlus);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.XPerfect, out result.XPerfect);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.PerfectMinus, out result.PerfectMinus);

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.EarlyPerfect, out result.EarlyPerfect);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.VeryEarly, out result.VeryEarly);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.TooEarly, out result.TooEarly);

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.FailMiss, out result.Miss);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.FailOverload, out result.Overload);

			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.Auto, out result.Auto);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.OverPress, out result.Overpress);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.Midspin, out result.Midspin);
			HitMarginTools.TryGetHitMarginCount(hitMarginsCount, HitMargin.FailedFloor, out result.FailedFloor);

			result.TotalPerfects = result.PerfectPlus + result.XPerfect + result.PerfectMinus;
			result.TotalCount = hitMarginsCount.Sum();

			return result;
		}

		public static SerializableHitMargins operator +(SerializableHitMargins a, SerializableHitMargins b) {
			var result = new SerializableHitMargins();

			result.TooLate += a.TooLate + b.TooLate;
			result.VeryLate += a.VeryLate + b.VeryLate;
			result.LatePerfect += a.LatePerfect + b.LatePerfect;
			result.PerfectPlus += a.PerfectPlus + b.PerfectPlus;
			result.XPerfect += a.XPerfect + b.XPerfect;
			result.PerfectMinus += a.PerfectMinus + b.PerfectMinus;
			result.EarlyPerfect += a.EarlyPerfect + b.EarlyPerfect;
			result.VeryEarly += a.VeryEarly + b.VeryEarly;
			result.TooEarly += a.TooEarly + b.TooEarly;
			result.Miss += a.Miss + b.Miss;
			result.Overload += a.Overload + b.Overload;
			result.Auto += a.Auto + b.Auto;
			result.Overpress += a.Overpress + b.Overpress;
			result.Midspin += a.Midspin + b.Midspin;
			result.FailedFloor += a.FailedFloor + b.FailedFloor;

			result.TotalPerfects = a.TotalPerfects + b.TotalPerfects;
			result.TotalCount += a.TotalCount + b.TotalCount;

			return result;
		}
	}
}