using System;
using CourseMod.DataModel;
using R3;
using UnityEngine;

namespace CourseMod.Player
{
	public class CountdownPlayer : IDisposable {
		public CountdownPlayer(int? countdownSeconds) {
			_countdownSeconds = countdownSeconds;

			Reset();
		}
		
		public readonly ReactiveProperty<float> TimeLeft = new();
		public readonly Subject<bool> CountdownEnded = new();
		
		private readonly CoursePlayer _coursePlayer;
		private readonly int? _countdownSeconds;
		private IDisposable _updateCycle;

		public void Reset() {
			Stop();
			
			if (_countdownSeconds is { } seconds) {
				TimeLeft.Value = seconds;
			} else {
				TimeLeft.Value = float.PositiveInfinity;
			}
		}

		public void Start() {
			Stop(); // if not ran, calling this method twice creates indisposable object
			
			_updateCycle = Observable.EveryUpdate(UnityFrameProvider.Update).Subscribe(_ => {
				var time = TimeLeft.Value;
				TimeLeft.Value = Math.Max(0, time - Time.unscaledDeltaTime);

				if (time <= 0) {
					End();
				}
			});
		}

		public void Stop() {
			_updateCycle?.Dispose();
		}

		public void Skip() {
			if (_coursePlayer.CurrentLevelPlayer.CurrentValue.CanStartPlaying.Value) {
				EndInternal(true);
			}
		}

		public void End() => EndInternal(false);

		private void EndInternal(bool skipped) {
			Stop();
			TimeLeft.Value = 0;

			CountdownEnded.OnNext(skipped);
		}
		
		public void Dispose() {
			TimeLeft?.Dispose();
			_updateCycle?.Dispose();
		}
	}
}