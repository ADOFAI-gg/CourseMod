using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Organisms.EditorLevelList {
	[RequireComponent(typeof(LayoutElement))]
	public class FillViewport : MonoBehaviour {
		public enum FillType {
			None,
			Width,
			Height,
			WidthAndHeight
		}

		public RectTransform viewport;
		public FillType fillType = FillType.Height;

		private LayoutElement _layout;

		private bool FillWidth => fillType is FillType.Width or FillType.WidthAndHeight;
		private bool FillHeight => fillType is FillType.Height or FillType.WidthAndHeight;

		private void Awake() {
			_layout = GetComponent<LayoutElement>();
		}

		private void Start() {
			StartCoroutine(UpdatePreferredSizeNextFrame());
		}

		private IEnumerator UpdatePreferredSizeNextFrame() {
			yield return null;
			UpdatePreferredSize();
		}

		public void UpdatePreferredSize() {
			if (FillWidth)
				_layout.preferredWidth = viewport.rect.width;

			if (FillHeight)
				_layout.preferredHeight = viewport.rect.height;
		}

#if UNITY_EDITOR
		private void OnValidate() {
			Awake();
			UpdatePreferredSize();
		}
#endif
	}
}