using System;
using System.Collections.Generic;
using System.Linq;
using CourseMod.DataModel;
using CourseMod.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using R3;

namespace CourseMod.Player {
	public class CoursePlayer : IDisposable {
		public CoursePlayer(Course course) {
			Course = course;
			
			ConstraintChecker = new(this);
			_disposables.Add(ConstraintChecker);
			
			LevelPlayers = Course.Levels.Select((level, index) => new LevelPlayer(this, index, level)).ToArray();
			PlayRecords = new CourseLevelPlayRecord?[LevelPlayers.Length];
			Array.Fill(PlayRecords, null);

			CurrentLevelPlayer = Index.Select(i => {
				if (i >= 0 && i < LevelPlayers.Length) {
					return LevelPlayers[i];
				}

				return null;
			}).ToReadOnlyReactiveProperty();
			_disposables.Add(CurrentLevelPlayer);

			IsOnLastLevel = Index.Select(i => i >= LevelPlayers.Length - 1).ToReadOnlyReactiveProperty();
			_disposables.Add(IsOnLastLevel);

			// var builder = new DisposableBuilder();
			// foreach (var player in LevelPlayers) {
			// 	player.PlayFinished.Subscribe(record => PlayRecords[player.Index] = record).AddTo(ref builder);
			// }
			//
			// _disposables.Add(builder.Build());
			
			_disposables.Add(LevelPlayerInitialized);
			_disposables.Add(CourseEnded);
		}

		public readonly Course Course;
		public readonly ConstraintChecker ConstraintChecker;
		public readonly LevelPlayer[] LevelPlayers;
		public readonly CourseLevelPlayRecord?[] PlayRecords;
		
		public readonly ReactiveProperty<int> Index = new(-1);
		public readonly ReadOnlyReactiveProperty<LevelPlayer> CurrentLevelPlayer;
		public readonly ReadOnlyReactiveProperty<bool> IsOnLastLevel;

		public readonly Subject<LevelPlayer> LevelPlayerInitialized = new();
		public readonly Subject<CourseResult> CourseEnded = new();
		
		private readonly List<IDisposable> _disposables = new();

		private bool _failed;

		public void NextLevel() {
			_failed = false;

			if (Index.Value + 1 >= LevelPlayers.Length) {
				LogTools.LogWarning($"Index out of range; was given {Index.Value}");
				return;
			}

			var nextPlayer = LevelPlayers[++Index.Value];
			nextPlayer.Initialize();
			
			LevelPlayerInitialized.OnNext(nextPlayer);
			_disposables.Add(nextPlayer.PlayFinished.Subscribe(record => {
				PlayRecords[Index.Value] = record;

				if (IsOnLastLevel.CurrentValue && !_failed)
					EndInternal();
			}));
		}

		public void FailFromConstraints(IEnumerable<ConstraintType> constraints) =>
			FailInternal(constraints.Select(c => c.ToFailReason()).ToArray());

		public void FailFromGameMechanics() =>
			FailInternal(CourseFailReason.VanillaGameMechanics);

		public void FailFromPlayerIntent() =>
			FailInternal(CourseFailReason.PlayerIntent);

		private void FailInternal(CourseFailReason reason) => FailInternal(new [] { reason });
		private void FailInternal(CourseFailReason[] reasons) {
			_failed = true;
			
			CurrentLevelPlayer.CurrentValue?.Fail(!reasons.Contains(CourseFailReason.VanillaGameMechanics),
				reasons.First().ToFailMessage());

			EndInternal(reasons);
		}

		private void EndInternal([CanBeNull] CourseFailReason[] failReasons = null) {
			var result = new CourseResult {
				Records = PlayRecords.Where(record => record.HasValue && !record.Value.Equals(default(CourseLevelPlayRecord))).Cast<CourseLevelPlayRecord>().ToArray(),
				FailReasons = failReasons,
			};
			
			ConstraintChecker.Reset();

			// TODO I shouldn't be doing this because this is a responsibility of LevelPlayer
			foreach (var player in LevelPlayers)
				player.Stats.Reset();
			
			Array.Fill(PlayRecords, null);
			
			Index.Value = -1;
			
			CourseEnded.OnNext(result);
			LogTools.Log($"Course ended with score {JsonConvert.SerializeObject(result)}");
		}

		public void Dispose() {
			foreach (var disposable in _disposables) {
				disposable?.Dispose();
			}

			foreach (var player in LevelPlayers) {
				player?.Dispose();
			}
		}
	}
}