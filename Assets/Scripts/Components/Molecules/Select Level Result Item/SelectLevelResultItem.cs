using System;
using CourseMod.Components.Atoms.HitMarginDisplay;
using CourseMod.DataModel;
using CourseMod.Utils;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.SelectLevelResultItem {
	public class SelectLevelResultItem : MonoBehaviour {
		public TextMeshProUGUI levelNumber;
		public TextMeshProUGUI levelName;
		public TextMeshProUGUI accuracy;

		public HitMarginDisplay hitMarginDisplay;

		public void UpdateDisplay(string levelNameValue, int levelNumberValue,
			[CanBeNull] CourseLevelPlayRecord record) {
			levelNumber.text = levelNumberValue.ToString();
			levelName.text = levelNameValue;

			UpdateDisplay(record);
		}

		public void UpdateDisplay([CanBeNull] CourseLevelPlayRecord record) {
			if (record == null) {
				accuracy.gameObject.SetActive(false);
				hitMarginDisplay.transform.parent.gameObject.SetActive(false);
				return;
			}

			accuracy.gameObject.SetActive(true);
			accuracy.text = record.XAccuracy
				.ToAccuracyNotation()
				.GoldTextIfTrue(record.HitMargins.IsPurePerfect(record.TotalFloors));

			hitMarginDisplay.UpdateDisplay(record.HitMargins);
			hitMarginDisplay.transform.parent.gameObject.SetActive(true);
		}

		public void UpdateEtc(int count) {
			levelNumber.text = "···";
			levelName.text = I18N.Get("general-and-more-levels", new() { ["count"] = count });

			accuracy.gameObject.SetActive(false);
			hitMarginDisplay.transform.parent.gameObject.SetActive(false);
		}
	}
}