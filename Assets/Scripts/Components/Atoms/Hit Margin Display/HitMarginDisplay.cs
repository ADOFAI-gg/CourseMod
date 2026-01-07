using CourseMod.DataModel;
using CourseMod.Utils;
using TMPro;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Atoms.HitMarginDisplay {
	public class HitMarginDisplay : MonoBehaviour {
		public TextMeshProUGUI tooEarly;
		public TextMeshProUGUI early;
		public TextMeshProUGUI ePerfect;
		public TextMeshProUGUI perfect;
		public TextMeshProUGUI lPerfect;
		public TextMeshProUGUI late;
		public TextMeshProUGUI tooLate;

		public TextMeshProUGUI miss;
		public TextMeshProUGUI overload;

		public GameObject[] fails;

		public void UpdateDisplay(int[] hitMargins) {
			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.TooEarly, out var count))
				tooEarly.text = count.ToString();

			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.VeryEarly, out count))
				early.text = count.ToString();

			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.EarlyPerfect, out count))
				ePerfect.text = count.ToString();

			HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.Perfect, out var perfectsCount);

			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.Auto, out count))
				perfectsCount += count;

			perfect.text = perfectsCount.ToString();

			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.LatePerfect, out count))
				lPerfect.text = count.ToString();

			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.VeryLate, out count))
				late.text = count.ToString();

			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.TooLate, out count))
				tooLate.text = count.ToString();


			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.FailMiss, out var failCount))
				miss.text = failCount.ToString();

			if (HitMarginTools.TryGetHitMarginCount(hitMargins, HitMargin.FailOverload, out count))
				overload.text = (failCount += count).ToString();

			var showFails = failCount != 0;

			foreach (var fail in fails)
				fail.gameObject.SetActive(showFails);
		}

		public void UpdateDisplay(SerializableHitMargins hitMargins) {
			tooEarly.text = hitMargins.TooEarly.ToString();
			early.text = hitMargins.VeryEarly.ToString();
			ePerfect.text = hitMargins.EarlyPerfect.ToString();
			perfect.text = (hitMargins.Perfect + hitMargins.Auto).ToString();
			lPerfect.text = hitMargins.LatePerfect.ToString();
			late.text = hitMargins.VeryLate.ToString();
			tooLate.text = hitMargins.TooLate.ToString();
			miss.text = hitMargins.Miss.ToString();
			overload.text = hitMargins.Overload.ToString();

			var showFails = hitMargins.Miss + hitMargins.Overload > 0;

			foreach (var fail in fails)
				fail.gameObject.SetActive(showFails);
		}
	}
}