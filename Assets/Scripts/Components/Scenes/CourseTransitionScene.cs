using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CourseMod.Components.Atoms;
using CourseMod.Components.Atoms.HitMarginDisplay;
using CourseMod.Components.Molecules.SelectLevelResultItem;
using CourseMod.DataModel;
using CourseMod.Exceptions;
using CourseMod.Patches;
using CourseMod.Utils;
using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CourseMod.Components.Scenes {
	public class CourseTransitionScene : MonoBehaviour {
		[UsedImplicitly] public const string SCENE_NAME = "CourseTransition";

		[Header("Prefabs")] public SelectLevelResultItem levelResultItemPrefab;

		[Header("Background")] public RawImage background;

		[Header("Main Containers")] public CanvasGroup courseStartUI;
		public CanvasGroup courseEndUI;
		public CanvasGroup courseSidebarUI;
		public CanvasGroup courseCountdownUI;

		[Header("Start UI")] public TextMeshProUGUI startMenuCourseName;
		public GameObject startMenuClearChallengeContainer;
		public GameObject startMenuAccuracyConstraintChip;
		public GameObject startMenuDeathConstraintChip;
		public GameObject startMenuLifeConstraintChip;
		public TextMeshProUGUI startMenuAccuracyConstraintText;
		public TextMeshProUGUI startMenuDeathConstraintText;
		public TextMeshProUGUI startMenuLifeConstraintText;

		public GameObject startMenuLevelItem1;
		public GameObject startMenuLevelItem2;
		public GameObject startMenuLevelItem3;
		public GameObject startMenuLevelItem4;
		public GameObject startMenuLevelItemEtc;
		public TextMeshProUGUI startMenuLevelItem1Text;
		public TextMeshProUGUI startMenuLevelItem2Text;
		public TextMeshProUGUI startMenuLevelItem3Text;
		public TextMeshProUGUI startMenuLevelItem4Text;
		public TextI18N startMenuLevelItemEtcText;

		[Header("End UI")] public TextMeshProUGUI endMenuTitle;
		public TextMeshProUGUI endMenuCourseName;

		public Button endMenuRetryButton;
		public Button endMenuExitButton;

		public GameObject endMenuClearChallengeContainer;
		public Image endMenuAccuracyConstraintChip;
		public Image endMenuDeathConstraintChip;
		public Image endMenuLifeConstraintChip;
		public Image endMenuAccuracyConstraintChipIcon;
		public Image endMenuDeathConstraintChipIcon;
		public Image endMenuLifeConstraintChipIcon;
		public TextMeshProUGUI endMenuAccuracyConstraintText;
		public TextMeshProUGUI endMenuDeathConstraintText;
		public TextMeshProUGUI endMenuLifeConstraintText;
		public TextMeshProUGUI endMenuDeathConstraintSubText;
		public TextMeshProUGUI endMenuLifeConstraintSubText;

		public RectTransform endMenuLevelItemsContainer;

		public TextMeshProUGUI endMenuTotalAccuracyText;
		public GameObject endMenuPersonalBestText;
		public HitMarginDisplay endMenuHitMarginDisplay;

		[Header("Sidebar UI")] public TextMeshProUGUI sidebarCourseName;
		public SelectLevelResultItem sidebarLevelItem1;
		public SelectLevelResultItem sidebarLevelItem2;
		public SelectLevelResultItem sidebarLevelItem3;
		public SelectLevelResultItem sidebarLevelItem4;
		public SelectLevelResultItem sidebarLevelEtcItemTop;
		public SelectLevelResultItem sidebarLevelEtcItemBottom;

		public GameObject sidebarClearChallengeContainer;
		public GameObject sidebarAccuracyConstraintChip;
		public GameObject sidebarDeathConstraintChip;
		public GameObject sidebarLifeConstraintChip;
		public Image sidebarAccuracyConstraintChipBackground;
		public Image sidebarDeathConstraintChipBackground;
		public Image sidebarLifeConstraintChipBackground;
		public Image sidebarAccuracyConstraintChipIcon;
		public Image sidebarDeathConstraintChipIcon;
		public Image sidebarLifeConstraintChipIcon;
		public TextMeshProUGUI sidebarAccuracyConstraintText;
		public TextMeshProUGUI sidebarDeathConstraintText;
		public TextMeshProUGUI sidebarLifeConstraintText;
		public TextMeshProUGUI sidebarAccuracyConstraintSubText;
		public TextMeshProUGUI sidebarDeathConstraintSubText;
		public TextMeshProUGUI sidebarLifeConstraintSubText;

		[Header("Countdown UI")] public TextMeshProUGUI countdownNumber;
		public TextMeshProUGUI countdownSkip;

		[NonSerialized] public bool EnableSidebarMenuOnGameScene;

		// Keep in mind this scene is always side-loaded along with game scene or course scenes

		[CanBeNull] public static CourseTransitionScene Instance;

		private double _countdownValue;
		private bool _performCountdown;
		private bool _allowCountdownSkip;
		private bool _displayingStartScreen;

		private Tween _backgroundTween;
		private Tween _contentTween;
		private Tween _subContentTween;
		private readonly Tween[] _flashTweens = new Tween[4];

		private Color _chipBackgroundColor;
		private Color _chipIconColor;
		private Color _chipTextColor;
		private Color _chipSubTextColor;

		private bool _leavingScene;
		private bool _displayingEndScreen;

		private float ContentDisplayTime { get; set; }

		public static string CourseEnteredSceneName = null;

		private const string MysteriousLevelCover = "???";

		private readonly List<SelectLevelResultItem> _endMenuLevelItems = new();

		private void Awake() {
			Instance = this;

			_chipBackgroundColor = sidebarAccuracyConstraintChipBackground.color;
			_chipIconColor = sidebarAccuracyConstraintChipIcon.color;
			_chipTextColor = sidebarAccuracyConstraintText.color;
			_chipSubTextColor = sidebarDeathConstraintSubText.color;
		}

		private void Start() {
#if DEBUG
			EnableSidebarMenuOnGameScene = ModDataStorage.PlayerSettings.DebugSettings.ForceEnableTransitionSidebarMenu;
#endif

			endMenuExitButton.onClick.AddListener(QuitToLastScene);
			endMenuRetryButton.onClick.AddListener(RetryCourse);

			if (GameplayPatches.CourseState.SelectedCourse.HasValue) {
				SetCourseInfo();
				ShowStartScreen();
			} else {
				ResetCourseInfo();
			}
		}

		private void Update() {
			UpdateCountdown();
			UpdateContentScreen();

			var mainPress =
#if UNITY_EDITOR
				Input.GetKeyDown(KeyCode.Return);
#else
				RDInput.mainPress;
#endif

			if (_displayingEndScreen) {
				if (Input.GetKeyDown(KeyCode.Escape)) {
					QuitToLastScene();
					return;
				}

				if (Input.GetKeyDown(KeyCode.R)) {
					RetryCourse();
					return;
				}

				return;
			}

			if (mainPress) {
				if (_allowCountdownSkip) {
					SkipCountdown();
					return;
				}

				if (_displayingStartScreen) {
					ContentDisplayTime = float.Epsilon;
					return;
				}
			}
		}

		private void OnDestroy() {
			ReleaseTexture();
		}

		private void ReleaseTexture() {
			var texture = background.texture;
			if (texture == null)
				return;

			if (background.texture is RenderTexture garbage) {
				garbage.DiscardContents();
				garbage.Release();
			}

			Destroy(background.texture);
		}

		private void ResetCourseInfo() {
			_backgroundTween?.Kill(true);
			_backgroundTween = null;

			ReleaseTexture();
			background.texture = null;
			background.color = ColorTools.Html("232B5A");

			endMenuAccuracyConstraintChip.gameObject.SetActive(false);
			startMenuAccuracyConstraintChip.gameObject.SetActive(false);
			sidebarAccuracyConstraintChip.gameObject.SetActive(false);
			endMenuDeathConstraintChip.gameObject.SetActive(false);
			startMenuDeathConstraintChip.gameObject.SetActive(false);
			sidebarDeathConstraintChip.gameObject.SetActive(false);
			endMenuLifeConstraintChip.gameObject.SetActive(false);
			startMenuLifeConstraintChip.gameObject.SetActive(false);
			sidebarLifeConstraintChip.gameObject.SetActive(false);

			endMenuAccuracyConstraintChip.color = Color.white.SetAlpha(endMenuAccuracyConstraintChip.color.a);
			endMenuDeathConstraintChip.color = Color.white.SetAlpha(endMenuDeathConstraintChip.color.a);
			endMenuLifeConstraintChip.color = Color.white.SetAlpha(endMenuLifeConstraintChip.color.a);
			endMenuAccuracyConstraintChipIcon.color = Color.white.SetAlpha(endMenuAccuracyConstraintChipIcon.color.a);
			endMenuDeathConstraintChipIcon.color = Color.white.SetAlpha(endMenuDeathConstraintChipIcon.color.a);
			endMenuLifeConstraintChipIcon.color = Color.white.SetAlpha(endMenuLifeConstraintChipIcon.color.a);
			endMenuAccuracyConstraintText.color = Color.white.SetAlpha(endMenuAccuracyConstraintText.color.a);
			endMenuDeathConstraintText.color = Color.white.SetAlpha(endMenuDeathConstraintText.color.a);
			endMenuLifeConstraintText.color = Color.white.SetAlpha(endMenuLifeConstraintText.color.a);
			endMenuDeathConstraintSubText.color = Color.white.SetAlpha(endMenuDeathConstraintSubText.color.a);
			endMenuLifeConstraintSubText.color = Color.white.SetAlpha(endMenuLifeConstraintSubText.color.a);

			startMenuLevelItem1.gameObject.SetActive(false);
			startMenuLevelItem2.gameObject.SetActive(false);
			startMenuLevelItem3.gameObject.SetActive(false);
			startMenuLevelItem4.gameObject.SetActive(false);
			startMenuLevelItemEtc.gameObject.SetActive(false);

			sidebarLevelItem1.gameObject.SetActive(false);
			sidebarLevelItem2.gameObject.SetActive(false);
			sidebarLevelItem3.gameObject.SetActive(false);
			sidebarLevelItem4.gameObject.SetActive(false);
			sidebarLevelEtcItemTop.gameObject.SetActive(false);
			sidebarLevelEtcItemBottom.gameObject.SetActive(false);

			_endMenuLevelItems.Clear();

			for (var i = 0; i < endMenuLevelItemsContainer.childCount; i++) {
				var child = endMenuLevelItemsContainer.GetChild(i);
				Destroy(child.gameObject);
			}

			_allowCountdownSkip = false;
			countdownSkip.color = Color.white.SetAlpha(0);
		}

		private void SetCourseInfo() {
			var selectedCourse = GameplayPatches.CourseState.SelectedCourse;

			if (!selectedCourse.HasValue)
				return;

			ResetCourseInfo();

			var course = selectedCourse.Value;
			var settings = course.Settings;

			var usesAnyConstraint = false;

			if (settings.ThumbnailFile is { } thumbnailFile) {
				var texture =
					ImageTools.OpenTexture2D(Path.Combine(Path.GetDirectoryName(course.FilePath)!, thumbnailFile));

				if (texture) {
					var renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
					renderTexture.Create();
					ImageBlur.PerformBlur(texture, renderTexture);

					background.color = (Color.white / 2).SetAlpha(1);
					background.texture = renderTexture;
				}
			}

			startMenuCourseName.text = sidebarCourseName.text = endMenuCourseName.text = course.Name.SanitizeForUI();

			if (settings.AccuracyConstraint is { } accuracy) {
				endMenuAccuracyConstraintChip.gameObject.SetActive(true);
				startMenuAccuracyConstraintChip.gameObject.SetActive(true);
				sidebarAccuracyConstraintChip.gameObject.SetActive(true);

				usesAnyConstraint = true;
				endMenuAccuracyConstraintText.text = startMenuAccuracyConstraintText.text =
					sidebarAccuracyConstraintText.text =
						sidebarAccuracyConstraintSubText.text =
							$"≥ {accuracy.ToAccuracyNotation(false)} {I18N.Get("general-accuracy-constraint")}";

				if (EnableSidebarMenuOnGameScene) {
					sidebarAccuracyConstraintText.text = course.Levels[0].DisableAccuracyConstraint
						? "-"
						: 1d.ToAccuracyNotation();
				}

				sidebarAccuracyConstraintSubText.gameObject.SetActive(EnableSidebarMenuOnGameScene);
			}

			if (settings.DeathConstraint is { } death) {
				endMenuDeathConstraintChip.gameObject.SetActive(true);
				startMenuDeathConstraintChip.gameObject.SetActive(true);
				sidebarDeathConstraintChip.gameObject.SetActive(true);

				usesAnyConstraint = true;
				endMenuDeathConstraintSubText.text = startMenuDeathConstraintText.text =
					sidebarDeathConstraintSubText.text = $"{death} {I18N.Get("general-death-constraint")}";
			}

			if (settings.LifeConstraint is { } life) {
				endMenuLifeConstraintChip.gameObject.SetActive(true);
				startMenuLifeConstraintChip.gameObject.SetActive(true);
				sidebarLifeConstraintChip.gameObject.SetActive(true);

				usesAnyConstraint = true;
				endMenuLifeConstraintSubText.text = startMenuLifeConstraintText.text =
					sidebarLifeConstraintSubText.text = $"{life} {I18N.Get("general-life-constraint")}";
			}

			endMenuClearChallengeContainer.gameObject.SetActive(usesAnyConstraint);
			startMenuClearChallengeContainer.gameObject.SetActive(usesAnyConstraint);
			sidebarClearChallengeContainer.gameObject.SetActive(usesAnyConstraint);

			sidebarLevelItem1.gameObject.SetActive(false);
			sidebarLevelItem2.gameObject.SetActive(false);
			sidebarLevelItem3.gameObject.SetActive(false);
			sidebarLevelItem4.gameObject.SetActive(false);
			sidebarLevelEtcItemTop.gameObject.SetActive(false);
			sidebarLevelEtcItemBottom.gameObject.SetActive(false);

			var levels = course.Levels;
			for (var i = 0; i < levels.Count; i++) {
				var level = levels[i];
				var levelMeta = level.LevelMeta;

				switch (i) {
					case 0:
						startMenuLevelItem1.SetActive(true);
						startMenuLevelItem1Text.text = level.Mysterious ? MysteriousLevelCover : levelMeta.Song;
						break;
					case 1:
						startMenuLevelItem2.SetActive(true);
						startMenuLevelItem2Text.text = level.Mysterious ? MysteriousLevelCover : levelMeta.Song;
						break;
					case 2:
						startMenuLevelItem3.SetActive(true);
						startMenuLevelItem3Text.text = level.Mysterious ? MysteriousLevelCover : levelMeta.Song;
						break;
					case 3:
						startMenuLevelItem4.SetActive(true);
						startMenuLevelItem4Text.text = level.Mysterious ? MysteriousLevelCover : levelMeta.Song;
						break;
					case 4:
						startMenuLevelItemEtc.SetActive(true);
						startMenuLevelItemEtcText.UpdateArguments(new() { ["count"] = levels.Count - 4 });
						break;
				}

				var endResultItem = Instantiate(levelResultItemPrefab, endMenuLevelItemsContainer);
				var record = GameplayPatches.CourseState.LevelPlayRecords.ElementAtOrDefault(i);
				endResultItem.UpdateDisplay(level.LevelMeta.Song, i + 1, record);
				_endMenuLevelItems.Add(endResultItem);

				// item visibility by _levelIndex
				// 0 = [0, 1, 2, 3] etcBottom
				// 1 = [0, 1, 2, 3] etcBottom
				// 2 = [0, 1, 2, 3] etcBottom
				// 3 = etcTop [1, 2, 3, 4] etcBottom
				// 4 = etcTop [2, 3, 4, 5] etcBottom
				// ...

				UpdateCourseSidebarLevelItem(i, level, record, levels.Count);
			}

			UpdateConstraintStrings();
		}

		private void UpdateConstraintStrings(double? newXAccDisplay = null) {
			var selectedCourse = GameplayPatches.CourseState.SelectedCourse;

			if (selectedCourse is not { } course) return;
			var settings = course.Settings;

			var xAccLimit = settings.AccuracyConstraint;
			var totalDeath = settings.DeathConstraint;
			var totalLife = settings.LifeConstraint;

			if (newXAccDisplay is { } newXAcc && xAccLimit != null) {
				var levelIndex = GameplayPatches.CourseState.LevelIndex;
				var currentLevel = course.Levels.Count <= levelIndex + 1
					? course.Levels.Last()
					: course.Levels[levelIndex];

				sidebarAccuracyConstraintText.text = currentLevel.DisableAccuracyConstraint
					? "-"
					: newXAcc.ToAccuracyNotation();
			}

			if (totalDeath != null) {
				endMenuDeathConstraintText.text =
					sidebarDeathConstraintText.text = (totalDeath - GameplayPatches.CourseState.DeathsLeft).ToString();
			}

			if (totalLife != null) {
				endMenuLifeConstraintText.text =
					sidebarLifeConstraintText.text = (totalLife - GameplayPatches.CourseState.LivesLeft).ToString();
			}
		}

		public void UpdateConstraintChip(GameplayPatches.CourseState.FailReason flashChipType, bool flash,
			double? currentMaxPossibleXAcc) {
			UpdateConstraintStrings(currentMaxPossibleXAcc);
			if (!flash) return;

			Image bg;
			Image icon;
			TextMeshProUGUI text;
			TextMeshProUGUI subText;

			switch (flashChipType) {
				case GameplayPatches.CourseState.FailReason.Accuracy:
					bg = sidebarAccuracyConstraintChipBackground;
					icon = sidebarAccuracyConstraintChipIcon;
					text = sidebarAccuracyConstraintText;
					subText = sidebarAccuracyConstraintSubText;
					break;
				case GameplayPatches.CourseState.FailReason.Death:
					bg = sidebarDeathConstraintChipBackground;
					icon = sidebarDeathConstraintChipIcon;
					text = sidebarDeathConstraintText;
					subText = sidebarDeathConstraintSubText;
					break;
				case GameplayPatches.CourseState.FailReason.Life:
					bg = sidebarLifeConstraintChipBackground;
					icon = sidebarLifeConstraintChipIcon;
					text = sidebarLifeConstraintText;
					subText = sidebarLifeConstraintSubText;
					break;
				default: // wtf
					return;
			}

			bg.color = ColorTools.Html("F54F51").SetAlpha(bg.color.a);
			_flashTweens[0]?.Kill();
			_flashTweens[0] = bg.DOColor(_chipBackgroundColor, .2f)
				.SetUpdate(true);

			icon.color = ColorTools.Html("F54F51").SetAlpha(icon.color.a);
			_flashTweens[1]?.Kill();
			_flashTweens[1] = icon.DOColor(_chipIconColor, .2f)
				.SetUpdate(true);

			text.color = ColorTools.Html("F54F51").SetAlpha(text.color.a);
			_flashTweens[2]?.Kill();
			_flashTweens[2] = text.DOColor(_chipTextColor, .2f)
				.SetUpdate(true);

			subText.color = ColorTools.Html("F54F51").SetAlpha(subText.color.a);
			_flashTweens[3]?.Kill();
			_flashTweens[3] = subText.DOColor(_chipSubTextColor, .2f)
				.SetUpdate(true);
		}

		private void ResetConstraintChips() {
			sidebarDeathConstraintText.text =
				sidebarLifeConstraintText.text = "0";

			if (GameplayPatches.CourseState.SelectedCourse is not { } course)
				return;

			sidebarAccuracyConstraintText.text = course.Levels[0].DisableAccuracyConstraint
				? "-"
				: 1d.ToAccuracyNotation();
		}

		private void UpdateCourseEndUI() {
			if (GameplayPatches.CourseState.SelectedCourse is not { } course)
				return;

			LogTools.Log("Transition - UpdateCourseEndUI()");

			endMenuTitle.text =
				I18N.Get($"transition-course-{(GameplayPatches.CourseState.Failed ? "fail" : "clear")}");

			for (var i = 0; i < _endMenuLevelItems.Count; i++) {
				var item = _endMenuLevelItems[i];
				var record = GameplayPatches.CourseState.LevelPlayRecords.ElementAtOrDefault(i);

				item.UpdateDisplay(record);

				if (course.Levels[i].Mysterious && i >= GameplayPatches.CourseState.LevelIndex)
					item.levelName.text = MysteriousLevelCover;
			}

			var totalAccuracy = GameplayPatches.CourseState.TotalXAccuracy;
			var totalMargins = GameplayPatches.CourseState.TotalHitMargins;
			var totalFloors = GameplayPatches.CourseState.TotalFloors;
			var previousPlayRecord = course.GetPlayRecord();
			var personalBest = previousPlayRecord == null || previousPlayRecord.TotalAccuracy < totalAccuracy;

			endMenuTotalAccuracyText.text = totalAccuracy
				.ToAccuracyNotation()
				.GoldTextIfTrue(totalMargins.IsPurePerfect(totalFloors));

			endMenuPersonalBestText.SetActive(personalBest);
			endMenuHitMarginDisplay.UpdateDisplay(totalMargins);

			UpdateConstraintStrings();

			endMenuAccuracyConstraintChip.color = Color.white.SetAlpha(endMenuAccuracyConstraintChip.color.a);
			endMenuDeathConstraintChip.color = Color.white.SetAlpha(endMenuDeathConstraintChip.color.a);
			endMenuLifeConstraintChip.color = Color.white.SetAlpha(endMenuLifeConstraintChip.color.a);
			endMenuAccuracyConstraintChipIcon.color = Color.white.SetAlpha(endMenuAccuracyConstraintChipIcon.color.a);
			endMenuDeathConstraintChipIcon.color = Color.white.SetAlpha(endMenuDeathConstraintChipIcon.color.a);
			endMenuLifeConstraintChipIcon.color = Color.white.SetAlpha(endMenuLifeConstraintChipIcon.color.a);
			endMenuAccuracyConstraintText.color = Color.white.SetAlpha(endMenuAccuracyConstraintText.color.a);
			endMenuDeathConstraintText.color = Color.white.SetAlpha(endMenuDeathConstraintText.color.a);
			endMenuLifeConstraintText.color = Color.white.SetAlpha(endMenuLifeConstraintText.color.a);
			endMenuDeathConstraintSubText.color = Color.white.SetAlpha(endMenuDeathConstraintSubText.color.a);
			endMenuLifeConstraintSubText.color = Color.white.SetAlpha(endMenuLifeConstraintSubText.color.a);

			var red = ColorTools.Html("f54f51");
			foreach (var reason in GameplayPatches.CourseState.FailReasons) {
				var chipBackground = reason switch {
					GameplayPatches.CourseState.FailReason.Accuracy => endMenuAccuracyConstraintChip,
					GameplayPatches.CourseState.FailReason.Death => endMenuDeathConstraintChip,
					GameplayPatches.CourseState.FailReason.Life => endMenuLifeConstraintChip,
					_ => null
				};

				if (chipBackground)
					chipBackground.color = red.SetAlpha(chipBackground.color.a);

				var chipIcon = reason switch {
					GameplayPatches.CourseState.FailReason.Accuracy => endMenuAccuracyConstraintChipIcon,
					GameplayPatches.CourseState.FailReason.Death => endMenuDeathConstraintChipIcon,
					GameplayPatches.CourseState.FailReason.Life => endMenuLifeConstraintChipIcon,
					_ => null
				};

				if (chipIcon)
					chipIcon.color = red.SetAlpha(chipIcon.color.a);

				var chipText = reason switch {
					GameplayPatches.CourseState.FailReason.Accuracy => endMenuAccuracyConstraintText,
					GameplayPatches.CourseState.FailReason.Death => endMenuDeathConstraintText,
					GameplayPatches.CourseState.FailReason.Life => endMenuLifeConstraintText,
					_ => null
				};

				if (chipText)
					chipText.color = red.SetAlpha(chipText.color.a);

				var chipSubText = reason switch {
					GameplayPatches.CourseState.FailReason.Death => endMenuDeathConstraintSubText,
					GameplayPatches.CourseState.FailReason.Life => endMenuLifeConstraintSubText,
					_ => null
				};

				if (chipSubText)
					chipSubText.color = red.SetAlpha(chipSubText.color.a);
			}

			if (personalBest)
				GameplayPatches.CourseState.SaveRecord();
		}

		private void UpdateCourseSidebarUI() {
			if (GameplayPatches.CourseState.SelectedCourse is not { } course)
				return;

			sidebarLevelItem1.gameObject.SetActive(false);
			sidebarLevelItem2.gameObject.SetActive(false);
			sidebarLevelItem3.gameObject.SetActive(false);
			sidebarLevelItem4.gameObject.SetActive(false);
			sidebarLevelEtcItemTop.gameObject.SetActive(false);
			sidebarLevelEtcItemBottom.gameObject.SetActive(false);

			var levels = course.Levels;
			for (var i = 0; i < levels.Count; i++) {
				var level = levels[i];
				var record = GameplayPatches.CourseState.LevelPlayRecords.ElementAtOrDefault(i);
				UpdateCourseSidebarLevelItem(i, level, record, levels.Count);
			}
		}

		private void UpdateCourseSidebarLevelItem(int i, CourseLevel level, CourseLevelPlayRecord record,
			int levelsCount) {
			if (GameplayPatches.CourseState.SelectedCourse is null)
				return;

			var currentLevelIndex = GameplayPatches.CourseState.LevelIndex;
			var listMoveOffset = Math.Max(-1, currentLevelIndex - 2);
			var levelMeta = level.LevelMeta;

			switch (i - listMoveOffset) {
				case -1:
					sidebarLevelEtcItemTop.gameObject.SetActive(true);
					sidebarLevelEtcItemTop.UpdateEtc(i + 1);
					break;
				case 0:
					sidebarLevelItem1.gameObject.SetActive(true);
					sidebarLevelItem1.UpdateDisplay(
						level.Mysterious && i >= currentLevelIndex ? MysteriousLevelCover : levelMeta.Song, i + 1,
						record);
					break;
				case 1:
					sidebarLevelItem2.gameObject.SetActive(true);
					sidebarLevelItem2.UpdateDisplay(
						level.Mysterious && i >= currentLevelIndex ? MysteriousLevelCover : levelMeta.Song, i + 1,
						record);
					break;
				case 2:
					sidebarLevelItem3.gameObject.SetActive(true);
					sidebarLevelItem3.UpdateDisplay(
						level.Mysterious && i >= currentLevelIndex ? MysteriousLevelCover : levelMeta.Song, i + 1,
						record);
					break;
				case 3:
					sidebarLevelItem4.gameObject.SetActive(true);
					sidebarLevelItem4.UpdateDisplay(
						level.Mysterious && i >= currentLevelIndex ? MysteriousLevelCover : levelMeta.Song, i + 1,
						record);
					break;
				case 4:
					sidebarLevelEtcItemBottom.gameObject.SetActive(true);
					sidebarLevelEtcItemBottom.UpdateEtc(levelsCount - i);
					break;
			}
		}

		private void BeginCountdown() {
			if (GameplayPatches.CourseState.SelectedCourse is not { } course)
				return;

			if (course.Settings.CountdownSeconds is { } countdown) {
				_countdownValue = countdown;
				countdownNumber.text = countdown.ToString();
				_performCountdown = true;
			} else {
				_countdownValue = double.PositiveInfinity;
				countdownNumber.text = "∞";
			}

			LoadLevel(AllowCountdownSkip);
		}

		private void UpdateCountdown() {
			if (!_performCountdown)
				return;

			if (_countdownValue > 0 && double.IsFinite(_countdownValue)) {
				_countdownValue -= Time.unscaledDeltaTime;
				countdownNumber.text = Math.Floor(_countdownValue).ToString(CultureInfo.InvariantCulture);
			}

			if (_countdownValue <= 1) {
				_countdownValue = 0;
				_performCountdown = false;

				ContentDisplayTime = float.Epsilon;
			}
		}

		private void AllowCountdownSkip() {
			_allowCountdownSkip = true;

			countdownSkip.color = Color.white.SetAlpha(0);
			countdownSkip.DOColor(Color.white.SetAlpha(.6f), 0.4f).SetUpdate(true);
		}

		private void SkipCountdown() {
			LogTools.Log("Transition - SkipCountdown()");

			var countdown = Math.Min(_countdownValue, 99);
			if (countdown < 1.5)
				return;

			_performCountdown = false;
			_allowCountdownSkip = false;

			DOTween.To(
					() => countdown,
					x => { countdownNumber.text = Math.Floor(countdown = x).ToString(CultureInfo.InvariantCulture); },
					0,
					0.3f)
				.SetUpdate(true)
				.SetEase(Ease.OutCirc)
				.OnComplete(() => CloseAllAndProceed())
				.OnKill(() => CloseAllAndProceed());
		}

		private void UpdateContentScreen() {
			if (ContentDisplayTime <= 0)
				return;

			ContentDisplayTime -= Time.unscaledDeltaTime;

			if (ContentDisplayTime <= 0) {
				LogTools.Log("Content Display Time ran out!");
				CloseAllAndProceed();
				ContentDisplayTime = float.NegativeInfinity;
				_displayingStartScreen = false;
			}
		}

		private void CloseAllAndProceed([CanBeNull] Action customAction = null, bool fadeImage = false) {
			LogTools.Log("Transition - CloseAllAndProceed()");

			_contentTween?.Kill();
			_contentTween = DOTween.To(GetContentAlpha, SetContentAlpha, 0, .2f)
				.SetUpdate(true)
				.OnComplete(DisableAllContent);

			_displayingEndScreen = false;
			return;

			void DisableAllContent() {
				LogTools.Log("Transition - CloseAllAndProceed() - DisableAllContent()");
				courseCountdownUI.gameObject.SetActive(false);
				courseSidebarUI.gameObject.SetActive(EnableSidebarMenuOnGameScene);
				courseEndUI.gameObject.SetActive(false);
				courseStartUI.gameObject.SetActive(false);

				HideBackground(customAction ?? ProceedToLevel, fadeImage);

				if (!EnableSidebarMenuOnGameScene)
					return;

				if (_leavingScene) {
					_subContentTween?.Kill(true);
					_subContentTween = courseSidebarUI.DOFade(0, .4f)
						.SetUpdate(true)
						.OnComplete(() => courseSidebarUI.gameObject.SetActive(false));
				} else {
					// ReSharper disable once CompareOfFloatsByEqualityOperator
					if (courseSidebarUI.alpha != 1) {
						if (!courseSidebarUI.gameObject.activeSelf) {
							courseSidebarUI.alpha = 0;
							courseSidebarUI.gameObject.SetActive(true);
						}

						_subContentTween?.Kill(true);
						_subContentTween = courseSidebarUI.DOFade(1, .4f)
							.SetUpdate(true);
					}
				}
			}

			float GetContentAlpha() {
				var sidebarAlpha = courseSidebarUI.alpha;

				if (EnableSidebarMenuOnGameScene)
					sidebarAlpha = 0;

				return Mathf.Max(courseCountdownUI.alpha, sidebarAlpha, courseStartUI.alpha, courseEndUI.alpha);
			}

			void SetContentAlpha(float alpha) {
				courseStartUI.alpha = courseEndUI.alpha = courseCountdownUI.alpha = alpha;

				if (!EnableSidebarMenuOnGameScene)
					courseSidebarUI.alpha = alpha;
			}
		}

		// TODO better transitions
		private void ShowBackground(Action callback) {
			var cams = scrCamera.instance;
			cams?.PausePlanetsCam.gameObject.SetActive(true);

			_backgroundTween?.Kill();

			background.color = background.color.SetAlpha(0);
			_backgroundTween = background.DOColor(background.color.SetAlpha(1), .2f)
				.OnComplete(() => callback?.Invoke());
		}

		private void HideBackground(Action callback, bool fadeImage = false) {
			_backgroundTween?.Kill();

			background.color = background.color.SetAlpha(1);
			_backgroundTween = background.DOColor(fadeImage ? Color.black : background.color.SetAlpha(0), .2f)
				.SetUpdate(true)
				.OnComplete(() => {
					var cams = scrCamera.instance;
					cams?.PausePlanetsCam.gameObject.SetActive(false);

					callback?.Invoke();
				});
		}

		private void QuitToLastScene() {
			_leavingScene = true;
			CloseAllAndProceed(() => {
				GameplayPatches.CourseState.TerminateCourse();
				SceneTools.LoadSceneAnimated(
					() => SceneManager.LoadScene(CourseEnteredSceneName ?? GCNS.sceneLevelSelect),
					null
				);
			}, true);
		}

		private void RetryCourse() {
			CloseAllAndProceed(() => {
				GameplayPatches.CourseState.ResetProgress();
				ResetConstraintChips();
				LoadLevel();
				ProceedToLevel();
			});
		}

		// ---

		public void ProceedToLevel() {
			if (GameplayPatches.CourseState.SelectedCourse is not { } course)
				return;

			LogTools.Log("Transition - ProceedToLevel()");
			KillAllTweens();

			var levelIndex = GameplayPatches.CourseState.LevelIndex;
			if (levelIndex >= course.Levels.Count) {
				LogTools.Log("End of course");
				ShowEndScreen();
				return;
			}

			LogTools.Log($"Proceed to level index {levelIndex}");

			var level = course.Levels[levelIndex];

			if (level.CutsceneFile != null)
				CourseCutsceneScene.BeginCutscene(level.CutsceneFile, StartLevel);

			else StartLevel();

			return;

			void StartLevel() {
				GameplayPatches.CourseState.PauseRequested = false;

				LogTools.Log(
					$"Transition - Setting PlayStartedLevelIndex {GameplayPatches.CourseState.PlayStartedLevelIndex} -> {GameplayPatches.CourseState.LevelIndex}");
				GameplayPatches.CourseState.PlayStartedLevelIndex = GameplayPatches.CourseState.LevelIndex;
			}
		}


		public void ShowEndScreen() {
			LogTools.Log("Show end screen");
			ShowBackground(ShowContent);
			UpdateCourseEndUI();

			return;

			void ShowContent() {
				courseEndUI.alpha = 0;
				courseEndUI.gameObject.SetActive(true);

				courseCountdownUI.gameObject.SetActive(false);
				courseStartUI.gameObject.SetActive(false);
				courseSidebarUI.gameObject.SetActive(false);

				_contentTween?.Kill();
				_contentTween = courseEndUI.DOFade(1, .2f)
					.SetUpdate(true)
					.OnComplete(() => _displayingEndScreen = true);

				ContentDisplayTime = float.PositiveInfinity;
			}
		}

		public void ShowCountdown() {
			LogTools.Log("Show countdown");

			countdownNumber.text = "···";
			countdownSkip.color = Color.white.SetAlpha(0);

			KillAllTweens();
			ShowBackground(ShowContent);
			UpdateCourseSidebarUI();

			GameplayPatches.CourseState.PauseRequested = true;

			return;

			void ShowContent() {
				courseCountdownUI.alpha = 0;
				courseCountdownUI.gameObject.SetActive(true);

				if (!EnableSidebarMenuOnGameScene) {
					courseSidebarUI.alpha = 0;
					courseSidebarUI.gameObject.SetActive(true);
				}

				courseEndUI.gameObject.SetActive(false);
				courseStartUI.gameObject.SetActive(false);

				_contentTween?.Kill();
				_contentTween = DOTween.To(() => courseCountdownUI.alpha,
						x => courseSidebarUI.alpha = courseCountdownUI.alpha = x, 1, .2f)
					.SetUpdate(true)
					.OnComplete(BeginCountdown);

				ContentDisplayTime = float.PositiveInfinity;
			}
		}

		private void ShowStartScreen() {
			LogTools.Log("Show start screen");

			KillAllTweens();
			ShowBackground(ShowContent);

			GameplayPatches.CourseState.PauseRequested = true;

			return;

			void ShowContent() {
				courseStartUI.alpha = 0;
				courseStartUI.gameObject.SetActive(true);

				courseCountdownUI.gameObject.SetActive(false);
				courseEndUI.gameObject.SetActive(false);
				courseSidebarUI.gameObject.SetActive(false);

				_contentTween?.Kill();
				_contentTween = courseStartUI.DOFade(1, .2f)
					.SetUpdate(true)
					.OnComplete(() => {
						LoadLevel();
						_displayingStartScreen = true;
					});

				ContentDisplayTime = 7;
				_displayingEndScreen = false;
			}
		}

		// ---

		public static void BeginCourse() {
			if (GameplayPatches.CourseState.SelectedCourse is not { } course)
				throw new AssertionException("A course object must be selected in order to start it");

			Assert.True(course.Levels.Select(l => l.AbsolutePath).All(File.Exists),
				"One or more levels have missing files");
			Assert.False(course.Levels.Count == 0, "A course must have at least one level");

			LogTools.Log($"course name: {course.Name}");
			LogTools.Log($"course desc: {course.Description}");
			LogTools.Log($"course creator: {course.Creator}");
			LogTools.Log($"course levels: {course.Levels.Count}");

			SceneManager.LoadScene(SCENE_NAME, LoadSceneMode.Single);
			SceneManager.LoadScene(GCNS.sceneGame, LoadSceneMode.Additive);

			GameplayPatches.CourseState.PlayingCourse = true;
			GameplayPatches.CourseState.ResetProgress();

			LogTools.Log("Loaded all scenes");

#if DEBUG
			var courseChecksum = ChecksumTools.ComputeCourseChecksum(course).Hash;
			var levelsChecksum = string.Join(" ", course.Levels.Select(l => l.LevelMeta.Checksum));
			var gameplayChecksum = string.Join(" ", course.Levels.Select(l => l.LevelMeta.GameplayChecksum));

			LogTools.Log(
				$"CHECKSUM INFO\nCourse Checksum: {courseChecksum}\nLevels: {levelsChecksum}\nGameplay: {gameplayChecksum}");
#endif
		}

		private static void KillAllTweens() {
			try {
				DOTween.KillAll();
			} catch {
				// ignored
			}
		}

		private static void LoadLevel() => LoadLevel(null);

		private static void LoadLevel([CanBeNull] Action callback) {
			if (GameplayPatches.CourseState.SelectedCourse is not { } course)
				return;

			var levelIndex = GameplayPatches.CourseState.LevelIndex;
			LogTools.Log($"Start loading level at index {levelIndex}");

			if (course.Levels.Count <= levelIndex)
				return;

			var level = course.Levels[levelIndex];
			GameplayPatches.SetupGameSceneParameters.LoadLevel(level.AbsolutePath, callback);
		}
	}
}