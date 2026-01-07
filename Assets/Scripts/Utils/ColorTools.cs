using System;
using System.Collections.Generic;
using UnityEngine;

namespace CourseMod.Utils {
	public static class ColorTools {
		private static readonly Dictionary<string, Color> HtmlColors = new();

		public static Color Html(string color) {
			color = color.ToLowerInvariant();

			if (HtmlColors.TryGetValue(color, out var cachedColor))
				return cachedColor;

			var rawResultSucceeded = ColorUtility.TryParseHtmlString(color, out var rawResult);

			if (!color.StartsWith("#"))
				color = "#" + color;

			var result = ColorUtility.TryParseHtmlString(color, out var hexResult)
				? hexResult
				: rawResultSucceeded
					? rawResult
					: None;

			HtmlColors[color] = result;
			return result;
		}

		public static readonly Color None = new(0, 0, 0, 0);

		public static Color SetAlpha(this Color color, float alpha) {
			color.a = alpha;
			return color;
		}
	}
}