using System;
using UnityEngine;
using UnityEngine.UI;

namespace CourseMod.Components.Molecules {
	[ExecuteAlways]
	[RequireComponent(typeof(ScrollRect))]
	[RequireComponent(typeof(LayoutElement))]
	public class ScrollRectExpander : MonoBehaviour {
		private ScrollRect _scroll;
		private LayoutElement _layout;

		public float maxHeight = -1;

		private void Awake() {
			_scroll = GetComponent<ScrollRect>();
			_layout = GetComponent<LayoutElement>();
		}

		private void Update() => UpdateSize();

		public void UpdateSize() {
			var height = LayoutUtility.GetPreferredHeight(_scroll.content);

			if (maxHeight >= 0)
				height = Mathf.Clamp(height, 0, maxHeight);

			_layout.preferredHeight = height;
		}
	}
}