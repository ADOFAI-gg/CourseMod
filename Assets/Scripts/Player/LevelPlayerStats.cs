using System;
using System.Linq;
using CourseMod.DataModel;
using CourseMod.Utils;
using R3;
using UnityEngine;

namespace CourseMod.Player {
	public class LevelPlayerStats : IDisposable {
		public LevelPlayerStats(int totalTiles) {
			_tilesLeft = ScoreUpdated.Select(score => totalTiles - score.CurrentFloor)
				.ToReadOnlyReactiveProperty(totalTiles);

			LatestHitMargin = ScoreUpdated.Select(score => score.CurrentHitMargin);

			HitMarginsCount = ScoreUpdated.Select(score => score.HitMarginsCount)
				.ToReadOnlyReactiveProperty(HitMarginTools.DefaultHitMarginsCount);

			Progress = ScoreUpdated.Select(score => (score.CurrentFloor + 1f) / totalTiles)
				.ToReadOnlyReactiveProperty(0);

			XAccuracy = HitMarginsCount.Select(HitMarginTools.EvaluateXAccuracy)
				.ToReadOnlyReactiveProperty(0);

			AbsoluteXAccuracy = HitMarginsCount.CombineLatest(_tilesLeft, HitMarginTools.EvaluateAbsoluteXAccuracy)
				.ToReadOnlyReactiveProperty(0);

			MaxPossibleXAccuracy = HitMarginsCount
				.CombineLatest(_tilesLeft, HitMarginTools.EvaluateMaxPossibleXAccuracy)
				.ToReadOnlyReactiveProperty(1);
		}

		private readonly ReadOnlyReactiveProperty<int> _tilesLeft;

		// TODO invoke this!!!!!!!!
		public readonly Subject<LevelProgressFromPatch> ScoreUpdated = new();

		public readonly Observable<HitMargin?> LatestHitMargin;
		public readonly ReadOnlyReactiveProperty<int[]> HitMarginsCount;
		public readonly ReadOnlyReactiveProperty<float> Progress;
		public readonly ReadOnlyReactiveProperty<float> XAccuracy;
		public readonly ReadOnlyReactiveProperty<float> AbsoluteXAccuracy;
		public readonly ReadOnlyReactiveProperty<float> MaxPossibleXAccuracy;

		public void Reset() {
			ScoreUpdated.OnNext(LevelProgressFromPatch.Default);
		}

		public void Dispose() {
			_tilesLeft?.Dispose();
			HitMarginsCount?.Dispose();
			Progress?.Dispose();
			XAccuracy?.Dispose();
			AbsoluteXAccuracy?.Dispose();
			MaxPossibleXAccuracy?.Dispose();
		}
	}
}