using CourseMod.Utils;

namespace CourseMod.DataModel {
	public struct LevelProgressFromPatch {
		public int CurrentFloor;
		public HitMargin? CurrentHitMargin;
		public int[] HitMarginsCount;

		public static readonly LevelProgressFromPatch Default = new() {
			CurrentFloor = 0,
			CurrentHitMargin = null,
			HitMarginsCount = HitMarginTools.DefaultHitMarginsCount,
		};
	}
}