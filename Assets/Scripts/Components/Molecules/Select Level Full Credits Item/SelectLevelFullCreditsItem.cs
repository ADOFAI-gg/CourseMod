using CourseMod.DataModel;
using TMPro;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.SelectLevelFullCreditsItem {
	public class SelectLevelFullCreditsItem : MonoBehaviour {
		public TextMeshProUGUI levelNumber;
		public TextMeshProUGUI levelName;

		public PackedPreviewList.PackedPreviewList musicCredits;
		public PackedPreviewList.PackedPreviewList levelCredits;

		public bool mysterious;

		private const string MysteriousLabel = "???";
		private CourseLevel _level;

		public void UpdateInfo(int levelNum, CourseLevel level) {
			levelNumber.text = (levelNum + 1).ToString();
			mysterious = level.Mysterious;

			_level = level;

			DisplayInfo();
			UpdateSpoilerState(false);
		}

		private void DisplayInfo() {
			var meta = _level.LevelMeta;

			levelName.text = meta.Song;
			musicCredits.UpdateDisplay(new[] { meta.Artist });
			levelCredits.UpdateDisplay(new[] { meta.Creator });
		}

		private void HideInfo() {
			levelName.text = MysteriousLabel;
			musicCredits.UpdateDisplay(new[] { MysteriousLabel });
			levelCredits.UpdateDisplay(new[] { MysteriousLabel });
		}

		public void UpdateSpoilerState(bool forceShowSpoilers) {
			if (!mysterious)
				return;

			if (forceShowSpoilers)
				DisplayInfo();
			else HideInfo();
		}
	}
}