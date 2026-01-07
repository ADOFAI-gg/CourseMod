using System.IO;
using UnityEngine;

namespace CourseMod.Utils {
	public static class ImageTools {
		public static Sprite OpenSprite(string path) {
			var tex = OpenTexture2D(path);
			if (tex == null) return null;

			var rect = new Rect(0, 0, tex.width, tex.height);

			return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100);
		}

		public static Texture2D OpenTexture2D(string path) {
			if (!File.Exists(path))
				return null;

			var binary = File.ReadAllBytes(path);
			var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

			if (!tex.LoadImage(binary))
				return null;

			tex.wrapMode = TextureWrapMode.Clamp;
			return tex;
		}
	}
}