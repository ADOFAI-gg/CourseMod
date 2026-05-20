using System;
using System.Collections.Generic;
using System.Linq;
using CourseMod.DataModel;
using CourseMod.Utils;
using R3;

namespace CourseMod.Player {
	public class ConstraintChecker : IDisposable {
		public ConstraintChecker(CoursePlayer coursePlayer) {
			var settings = coursePlayer.Course.Settings;

			var d = new DisposableBuilder();
			var tests = new List<Observable<(ConstraintType, bool)>>();

			if (settings.AccuracyConstraint is { } accConstraint)
				tests.Add(ObserveStat(stats => stats.MaxPossibleXAccuracy).Select(acc =>
					(ConstraintType.Accuracy, acc >= accConstraint &&
					                          (!coursePlayer.CurrentLevelPlayer.CurrentValue?.Level
						                          .DisableAccuracyConstraint ?? true))));

			if (settings.DeathConstraint is { } deathConstraint) {
				_deathConstraint.Value =
					_deathConstraintInitialValue = deathConstraint;

				tests.Add(ObserveStat(stats => stats.LatestHitMargin.Where(h => h?.IsFail() ?? false)).Select(_ =>
					(ConstraintType.Death, --_deathConstraint.Value > 0 &&
					                       (!coursePlayer.CurrentLevelPlayer.CurrentValue?.Level
						                       .DisableDeathConstraint ?? true))));
			}

			if (settings.LifeConstraint is { } lifeConstraint) {
				_lifeConstraint.Value =
					_lifeConstraintInitialValue = lifeConstraint;

				tests.Add(ObserveStat(stats => stats.LatestHitMargin.Where(h => !h?.IsPerfect() ?? false)).Select(_ =>
					(ConstraintType.Life, --_lifeConstraint.Value > 0 &&
					                      (!coursePlayer.CurrentLevelPlayer.CurrentValue?.Level
						                      .DisableLifeConstraint ?? true))));
			}

			var testResults = Observable.Zip(tests).ThrottleLastFrame(1).ToReadOnlyReactiveProperty();
			testResults.AddTo(ref d);

			var isPassingConstraint = testResults.Select(results => results?.All(test => test.Item2) ?? true);

			isPassingConstraint.Subscribe(result => {
				if (result) {
					// skip records within constraint
					return;
				}

				var failedConstraints = testResults.CurrentValue
					.Where(test => !test.Item2)
					.Select(test => test.Item1);

				coursePlayer.FailFromConstraints(failedConstraints);
			});

			_disposable = d.Build();

			return;

			Observable<T> ObserveStat<T>(Func<LevelPlayerStats, Observable<T>> select) =>
				coursePlayer.CurrentLevelPlayer.Select(x => select(x.Stats)).Switch();
		}

		private readonly int _deathConstraintInitialValue;
		private readonly int _lifeConstraintInitialValue;

		private readonly ReactiveProperty<int> _deathConstraint = new();
		private readonly ReactiveProperty<int> _lifeConstraint = new();

		private readonly IDisposable _disposable;
		
		public ReadOnlyReactiveProperty<int> DeathConstraint => _deathConstraint;
		public ReadOnlyReactiveProperty<int> LifeConstraint => _lifeConstraint;

		public void Reset() {
			_deathConstraint.Value = _deathConstraintInitialValue;
			_lifeConstraint.Value = _lifeConstraintInitialValue;
		}

		public void Dispose() {
			_disposable?.Dispose();
		}
	}
}