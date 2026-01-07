using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CourseMod.Components.Atoms.Button;
using CourseMod.Components.Molecules.PackedPreviewList;
using CourseMod.Components.Molecules.Popup;
using CourseMod.Components.Molecules.SelectLevelFullCreditsItem;
using CourseMod.Components.Molecules.SelectLevelItem;
using CourseMod.Components.Molecules.SelectLevelResultItem;
using CourseMod.DataModel;
using CourseMod.Patches;
using CourseMod.Utils;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Nobi.UiRoundedCorners;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CourseMod.Components.Scenes {
	public class CourseSelectScene : BackdropScene {
		[UsedImplicitly] public const string SCENE_NAME = "CourseSelect";

		[Header("Prefabs")] public SelectLevelItem levelItemPrefab;
		public SelectLevelResultItem resultItemPrefab;
		public SelectLevelFullCreditsItem fullCreditsCourseLevelsItemPrefab;

		public RectTransform resultsContainer;
		public RectTransform coursesContainer;


		[Header("Metadata Category")] public ImageWithIndependentRoundedCorners creditsContainer;

		public TextMeshProUGUI courseTitle;
		public TextMeshProUGUI courseDescription;

		public PackedPreviewList musicCredits;
		public PackedPreviewList levelCredits;
		public TextMeshProUGUI courseCreator;
		public RectTransform courseCreatorContainer;

		public GameObject courseActionsContainer;
		public ButtonStyle fullCreditsButton;
		public ButtonStyle editCourseButton;
		public ButtonStyle deleteCourseButton;

		[Header("Clear Challenge Category")] public ImageWithIndependentRoundedCorners clearChallengeContainer;

		public Button clearChallengeButton;

		public GameObject accuracyConstraintContainer;
		public GameObject deathConstraintContainer;
		public GameObject lifeConstraintContainer;

		public TextMeshProUGUI accuracyConstraint;
		public TextMeshProUGUI deathConstraint;
		public TextMeshProUGUI lifeConstraint;

		[Header("Detailed Results Category")] public GameObject detailedResultsContainer;

		[Header("Total Result Category")] public ImageWithIndependentRoundedCorners totalResultContainer;
		public SelectLevelResultItem totalResultItem;

		[Header("Actions")] public ButtonStyle importCourseButton;
		public ButtonStyle courseEditorButton;

		public ButtonStyle toggleNoFailButton;
		public ButtonStyle settingsButton;
		public ButtonStyle playButton;

		[Header("Popups")] public Popup clearChallengePopup;
		public Popup fullCreditsPopup;

		[Header("Full Credits")] public TextMeshProUGUI fullCreditsCourseTitle;
		public TextMeshProUGUI fullCreditsCourseCreator;
		public RectTransform fullCreditsCourseLevelsContainer;
		public ButtonStyle fullCreditsToggleSpoilers;

		public SelectLevelItem ChosenItem { get; private set; }

		public readonly List<SelectLevelResultItem> ResultItems = new();

		private PlayerSettings _playerSettings;

		private readonly List<SelectLevelFullCreditsItem> _spoilerItems = new();
		private bool _showSpoilers;

		private void Awake() {
			_playerSettings = ModDataStorage.PlayerSettings;

			fullCreditsButton.button.onClick.AddListener(OpenAndReloadFullCreditsPopup);
			editCourseButton.button.onClick.AddListener(() => OpenCourseEditor(ChosenItem?.Course));
			deleteCourseButton.button.onClick.AddListener(DeleteCourse);

			clearChallengeButton.onClick.AddListener(clearChallengePopup.Open);
			importCourseButton.button.onClick.AddListener(ImportCourse);
			courseEditorButton.button.onClick.AddListener(() => OpenCourseEditor(null));

			toggleNoFailButton.button.onClick.AddListener(() => {
				_playerSettings.UseNoFail = !_playerSettings.UseNoFail;
				_playerSettings.Save();

				toggleNoFailButton.buttonText.text = I18N.Get(_playerSettings.UseNoFail
					? "general-no-fail-enabled"
					: "general-no-fail-disabled");
			});
			settingsButton.button.onClick.AddListener(OpenSettings);
			playButton.button.onClick.AddListener(() => {
				if (!ChosenItem) return;

				GameplayPatches.CourseState.SelectedCourse = ChosenItem.Course;

#if DEBUG
				CourseTransitionScene.CourseEnteredSceneName = SCENE_NAME;
#endif
				CourseTransitionScene.BeginCourse();
			});

			fullCreditsToggleSpoilers.button.onClick.AddListener(FullCreditsPopupSpoilersToggle);

			fullCreditsPopup.Init(this);
			clearChallengePopup.Init(this);

			return;

			void OpenCourseEditor(Course? course) {
				CourseEditorScene.CurrentCourse = course;
				SceneManager.LoadScene(CourseEditorScene.SCENE_NAME);
			}
		}

		private void Start() {
			UpdateCourseInfo();
		}

		private void Update() {
			if (ActiveBackdropExists) {
				return;
			}

			if (!clearChallengePopup.gameObject.activeSelf
			    && !fullCreditsPopup.gameObject.activeSelf) {
				if (Input.GetKeyDown(KeyCode.Escape)) {
					if (ChosenItem)
						DeselectItem();
					else
						LoadScene();
				}
			}

			return;

			void LoadScene() => SceneManager.LoadScene(GCNS.sceneLevelSelect);
		}

		private void UpdateLeftPanelBorder() {
			var panels = new[] { creditsContainer, clearChallengeContainer, totalResultContainer }
				.Where(p => p.gameObject.activeSelf).ToArray();

			for (var i = 0; i < panels.Length; i++) {
				var panel = panels[i];
				var next = panels.Length > i + 1 ? panels[i + 1] : null;

				var r = panel.r;

				if (!next) {
					panel.r = new(r.x, r.y, 12, 12);
					panel.Refresh();
					return;
				}

				if (r.w != 0) {
					panel.r = new(r.x, r.y, 0, 0);
					panel.Refresh();
				}
			}
		}

		public void SelectItem(SelectLevelItem selectLevelItem) {
			var previouslyChosenItem = ChosenItem;
			ChosenItem = selectLevelItem;

			selectLevelItem.UpdateAppearance();

			if (previouslyChosenItem)
				previouslyChosenItem.UpdateAppearance();

			UpdateCourseInfo();
		}

		public void DeselectItem() {
			if (ChosenItem) {
				var item = ChosenItem;
				ChosenItem = null;

				item.UpdateAppearance();
			}

			ResetCourseInfo();
		}

		private void UpdateLevelCreditInfo(out bool showFullCredits) {
			showFullCredits = false;

			if (!ChosenItem)
				return;

			var course = ChosenItem.Course;
			var levels = course.Levels
				.Where(l => _showSpoilers || !l.Mysterious)
				.Select(l => l.LevelMeta)
				.ToArray();


			var artists = levels.Select(level => level?.Artist).ToArray();
			musicCredits.gameObject.SetActive(true);
			musicCredits.UpdateDisplay(artists);

			showFullCredits |= musicCredits.maxVisibleTexts < artists.Length;


			var creators = levels.Select(level => level?.Creator).ToArray();
			levelCredits.gameObject.SetActive(true);
			levelCredits.UpdateDisplay(creators);

			showFullCredits |= levelCredits.maxVisibleTexts < levels.Length;
		}

		private void UpdateCourseInfo() {
			if (!ChosenItem) {
				ResetCourseInfo();
				return;
			}

			var course = ChosenItem.Course;
			courseTitle.text = course.Name;
			courseDescription.text = course.Description;
			courseDescription.gameObject.SetActive(!string.IsNullOrEmpty(course.Description));

			fullCreditsButton.Disabled = false;
			editCourseButton.Disabled = false;
			deleteCourseButton.Disabled = false;

			UpdateLevelCreditInfo(out var shouldEnableFullCredits);

			courseCreatorContainer.gameObject.SetActive(true);
			courseCreator.text = course.Creator;

			fullCreditsButton.Disabled = !shouldEnableFullCredits;

			var courseSettings = course.Settings;
			var usesClearChallenge = false;

			if (courseSettings.AccuracyConstraint is { } acc) {
				accuracyConstraint.text = $"{acc.ToAccuracyNotation(false)} {I18N.Get("general-accuracy-constraint")}";
				accuracyConstraintContainer.SetActive(usesClearChallenge = true);
			} else accuracyConstraintContainer.SetActive(false);

			if (courseSettings.LifeConstraint is { } life) {
				lifeConstraint.text = $"{life} {I18N.Get("general-life-constraint")}";
				lifeConstraintContainer.SetActive(usesClearChallenge = true);
			} else lifeConstraintContainer.SetActive(false);

			if (courseSettings.DeathConstraint is { } death) {
				deathConstraint.text = $"{death} {I18N.Get("general-death-constraint")}";
				deathConstraintContainer.SetActive(usesClearChallenge = true);
			} else deathConstraintContainer.SetActive(false);

			clearChallengeContainer.gameObject.SetActive(usesClearChallenge);

			for (var i = 0; i < resultsContainer.transform.childCount; i++)
				Destroy(resultsContainer.transform.GetChild(i).gameObject);

			ResultItems.Clear();
			UpdateCoursePlayRecord();
		}

		public void UpdateCoursePlayRecord() {
			var showResult = false;

			if (ChosenItem.Course.GetPlayRecord() is { } playRecord) {
				var levels = ChosenItem.Course.Levels;
				for (var i = 0; i < levels.Count; i++) {
					var record = playRecord.Records.Length > i ? playRecord.Records[i] : null;
					var resultItem = Instantiate(resultItemPrefab, resultsContainer);
					resultItem.UpdateDisplay(levels[i].LevelMeta?.Song, i + 1, record);
					ResultItems.Add(resultItem);
					LogTools.Log($"Added play record: {record?.LevelNumber} {record?.XAccuracy}");
				}

				if (ResultItems.Count > 0) {
					totalResultItem.UpdateDisplay(new CourseLevelPlayRecord {
						XAccuracy = playRecord.TotalAccuracy,
						HitMargins = playRecord.TotalHitMargins,
						TotalFloors = playRecord.TotalFloors
					});

					showResult = true;
				}
			}

			detailedResultsContainer.gameObject.SetActive(showResult);
			totalResultContainer.gameObject.SetActive(showResult);

			UpdateLeftPanelBorder();
		}

		private void ResetCourseInfo() {
			courseTitle.text = I18N.Get("select-select-course");
			courseDescription.text = I18N.Get("select-or-import-course");
			musicCredits.gameObject.SetActive(false);
			levelCredits.gameObject.SetActive(false);
			courseCreatorContainer.gameObject.SetActive(false);

			fullCreditsButton.Disabled = true;
			editCourseButton.Disabled = true;
			deleteCourseButton.Disabled = true;

			clearChallengeContainer.gameObject.SetActive(false);
			detailedResultsContainer.gameObject.SetActive(false);
			totalResultContainer.gameObject.SetActive(false);

			UpdateLeftPanelBorder();
		}

		private void OpenAndReloadFullCreditsPopup() {
			if (ChosenItem is not { } chosenItem)
				return;

			_spoilerItems.Clear();
			_showSpoilers = false;

			var course = chosenItem.Course;

			fullCreditsCourseTitle.text = course.Name;
			fullCreditsCourseCreator.text = course.Creator;

			for (var i = 0; i < course.Levels.Count; i++) {
				var level = course.Levels[i];
				var item = Instantiate(fullCreditsCourseLevelsItemPrefab, fullCreditsCourseLevelsContainer);

				if (item.mysterious)
					_spoilerItems.Add(item);

				item.UpdateInfo(i, level);
			}

			fullCreditsPopup.Open();
		}

		private void FullCreditsPopupSpoilersToggle() {
			_showSpoilers = !_showSpoilers;
			fullCreditsToggleSpoilers.buttonText.text =
				I18N.Get($"select-{(_showSpoilers ? "hide" : "show")}-spoilers");

			foreach (var item in _spoilerItems)
				item.UpdateSpoilerState(_showSpoilers);

			UpdateLevelCreditInfo(out _);
		}

		private void DeleteCourse() {
			var item = ChosenItem;
			if (!item) return;

			DeselectItem();

			var path = item.Course.FilePath;
			if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

			if (CourseCollection.CourseRecords.TryGetValue(path, out _))
				CourseCollection.UnregisterCourse(item.Course);

			File.Delete(path);
			Directory.Delete(Path.GetDirectoryName(path)!, true);

			Destroy(item.gameObject);
		}

		private void ImportCourse() {
			var path = FileDialogTools.OpenCourseFileDialog(_playerSettings.LastOpenedMainFilePath);
			if (path.IsNullOrEmpty() || !File.Exists(path)) return;

			_playerSettings.LastOpenedMainFilePath = path;
			_playerSettings.Save();

			string destinationDirectory;
			string selectedCourseName;

			if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
				destinationDirectory =
					CourseCollection.ParseDestinationDirectory(Path.GetFileNameWithoutExtension(path));

				Directory.CreateDirectory(destinationDirectory);

				var enc = Encoding.GetEncoding(949);
				ZipFile.ExtractToDirectory(path, destinationDirectory, enc);

				selectedCourseName = null;
			} else {
				var directoryName = Path.GetDirectoryName(path)!;
				destinationDirectory = CourseCollection.ParseDestinationDirectory(Path.GetFileName(directoryName));

				RDDirectory.Copy(directoryName, destinationDirectory, true);

				selectedCourseName = Path.GetFileName(path);
			}

			var courseFiles = CourseCollection.GetCoursePaths(destinationDirectory);

			for (var i = 0; i < courseFiles.Length; i++) {
				var currentCoursePath = courseFiles[i];
				var course = CourseCollection.ReadSingleCourse(currentCoursePath);
				var item = Instantiate(levelItemPrefab, coursesContainer);

				item.CourseSelect = this;
				item.AssignCourse(course);

				if (selectedCourseName == null && i == 0 || Path.GetFileName(currentCoursePath) == selectedCourseName)
					SelectItem(item);
			}
		}

		private void OpenSettings() {
			throw new NotImplementedException();
		}
	}
}