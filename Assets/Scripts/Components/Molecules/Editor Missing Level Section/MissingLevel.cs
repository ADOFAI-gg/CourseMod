using System.Collections;
using System.Collections.Generic;
using System.Text;
using CourseMod.Utils;
using TMPro;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.EditorMissingLevelSection {
	public class MissingLevel : MonoBehaviour {
		public TextMeshProUGUI levelNumber;
		public TextMeshProUGUI levelName;

		public void UpdateDisplay(int number, string level) {
			StringBuilder sb = new();
			sb.Append(number);
			if (I18N.IsKorean()) {
				sb.Append('번');
			} else {
				int lastDigit = number % 10;
				sb.Append(lastDigit switch {
					1 => "st",
					2 => "nd",
					3 => "rd",
					_ => "th",
				});
			}

			levelNumber.text = sb.ToString();
			levelName.text = level ?? I18N.Get("editor-unknown-level");
		}
	}
}