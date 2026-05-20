using System;
using CourseMod.DataModel;
using CourseMod.Patches;
using CourseMod.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using R3;

namespace CourseMod.Player
{
	public class LevelPlayer : IDisposable {
		public LevelPlayer(CoursePlayer coursePlayer, int index, CourseLevel level) {
			_coursePlayer = coursePlayer;
			_tiles = level.LevelMeta.Tiles;

			Index = index;
			Level = level;

			Stats = new(_tiles);
		}

		public readonly int Index;
		public readonly CourseLevel Level;
		public readonly LevelPlayerStats Stats;
		
		public readonly ReactiveProperty<bool> CanStartPlaying = new(false);
		public readonly Subject<CourseLevelPlayRecord> PlayFinished = new();

		private readonly CoursePlayer _coursePlayer;
		private readonly int _tiles;

		public void Initialize() {
			GameplayPatches.SetupGameSceneParameters.LoadLevel(
				this,
				() => CanStartPlaying.Value = true);
		}

		public void Fail(bool performDeath, [CanBeNull] string deathReason = null) {
			if (performDeath) {
				GameplayPatches.KillPlayer(deathReason);
			}

			End();
		}

		public void Complete() => End();

		private void End() {
			CourseLevelPlayRecord record = new() {
				CourseChecksum = ChecksumTools.ComputeCourseChecksum(_coursePlayer.Course).Hash,
				GameplayChecksum = Level.GameplayChecksum,
				HitMargins = SerializableHitMargins.FromHitMarginsCount(Stats.HitMarginsCount.CurrentValue),
				LevelNumber = Index,
				XAccuracy = Stats.AbsoluteXAccuracy.CurrentValue,
				TotalFloors = _tiles,
			};

			Stats.Reset();
			
			PlayFinished.OnNext(record);
			LogTools.Log($"LevelPlayer {Index} ended with score {JsonConvert.SerializeObject(record)}");
		}

		public void Dispose() {
			Stats?.Dispose();
			CanStartPlaying?.Dispose();
			PlayFinished?.Dispose();
		}
	}
}