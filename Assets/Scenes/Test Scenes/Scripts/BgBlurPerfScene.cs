using System.Collections;
using TMPro;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace CourseMod.TestScripts {
	public class BgBlurPerfScene : MonoBehaviour {
		public TextMeshProUGUI fpsText;

		private float _currentFps;
		private float _smoothedFps;
		private const float SmoothingFactor = 0.4f; // Adjust this to control how much weight is given to recent FPS

		private void Update() {
			_currentFps = 1f / Time.unscaledDeltaTime;

			// Apply exponential smoothing to calculate the weighted average
			_smoothedFps = (SmoothingFactor * _currentFps) + (1f - SmoothingFactor) * _smoothedFps;

			fpsText.text = $"FPS: {Mathf.RoundToInt(_smoothedFps)}";
		}
	}
}