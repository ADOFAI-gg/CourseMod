using System;
using DG.Tweening;

namespace CourseMod.Utils {
	public static class SceneTools {
		public static void LoadSceneAnimated(Action onComplete, Action onCancel) {
			DOTween.KillAll(); //Kill all existing DOTween because all target tween objects will be destroyed when loading scene
			scrUIController.instance.WipeToBlack(WipeDirection.StartsFromRight, onComplete, onCancel);
			//TODO Wipe from black
		}
	}
}