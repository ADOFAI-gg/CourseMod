using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Nobi.UiRoundedCorners {
	internal static class DestroyHelper {
		internal static void Destroy(Object @object) {
#if UNITY_EDITOR
			if (Application.isPlaying) {
				Object.Destroy(@object);
			} else {
				Object.DestroyImmediate(@object);
			}
#else
			Object.Destroy(@object);
#endif
		}
	}
}