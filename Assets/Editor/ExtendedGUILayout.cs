using UnityEditor;
using UnityEngine;

namespace CourseMod.Editor {
	internal static class ExtendedGUILayout {
		private static bool _hasSetupGUIStyle;

		internal static void SetupGUIStyles() {
			if (_hasSetupGUIStyle) return;

			// Setup GUIStyles
			var labelStyle = GUI.skin.label;
			labelStyle.richText = true; // <b></b> works
			labelStyle.wordWrap = true; // long text is not cut out

			var buttonStyle = GUI.skin.button;
			buttonStyle.richText = true;

			var fontColor =
				EditorGUIUtility.isProSkin ? Color.white : Color.black; // black or white depending on theme

			labelStyle.normal.textColor =
				labelStyle.hover.textColor =
					labelStyle.active.textColor =
						labelStyle.focused.textColor = fontColor;

			var foldoutStyle = EditorStyles.foldout;
			foldoutStyle.richText = true;

			_hasSetupGUIStyle = true;
		}

		internal static void SectionTitle(string label, float topMargin = 24, float bottomMargin = 4,
			float topLineMargin = 0, float bottomLineMargin = 0) {
			GUILayout.Space(topMargin);

			GUILayout.Label($"<b>{label}</b>");
			DrawLineWithMargin(topLineMargin, bottomLineMargin, lineColor: Color.white);

			GUILayout.Space(bottomMargin);
		}

		internal static void SetGUIBackgroundColor(Color color) {
			if (EditorGUIUtility.isProSkin) {
				GUI.backgroundColor = color;
			} else {
				var change = 0.4f;
				GUI.backgroundColor = new Color(color.r + change, color.g + change, color.b + change, 1f);
			}
		}

		internal static void DrawLine(float height = 1, Color? lineColor = null) {
			var rect = EditorGUILayout.GetControlRect(false, height);
			rect.height = height;

			EditorGUI.DrawRect(rect, lineColor ?? new Color(.098f, .098f, .098f));
		}

		internal static void DrawLineWithMargin(float topMargin = 0, float bottomMargin = 9, float height = 1,
			Color? lineColor = null) {
			GUILayout.Space(topMargin);
			DrawLine(height, lineColor);
			GUILayout.Space(bottomMargin);
		}

		/// <summary>
		/// Begins an indented section with a given indentation size.
		/// </summary>
		/// <param name="indentSize">The size of the indentation.</param>
		internal static void BeginIndent(float indentSize = 16f) {
			GUILayout.BeginHorizontal();
			GUILayout.Space(indentSize);
			GUILayout.BeginVertical();
		}

		/// <summary>
		/// Ends an indented section.
		/// </summary>
		internal static void EndIndent() {
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		internal class IndentScope : System.IDisposable {
			public IndentScope(float indentSize = 16f) {
				BeginIndent(indentSize);
			}

			public void Dispose() {
				EndIndent();
			}
		}
	}
}