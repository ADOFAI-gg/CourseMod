using System;

namespace CourseMod.Exceptions {
	public sealed class AssertionException : Exception {
		public AssertionException(string message) : base(message) {
		}
	}
}