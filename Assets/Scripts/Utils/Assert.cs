using CourseMod.Exceptions;

namespace CourseMod.Utils {
	public static class Assert {
		public static void True(bool condition, string failMessage) {
			if (!condition) throw new AssertionException(failMessage ?? "Assertion Failed");
		}

		public static void False(bool condition, string failMessage) {
			if (condition) throw new AssertionException(failMessage ?? "Assertion Failed");
		}
	}
}