using System;
using System.IO;
using JetBrains.Annotations;

namespace CourseMod.Utils {
	public static class StringTools {
		public static int? GetNullOrParsedInt(string s) {
			if (string.IsNullOrEmpty(s))
				return null;

			if (int.TryParse(s, out var parsed))
				return parsed;

			return null;
		}

		public static double? GetNullOrParsedDouble(string s) {
			if (string.IsNullOrEmpty(s))
				return null;

			if (double.TryParse(s, out var parsed))
				return parsed;

			return null;
		}

		public static float? GetNullOrParsedFloat(string s) {
			if (string.IsNullOrEmpty(s))
				return null;

			if (float.TryParse(s, out var parsed))
				return parsed;

			return null;
		}

		public static string WrapRichTag(this string s, string tag, [CanBeNull] string arg = null) {
			if (string.IsNullOrWhiteSpace(tag)) return s;

			var argStr = arg ?? "";
			if (argStr.Length > 0)
				argStr = "=" + argStr;

			return $"<{tag}{argStr}>{s}</{tag}>";
		}

		public static string WrapColorTag(this string s, string hex) => s.WrapRichTag("color", $"#{hex}");
		public static string GoldTextIfTrue(this string s, bool value) => value ? s.WrapColorTag("ffda00") : s;

		public static string ToAccuracyNotation(this double xAcc, bool fullDigits = true) =>
			xAcc.ToString(fullDigits ? "0.0000%" : "0.####%");

		public static string SanitizeForUI(this string s) {
			s = s.RemoveRichTags()
				.Replace('\t', ' ')
				.Replace('\n', ' ')
				.Replace("\r", "");

			return s;
		}
		
		public static string CombinePathNullable([CanBeNull] string a, string b) =>
			string.IsNullOrEmpty(a) ? b : Path.Combine(a, b);
	}
}