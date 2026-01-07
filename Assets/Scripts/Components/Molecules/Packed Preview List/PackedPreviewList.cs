using System;
using CourseMod.Components.Atoms;
using TMPro;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.PackedPreviewList {
	public class PackedPreviewList : MonoBehaviour {
		public TextMeshProUGUI textPrefab;
		public GameObject separatorPrefab;

		public RectTransform textContainer;
		public int maxVisibleTexts;

		public TextMeshProUGUI prefix;
		public TextI18N andMoreText;

		private void Awake() => ResetDisplay();

		public void UpdateDisplay(string[] texts) {
			if (texts.Length == 0) {
				ResetDisplay();
				return;
			}

			var childCount = textContainer.childCount;
			var textChildCount = (childCount + 1) / 2;

			var appliedMaxVisibleTexts = maxVisibleTexts == -1 ? texts.Length : maxVisibleTexts;

			var desiredChildCount = appliedMaxVisibleTexts * 2 - 1;
			var desiredVisibleTextChildCount = Math.Min(textChildCount, appliedMaxVisibleTexts);

			var totalTexts = texts.Length;

			var rest = (desiredVisibleTextChildCount - totalTexts) * 2;
			var require = totalTexts - desiredVisibleTextChildCount;

			// 1. childCount만큼 iterate해 text를 변경
			// 2. rest가 존재한다면 rest만큼 존재하는 child를 삭제
			// 3. require가 존재한다면 require만큼 Instantiate하고 값 설정

			for (var i = 0; i < Math.Min(childCount, desiredChildCount); i += 2)
				textContainer.GetChild(i).GetComponent<TextMeshProUGUI>().text = texts[i];

			for (var i = childCount - 1; i >= childCount - rest - 1; i--)
				Destroy(textContainer.GetChild(i).gameObject);

			for (var i = 0; i < require; i++) {
				var text = Instantiate(textPrefab, textContainer);
				text.text = texts[textChildCount + i];

				if (i != require - 1)
					Instantiate(separatorPrefab, textContainer);
			}

			var more = totalTexts - appliedMaxVisibleTexts;

			if (more > 0)
				andMoreText.UpdateArguments(new() { ["count"] = more });

			andMoreText.gameObject.SetActive(more > 0);
		}

		private void ResetDisplay() {
			var childCount = textContainer.childCount;

			for (var i = 1; i < childCount; i++)
				Destroy(textContainer.GetChild(i).gameObject);

			if (childCount == 0)
				Instantiate(textPrefab, textContainer);

			else textContainer.GetChild(0).GetComponent<TextMeshProUGUI>().text = "-";
		}
	}
}