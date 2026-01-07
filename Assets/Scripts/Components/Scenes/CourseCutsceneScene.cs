using System;
using CourseMod.Utils;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CourseMod.Components.Scenes {
	public class CourseCutsceneScene : MonoBehaviour, IPointerMoveHandler {
		[UsedImplicitly] public const string SCENE_NAME = "CourseCutscene";

		private static string _videoUrl;
		private static Action _staticCallback;

		private string _path;
		private Action _callback;
		private Tween _tween;
		private float? _lastMove;

		public VideoPlayer player;
		public Slider volumeSlider;
		public CanvasGroup canvasGroup;

		private void Awake() {
			_path = _videoUrl;
			_callback = _staticCallback;

			_videoUrl = null;
			_staticCallback = null;

			for (ushort i = 0; i < player.audioTrackCount; i++) player.SetDirectAudioVolume(i, 0.5f);

			volumeSlider.onValueChanged.AddListener(value => {
				for (ushort i = 0; i < player.audioTrackCount; i++) player.SetDirectAudioVolume(i, value);
			});

			canvasGroup.alpha = 0;
		}

		private void Start() {
			player.url = _path;
			player.loopPointReached += Exit;

			player.Prepare();
		}

		private void Update() {
			if (Input.GetKeyDown(KeyCode.Escape))
				Exit();

			if (_lastMove is { } time) {
				if (Time.timeSinceLevelLoad - time >= 4f) {
					_lastMove = null;

					_tween?.Kill();
					_tween = DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0, 0.4f)
							.SetUpdate(true)
						;
				}
			}
		}

		private void Exit([CanBeNull] VideoPlayer _ = null) {
			SceneManager.UnloadSceneAsync("CourseCutscene");
		}

		public void OnPointerMove(PointerEventData _) {
			_lastMove = Time.timeSinceLevelLoad;

			_tween?.Kill();
			_tween = DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1, 0.2f)
					.SetUpdate(true)
				;
		}

		private void OnDestroy() {
			_callback();
		}

		// ---

		public static void BeginCutscene(string path, Action afterCutscene) {
#if DEBUG && false
			_videoUrl = path;
			_staticCallback = afterCutscene;

			SceneManager.LoadScene(SCENE_NAME, LoadSceneMode.Additive);
#else
			// TODO scene not implemented yet
			afterCutscene.Invoke();
#endif
		}
	}
}