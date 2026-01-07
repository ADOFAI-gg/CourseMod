//Resharper disable CheckNamespace

namespace CourseMod.Components.Atoms.Backdrop {
	public struct BackdropSettings {
		public readonly float TransitionDuration;
		public readonly float Opacity;
		public readonly bool AllowBackdropClickToEnd;
		public readonly bool UseKeyboardControl;

		public BackdropSettings(float transitionDuration, float opacity, bool allowBackdropClickToEnd,
			bool useKeyboardControl) {
			TransitionDuration = transitionDuration;
			Opacity = opacity;
			AllowBackdropClickToEnd = allowBackdropClickToEnd;
			UseKeyboardControl = useKeyboardControl;
		}
	}
}