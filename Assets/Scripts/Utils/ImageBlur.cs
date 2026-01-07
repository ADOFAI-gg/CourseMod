using UnityEngine;
using UnityEngine.UI;

namespace CourseMod.Utils {
	public static class ImageBlur {
		public static Material BlurMaterial;

		public static void PerformBlur(Image image, RenderTexture dest) => PerformBlur(image.mainTexture, dest);

		public static void PerformBlur(Texture texture, RenderTexture dest) {
			Graphics.Blit(texture, dest,
				BlurMaterial ??= Assets.LoadAsset<Material>("Assets/Resources/Blur Material.mat"));
		}
	}
}