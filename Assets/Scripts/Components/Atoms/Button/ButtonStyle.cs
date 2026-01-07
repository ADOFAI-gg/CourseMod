using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using CourseMod.Utils;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace CourseMod.Components.Atoms.Button {
	[ExecuteAlways]
	public class ButtonStyle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
		IPointerUpHandler {
		public enum StyleType {
			GhostDark,
			Outlined,
			Primary,
			Danger,
			Item,
			ItemDanger,
			GhostLight,
		}

		private enum FontVariant {
			SemiBold,
			Regular,
			Bold
		}

		private enum ButtonState {
			Default,
			Hover,
			Active,
			Disabled
		}

		private record Rectangle {
			public int Left;
			public int Right;
			public int Top;
			public int Bottom;

			public Rectangle(int left, int right, int top, int bottom) {
				Left = left;
				Right = right;
				Top = top;
				Bottom = bottom;
			}

			public Rectangle(int every, int? left = null, int? right = null, int? top = null, int? bottom = null) {
				Left = Right = Top = Bottom = every;

				if (left is { } l)
					Left = l;

				if (right is { } r)
					Right = r;

				if (top is { } t)
					Top = t;

				if (bottom is { } b)
					Bottom = b;
			}

			public RectOffset ToRectOffset() => new(Left, Right, Top, Bottom);
		}

		private record StyleData {
			public Color BackgroundColor;
			public Color BorderColor;
			public Color TextColor;
			public Color SubTextColor;
			public Rectangle Padding;
			public TextAnchor ChildAlignment;
			public FontVariant FontVariant;
			public bool FillHorizontalAndHugVertical;
		}

		private static readonly IReadOnlyDictionary<StyleType, IReadOnlyDictionary<ButtonState, StyleData>> Styles =
			new Dictionary<StyleType, Dictionary<ButtonState, StyleData>> {
				[StyleType.GhostDark] = new() {
					[ButtonState.Default] =
						new() {
							BackgroundColor = Color.black.SetAlpha(0),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Hover] =
						new() {
							BackgroundColor = Color.black.SetAlpha(.1f),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Active] =
						new() {
							BackgroundColor = Color.black.SetAlpha(.2f),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Disabled] = new() {
						BackgroundColor = Color.black.SetAlpha(0),
						BorderColor = ColorTools.None,
						TextColor = Color.white.SetAlpha(.4f),
						FontVariant = FontVariant.SemiBold,
						SubTextColor = Color.white.SetAlpha(.2f),
						ChildAlignment = TextAnchor.MiddleCenter,
						Padding = new(12, 24, 24)
					},
				},
				[StyleType.GhostLight] = new() {
					[ButtonState.Default] =
						new() {
							BackgroundColor = Color.white.SetAlpha(0),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Hover] =
						new() {
							BackgroundColor = Color.white.SetAlpha(.1f),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Active] =
						new() {
							BackgroundColor = Color.white.SetAlpha(.2f),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Disabled] = new() {
						BackgroundColor = Color.white.SetAlpha(0),
						BorderColor = ColorTools.None,
						TextColor = Color.white.SetAlpha(.4f),
						FontVariant = FontVariant.SemiBold,
						SubTextColor = Color.white.SetAlpha(.2f),
						ChildAlignment = TextAnchor.MiddleCenter,
						Padding = new(12, 24, 24)
					},
				},
				[StyleType.Outlined] = new() {
					[ButtonState.Default] =
						new() {
							BackgroundColor = Color.white.SetAlpha(0),
							BorderColor = Color.white.SetAlpha(.2f),
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Hover] =
						new() {
							BackgroundColor = Color.white.SetAlpha(.1f),
							BorderColor = Color.white.SetAlpha(.2f),
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Active] =
						new() {
							BackgroundColor = Color.white,
							BorderColor = Color.white.SetAlpha(0),
							TextColor = Color.black,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.black.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Disabled] = new() {
						BackgroundColor = Color.white.SetAlpha(0),
						BorderColor = Color.white.SetAlpha(.2f),
						TextColor = Color.white.SetAlpha(.4f),
						FontVariant = FontVariant.SemiBold,
						SubTextColor = Color.white.SetAlpha(.2f),
						ChildAlignment = TextAnchor.MiddleCenter,
						Padding = new(12, 24, 24)
					},
				},
				[StyleType.Primary] = new() {
					[ButtonState.Default] =
						new() {
							BackgroundColor = ColorTools.Html("3B84F1"),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Hover] =
						new() {
							BackgroundColor = ColorTools.Html("337AE3"),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Active] =
						new() {
							BackgroundColor = ColorTools.Html("276BCF"),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Disabled] = new() {
						BackgroundColor = Color.white.SetAlpha(.1f),
						BorderColor = ColorTools.None,
						TextColor = Color.white.SetAlpha(.4f),
						SubTextColor = Color.white.SetAlpha(.2f),
						FontVariant = FontVariant.SemiBold,
						ChildAlignment = TextAnchor.MiddleCenter,
						Padding = new(12, 24, 24)
					},
				},
				[StyleType.Danger] = new() {
					[ButtonState.Default] =
						new() {
							BackgroundColor = ColorTools.Html("F54F51"),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Hover] =
						new() {
							BackgroundColor = ColorTools.Html("DE4446"),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.SemiBold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Active] =
						new() {
							BackgroundColor = ColorTools.Html("CB393B"),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.Bold,
							SubTextColor = Color.white.SetAlpha(.4f),
							ChildAlignment = TextAnchor.MiddleCenter,
							Padding = new(12, 24, 24)
						},
					[ButtonState.Disabled] = new() {
						BackgroundColor = Color.white.SetAlpha(.1f),
						BorderColor = ColorTools.None,
						TextColor = Color.white.SetAlpha(.4f),
						SubTextColor = Color.white.SetAlpha(.2f),
						FontVariant = FontVariant.SemiBold,
						ChildAlignment = TextAnchor.MiddleCenter,
						Padding = new(12, 24, 24)
					},
				},
				[StyleType.Item] = new() {
					[ButtonState.Default] =
						new() {
							BackgroundColor = Color.white.SetAlpha(0),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.Regular,
							SubTextColor = Color.white.SetAlpha(.2f),
							ChildAlignment = TextAnchor.MiddleLeft,
							Padding = new(8, 12, 12),
							FillHorizontalAndHugVertical = true
						},
					[ButtonState.Hover] =
						new() {
							BackgroundColor = Color.white.SetAlpha(.05f),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.Regular,
							SubTextColor = Color.white.SetAlpha(.2f),
							ChildAlignment = TextAnchor.MiddleLeft,
							Padding = new(8, 12, 12),
							FillHorizontalAndHugVertical = true
						},
					[ButtonState.Active] =
						new() {
							BackgroundColor = Color.white.SetAlpha(.1f),
							BorderColor = ColorTools.None,
							TextColor = Color.white,
							FontVariant = FontVariant.Regular,
							SubTextColor = Color.white.SetAlpha(.2f),
							ChildAlignment = TextAnchor.MiddleLeft,
							Padding = new(8, 12, 12),
							FillHorizontalAndHugVertical = true
						},
					[ButtonState.Disabled] = new() {
						BackgroundColor = Color.white.SetAlpha(0),
						BorderColor = ColorTools.None,
						TextColor = Color.white.SetAlpha(.2f),
						FontVariant = FontVariant.Regular,
						SubTextColor = Color.white.SetAlpha(.2f),
						ChildAlignment = TextAnchor.MiddleLeft,
						Padding = new(8, 12, 12),
						FillHorizontalAndHugVertical = true
					},
				},
				[StyleType.ItemDanger] = new() {
					[ButtonState.Default] =
						new() {
							BackgroundColor = ColorTools.Html("F54F51").SetAlpha(0),
							BorderColor = ColorTools.None,
							TextColor = ColorTools.Html("F54F51"),
							SubTextColor = ColorTools.Html("F54F51").SetAlpha(.2f),
							FontVariant = FontVariant.Regular,
							ChildAlignment = TextAnchor.MiddleLeft,
							Padding = new(8, 12, 12),
							FillHorizontalAndHugVertical = true
						},
					[ButtonState.Hover] =
						new() {
							BackgroundColor = ColorTools.Html("F54F51").SetAlpha(.05f),
							BorderColor = ColorTools.None,
							TextColor = ColorTools.Html("F54F51"),
							SubTextColor = ColorTools.Html("F54F51").SetAlpha(.2f),
							FontVariant = FontVariant.Regular,
							ChildAlignment = TextAnchor.MiddleLeft,
							Padding = new(8, 12, 12),
							FillHorizontalAndHugVertical = true
						},
					[ButtonState.Active] =
						new() {
							BackgroundColor = ColorTools.Html("F54F51").SetAlpha(.1f),
							BorderColor = ColorTools.None,
							TextColor = ColorTools.Html("F54F51"),
							SubTextColor = ColorTools.Html("F54F51").SetAlpha(.2f),
							FontVariant = FontVariant.Regular,
							ChildAlignment = TextAnchor.MiddleLeft,
							Padding = new(8, 12, 12),
							FillHorizontalAndHugVertical = true
						},
					[ButtonState.Disabled] = new() {
						BackgroundColor = ColorTools.Html("F54F51").SetAlpha(0),
						BorderColor = ColorTools.None,
						TextColor = ColorTools.Html("F54F51").SetAlpha(.2f),
						SubTextColor = ColorTools.Html("F54F51").SetAlpha(.1f),
						FontVariant = FontVariant.Regular,
						ChildAlignment = TextAnchor.MiddleLeft,
						Padding = new(8, 12, 12),
						FillHorizontalAndHugVertical = true
					},
				},
			}.Select(p =>
				new KeyValuePair<StyleType, IReadOnlyDictionary<ButtonState, StyleData>>(p.Key,
					p.Value.ToImmutableDictionary())).ToImmutableDictionary();

		[Header("References")] public UnityEngine.UI.Button button;
		public TextMeshProUGUI buttonText;
		public TextMeshProUGUI subText;
		public Image buttonBackground;
		public Image buttonBorder;
		public HorizontalLayoutGroup horizontalLayoutGroup;
		public LayoutElement layoutElement;

		[Header("Customization")] public StyleType selectedStyle;

		[Header("Others")] public TMP_FontAsset regularFontAsset;
		public TMP_FontAsset semiBoldFontAsset;

		public bool Disabled {
			get => _disabled;
			set {
				button.interactable = !(_disabled = value);
				UpdateButtonAppearance();
			}
		}

		private const float TransitionDuration = .2f;
		private Tween _backgroundTween;
		private Tween _textTween;
		private Tween _subTextTween;

		private StyleType _lastSelectedStyle = (StyleType) (-1);
		private ButtonState _lastButtonState = (ButtonState) (-1);

		private bool _hovered;
		private bool _active;
		private bool _disabled;

		private void Start() => UpdateButtonAppearance(true);

		private void UpdateButtonAppearance(bool skipTransition = false) {
			if (!button.interactable) {
				ChangeButtonAppearance(ButtonState.Disabled, skipTransition);
				return;
			}

			var state = _active
				? ButtonState.Active
				: _hovered
					? ButtonState.Hover
					: ButtonState.Default;

			ChangeButtonAppearance(state, skipTransition);
		}

		private void ChangeButtonAppearance(ButtonState state, bool skipTransition) {
			if (_lastSelectedStyle == selectedStyle && _lastButtonState == state) return;

			if (!Styles.TryGetValue(selectedStyle, out var styleInfo)) {
				Debug.LogWarning(
					$"Button Style Type '{selectedStyle}' does not exist. (From object {name}/{GetInstanceID()})");
				return;
			}

			var style = styleInfo[state];

			if (skipTransition)
				buttonBackground.color = style.BackgroundColor;
			else {
				_backgroundTween?.Kill();
				_backgroundTween = buttonBackground.DOColor(style.BackgroundColor, TransitionDuration).SetUpdate(true)
					.Done();
			}

			if (skipTransition)
				buttonText.color = style.TextColor;
			else {
				_textTween?.Kill();
				_textTween = buttonText.DOColor(style.TextColor, TransitionDuration).SetUpdate(true).Done();
			}

			if (skipTransition)
				subText.color = style.SubTextColor;
			else {
				_subTextTween?.Kill();
				_subTextTween = subText.DOColor(style.SubTextColor, TransitionDuration).SetUpdate(true).Done();
			}

			// no animations
			buttonBorder.gameObject.SetActive(style.BorderColor != ColorTools.None);
			buttonBorder.color = style.BorderColor;

			subText.gameObject.SetActive(!string.IsNullOrEmpty(subText.text));

			horizontalLayoutGroup.childAlignment = style.ChildAlignment;
			horizontalLayoutGroup.padding = style.Padding.ToRectOffset();

			if (style.FillHorizontalAndHugVertical) {
				layoutElement.preferredHeight = -1;
				layoutElement.flexibleWidth = 1;
				layoutElement.flexibleHeight = 0;
			} else {
				layoutElement.preferredHeight = 44;
				layoutElement.flexibleWidth = -1;
				layoutElement.flexibleHeight = -1;
			}

			var styleFontAsset = style.FontVariant == FontVariant.Regular ? regularFontAsset : semiBoldFontAsset;

			// query font change only if the font is going to change
			if (buttonText.font != styleFontAsset)
				buttonText.font = styleFontAsset;

			if (subText.font != styleFontAsset)
				subText.font = styleFontAsset;

			_lastSelectedStyle = selectedStyle;
			_lastButtonState = state;
		}

		public void OnPointerEnter(PointerEventData _) {
			_hovered = true;
			UpdateButtonAppearance();
		}

		public void OnPointerExit(PointerEventData _) {
			_hovered = false;
			UpdateButtonAppearance();
		}

		public void OnPointerDown(PointerEventData _) {
			_active = true;
			UpdateButtonAppearance(true);
		}

		public void OnPointerUp(PointerEventData _) {
			_active = false;
			UpdateButtonAppearance(true);
		}

#if UNITY_EDITOR
		private void OnValidate() {
			UpdateButtonAppearance(true);
		}
#endif
	}
}