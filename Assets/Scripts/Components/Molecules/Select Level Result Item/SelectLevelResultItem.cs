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
			CourseLevelPlayRecord? record) {
			levelNumber.text = levelNumberValue.ToString();
			levelName.text = levelNameValue;

			UpdateDisplay(record);
		}

		public void UpdateDisplay(CourseLevelPlayRecord? record) {
			if (record is not { } castedRecord) {
				goto useDefaultVariant;
			}

			if (record.Equals(default(CourseLevelPlayRecord))) {
				goto useDefaultVariant;
			}

			// LogTools.Log($"castedRecord = {castedRecord.GetLogString()}");

			accuracy.gameObject.SetActive(true);
			accuracy.text = castedRecord.XAccuracy
				.ToAccuracyNotation()
				.GoldTextIfTrue(castedRecord.HitMargins.IsPurePerfect(castedRecord.TotalFloors));

			hitMarginDisplay.UpdateDisplay(castedRecord.HitMargins);
			hitMarginDisplay.transform.parent.gameObject.SetActive(true);
			return;
			
			useDefaultVariant:
			accuracy.gameObject.SetActive(false);
			hitMarginDisplay.transform.parent.gameObject.SetActive(false);
			return;
		}

		public void UpdateEtc(int count) {
			levelNumber.text = "···";
			levelName.text = I18N.Get("general-and-more-levels", new() { ["count"] = count });

			accuracy.gameObject.SetActive(false);
			hitMarginDisplay.transform.parent.gameObject.SetActive(false);
		}
	}
}