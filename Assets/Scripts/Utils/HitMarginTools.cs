namespace CourseMod.Utils {
	public static class HitMarginTools {
		public static bool TryGetHitMarginCount(int[] hitMargins, HitMargin hitMargin, out int count) {
			count = 0;

			var index = (int) hitMargin;
			if (hitMargins.Length <= index)
				return false;

			count = hitMargins[index];
			return true;
		}
	}
}