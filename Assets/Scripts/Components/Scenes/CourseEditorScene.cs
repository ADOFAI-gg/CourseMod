using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ADOFAI;
using CourseMod.Components.Atoms;
using CourseMod.Components.Atoms.Backdrop;
using CourseMod.Components.Atoms.Button;
using CourseMod.Components.Atoms.InputField;
using CourseMod.Components.Molecules.ConstraintInputField;
using CourseMod.Components.Molecules.ContextMenu;
using CourseMod.Components.Molecules.EditorLevelItem;
using CourseMod.Components.Molecules.EditorMissingLevelSection;
using CourseMod.Components.Molecules.Popup;
using CourseMod.Components.Organisms.EditorLevelList;
using CourseMod.DataModel;
using CourseMod.Patches;
using CourseMod.Utils;
using DG.Tweening;
using GDMiniJSON;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityModManagerNet;

namespace CourseMod.Components.Scenes {
	public class CourseEditorScene : BackdropScene {
		public const string SCENE_NAME = "CourseEditor";

		public enum EditorPopupType {
			UnsavedQuit,
			UnsavedContinue,
			ClearChallenge,
			CopyrightNotice,
			MissingLevelNotice
		}

		[Header("Thumbnail")] public Image thumbnail;
		public GameObject thumbnailInfoObject;
		public TextMeshProUGUI thumbnailFileName;
		public TextMeshProUGUI thumbnailSize;
		public TextMeshProUGUI thumbnailResolution;
		public ButtonStyle changeThumbnailButton;
		public ButtonStyle deleteThumbnailButton;

		[Header("Backdrop")] public InputFieldStyle courseNameField;
		public InputFieldStyle courseCreatorField;
		public InputFieldStyle courseDescriptionField;

		public TextMeshProUGUI courseFilename;
		public GameObject dirtyCheck;
		public Button courseFileActionsButton;
		public FileActionsContextMenu fileActionsContextMenu;

		public InputFieldStyle countdownSecondsField;

		public Button clearChallengeInfoButton;

		public InputFieldIconHighlighter accuracyConstraintField;
		public InputFieldIconHighlighter deathConstraintField;
		public InputFieldIconHighlighter lifeConstraintField;

		public EditorLevelList levelList;

		[Header("Buttons")] public ButtonStyle addLevelButton;
		public ButtonStyle selectionTestPlayButton;
		public ButtonStyle testPlayButton;

		[Header("Popups")] public Popup copyrightNoticePopup;
		public Popup unsavedQuitPopup;
		public Popup unsavedContinuePopup;
		public Popup clearChallengePopup;
		public Popup missingLevelNoticePopup;

		public TextI18N missingLevelMessage;
		public List<MissingLevel> missingLevels = new();
		public GameObject missingLevelEtcObject;
		public TextI18N missingLevelEtcText;

		[NonSerialized] public string LastOpenedCoursePath;

		public static Course? CurrentCourse;

		private bool _isDirty;
		private string _lastThumbnailPath;

		private bool LastOpenedCourseDoesntExist =>
			string.IsNullOrEmpty(LastOpenedCoursePath) || !File.Exists(LastOpenedCoursePath);

		private IReadOnlyDictionary<EditorPopupType, Popup> _popups;


		public PlayerSettings PlayerSettings;

		private void Awake() {
			PlayerSettings = ModDataStorage.PlayerSettings;
			AddListeners();
			SetDirty(false);
			InitObjects();
			InitClearTests();
		}

		private void InitObjects() {
			InitPopup();
			InitContextMenus();
			InitTexts();
			InitButtonInteractableState();
			InitFieldInteractableState();
		}

		private void InitClearTests() {
			InitClearTestLevels();
			InitCloseTestContextMenus();
			InitClearTestBackdrops();
		}

		private void AddListeners() {
			changeThumbnailButton.button.onClick.AddListener(OnChangeThumbnailClicked);
			deleteThumbnailButton.button.onClick.AddListener(RemoveThumbnail);
			addLevelButton.button.onClick.AddListener(AddAndChangeSelectLevel);
			selectionTestPlayButton.button.onClick.AddListener(() =>
				RunWithUnsavedCheck(EditorPopupType.UnsavedContinue, SelectionTestPlay));
			testPlayButton.button.onClick.AddListener(() =>
				RunWithUnsavedCheck(EditorPopupType.UnsavedContinue, TestPlay));
			clearChallengeInfoButton.onClick.AddListener(() => OpenPopup(EditorPopupType.ClearChallenge));
			courseFileActionsButton.onClick.AddListener(OpenFileActionsContextMenu);
			levelList.onItemCountChanged.AddListener(OnLevelListItemCountChange);
			levelList.onItemSelectionChanged.AddListener(OnLevelListSelectionChange);

			courseNameField.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
			courseCreatorField.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
			courseDescriptionField.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
			countdownSecondsField.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
			accuracyConstraintField.inputFieldStyle.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
			deathConstraintField.inputFieldStyle.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
			lifeConstraintField.inputFieldStyle.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
			deathConstraintField.inputFieldStyle.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
			lifeConstraintField.inputFieldStyle.inputField.onEndEdit.AddListener(_ => TryApplyChanges());
		}

		private void InitPopup() {
			_popups = new Dictionary<EditorPopupType, Popup> {
				[EditorPopupType.CopyrightNotice] = copyrightNoticePopup,
				[EditorPopupType.UnsavedQuit] = unsavedQuitPopup,
				[EditorPopupType.UnsavedContinue] = unsavedContinuePopup,
				[EditorPopupType.ClearChallenge] = clearChallengePopup,
				[EditorPopupType.MissingLevelNotice] = missingLevelNoticePopup
			}.ToImmutableDictionary();
			_popups.ForEach(e => e.Value.Init(this));
		}

		private void InitContextMenus() {
			levelList.itemContextMenu.Init(this, new BackdropSettings(
				transitionDuration: 0,
				opacity: 0,
				allowBackdropClickToEnd: true,
				useKeyboardControl: true
			));


			fileActionsContextMenu.Init(this, new BackdropSettings(
				transitionDuration: 0,
				opacity: 0,
				allowBackdropClickToEnd: true,
				useKeyboardControl: true
			));
		}

		private void InitTexts() {
			courseNameField.inputField.text =
				courseCreatorField.inputField.text =
					courseDescriptionField.inputField.text =
						countdownSecondsField.inputField.text =
							accuracyConstraintField.inputFieldStyle.inputField.text =
								deathConstraintField.inputFieldStyle.inputField.text =
									lifeConstraintField.inputFieldStyle.inputField.text = string.Empty;
			courseFilename.text = I18N.Get("editor-no-file-opened");
		}

		private void InitFieldInteractableState() {
			courseNameField.Disabled =
				courseCreatorField.Disabled =
					courseDescriptionField.Disabled =
						countdownSecondsField.Disabled =
							accuracyConstraintField.inputFieldStyle.Disabled =
								deathConstraintField.inputFieldStyle.Disabled =
									lifeConstraintField.inputFieldStyle.Disabled = true;
		}

		private void InitButtonInteractableState() {
			addLevelButton.Disabled =
				changeThumbnailButton.Disabled =
					deleteThumbnailButton.Disabled =
						testPlayButton.Disabled =
							selectionTestPlayButton.Disabled = true;
		}

		private void InitClearTestLevels() {
			levelList.Clear();
		}

		private void InitCloseTestContextMenus() {
			levelList.itemContextMenu.gameObject.SetActive(false);
			fileActionsContextMenu.gameObject.SetActive(false);
		}

		private void InitClearTestBackdrops() {
			foreach (Transform child in backdropContainer.transform) {
				if (!child) continue;
				Backdrop backdrop = child.GetComponent<Backdrop>();
				if (backdrop) {
					backdrop.End();
				} else {
					Destroy(child.gameObject);
				}
			}
		}

		private void Start() {
			LastOpenedCoursePath = PlayerSettings.LastOpenedEditorFilePath;
			var lastCopyrightNotice = PlayerSettings.LastCopyrightNoticeCloseTime;
			var now = DateTime.Now;

			if ((now - lastCopyrightNotice).TotalDays >= 28 &&
			    (now.Month != lastCopyrightNotice.Month || now.Day >= lastCopyrightNotice.Day)) {
				Popup popup = OpenPopup(EditorPopupType.CopyrightNotice);
				popup.OnceAction.AddListener(action => {
					if (action == Popup.PopupActionType.Confirm)
						PlayerSettings.LastCopyrightNoticeCloseTime = DateTime.Now;
				});
			}

			if (CurrentCourse != null) {
				ApplyCourseToUI();
				RefreshUIActivity();
			}
		}

		private void Update() {
			CheckKeyShortCuts();
		}

		private void CheckKeyShortCuts() {
			// filter context menus and active ui elements
			if (ActiveBackdropExists)
				return;

			var selectedGo = EventSystem.current.currentSelectedGameObject;
			if (selectedGo) {
				if (selectedGo.GetComponent<Button>())
					goto after_filter;

				return;
			}

			after_filter:
			if (Input.GetKeyDown(KeyCode.Escape)) {
				var selection = levelList.SelectedLevels;
				var selectionExists = selection.Length > 0;

				if (selectionExists) {
					levelList.DeselectAllLevels();
				} else {
					OpenFileActionsContextMenu();
				}

				return;
			}

			var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
			if (ctrl) {
				CheckControlKeyShortCuts();
				return;
			}
		}

		private void CheckControlKeyShortCuts() {
			if (Input.GetKeyDown(KeyCode.A)) {
				if (CurrentCourse == null) return;
				levelList.SelectAllLevels();
				return;
			}

			if (Input.GetKeyDown(KeyCode.S)) {
				if (CurrentCourse == null) return;
				bool shift = Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.RightShift);
				Save(shift);
				return;
			}

			if (Input.GetKeyDown(KeyCode.D)) {
				if (CurrentCourse == null) return;
				levelList.DuplicateSelection();
				return;
			}
		}

		// private void UpdateCustomComponentFocusState(bool isFocused)
		// {
		//     _customComponentFocused = isFocused;
		//     // || PopupsList.Any(p => p.gameObject.activeInHierarchy)
		//     // || ContextMenus.Any(m => m.gameObject.activeInHierarchy);
		// }

		private void OpenFileActionsContextMenu() {
			fileActionsContextMenu.Open(null);
		}

		private void OnLevelListItemCountChange(int count) {
			bool disabled = count == 0;
			testPlayButton.Disabled = disabled;
		}

		private void OnLevelListSelectionChange(EditorLevelItem[] items) {
			selectionTestPlayButton.Disabled = items.Length == 0;
		}

		private void AddAndChangeSelectLevel() {
			string levelPath = FileDialogTools.OpenLevelFileDialog();
			if (!File.Exists(levelPath))
				return;

			FileTools.CopyLevelToCourseDir(LastOpenedCoursePath, levelPath);

			levelList.GetItems().ForEach(e => e.Selected = false);
			EditorLevelItem item = levelList.AddLevel(CourseLevel.FromPath(levelPath, CurrentCourse!.Value.FilePath));
			item.Selected = true;
			SetDirty(true);
		}

		public void SetDirty(bool isDirty) {
			dirtyCheck.SetActive(_isDirty = isDirty);
		}

		public void RunWithUnsavedCheck(EditorPopupType editorPopupType, Action callback) {
			if (_isDirty) {
				var popup = OpenPopup(editorPopupType);
				popup.OnceAction.AddListener(action => {
					switch (action) {
						case Popup.PopupActionType.Save:
							Save(CurrentCourse == null); // also prevents recursive loop
							SetDirty(false);
							callback();
							return;

						case Popup.PopupActionType.Discard:
							ApplyCourseToUI();
							SetDirty(false); // also prevents recursive loop
							callback();
							return;
						default: //otherwise
							return;
					}
				});
			} else callback();
		}

		public void Open(bool withDialog = false) {
			string path;

			if (LastOpenedCourseDoesntExist || withDialog) {
				path = FileDialogTools.OpenCourseFileDialog(Path.GetDirectoryName(LastOpenedCoursePath));
			} else {
				path = LastOpenedCoursePath;
			}

			if (path.IsNullOrEmpty()) //no file selected
				return;

			LastOpenedCoursePath = path!;
			PlayerSettings.LastOpenedEditorFilePath = path;
			levelList.Clear();

			Course course = JsonConvert.DeserializeObject<Course>(File.ReadAllText(LastOpenedCoursePath));
			InitCurrentDirCourse(ref course);

			CurrentCourse = course;
			ApplyCourseToUI();
			RefreshUIActivity();
		}

		private void InitCurrentDirCourse(ref Course course) {
			string coursePath = LastOpenedCoursePath;
			string courseDir = Path.GetDirectoryName(coursePath);
			Assert.True(courseDir != null, "The course directory doesn't exist");

			course.FilePath ??= coursePath;
			for (int i = 0; i < course.Levels.Count; i++) {
				CourseLevel courseLevel = course.Levels[i];
				courseLevel.AbsolutePath = Path.Combine(courseDir!, courseLevel.Path);
				course.Levels[i] = courseLevel;
			}
		}

		private void RefreshUIActivity() {
			courseNameField.Disabled =
				courseCreatorField.Disabled =
					courseDescriptionField.Disabled =
						countdownSecondsField.Disabled =
							accuracyConstraintField.inputFieldStyle.Disabled =
								deathConstraintField.inputFieldStyle.Disabled =
									lifeConstraintField.inputFieldStyle.Disabled = false;

			addLevelButton.Disabled =
				changeThumbnailButton.Disabled = CurrentCourse == null;

			selectionTestPlayButton.Disabled = levelList.SelectedLevels.Length == 0;
			testPlayButton.Disabled = levelList.ToLevels().Count == 0;
		}

		public Course GetCourseWithAssertion() {
			Assert.True(CurrentCourse != null, "Current course is null; create course first");
			return CurrentCourse!.Value;
		}

		public void ApplyCourseToUI() {
			Course course = GetCourseWithAssertion();
			CourseSettings settings = course.Settings;

			if (settings.ThumbnailFile == null || !TryChangeThumbnail(settings.ThumbnailFile)) {
				RemoveThumbnail();
			}

			courseNameField.inputField.SetTextWithoutNotify(course.Name);
			courseCreatorField.inputField.SetTextWithoutNotify(course.Creator);
			courseDescriptionField.inputField.SetTextWithoutNotify(course.Description);

			courseFilename.text = Path.GetFileName(LastOpenedCoursePath);

			countdownSecondsField.inputField.SetTextWithoutNotify(settings.CountdownSeconds?.ToString() ??
			                                                      string.Empty);
			accuracyConstraintField.inputFieldStyle.inputField.SetTextWithoutNotify(
				(settings.AccuracyConstraint * 100)?.ToString() ?? string.Empty);
			deathConstraintField.inputFieldStyle.inputField.SetTextWithoutNotify(settings.DeathConstraint?.ToString() ??
				string.Empty);
			lifeConstraintField.inputFieldStyle.inputField.SetTextWithoutNotify(settings.LifeConstraint?.ToString() ??
				string.Empty);

			levelList.AddLevels(course.Levels);
			var missingLevelItems = levelList.GetNotFoundLevelItems();
			if (missingLevelItems.Length != 0) {
				missingLevelMessage.UpdateArguments(new Dictionary<string, object> {
					["levelCount"] = missingLevelItems.Length
				});
				for (int i = 0; i < missingLevels.Count; i++) {
					MissingLevel missingLevel = missingLevels[i];
					if (i < missingLevelItems.Length) {
						EditorLevelItem item = missingLevelItems[i];
						missingLevel.UpdateDisplay(item.OrderNumber, item.TitleCache);
						missingLevel.gameObject.SetActive(true);
					} else {
						missingLevel.gameObject.SetActive(false);
					}
				}

				int etcCount = missingLevelItems.Length - missingLevels.Count;
				if (etcCount > 0) {
					missingLevelEtcObject.SetActive(true);
					missingLevelEtcText.UpdateArguments(new Dictionary<string, object> { ["count"] = etcCount });
				} else {
					missingLevelEtcObject.SetActive(false);
				}

				Popup popup = OpenPopup(EditorPopupType.MissingLevelNotice);
				popup.OnAction.AddListener(type => {
					if (type == Popup.PopupActionType.Discard) {
						levelList.RemoveNotFoundLevelItems();
					}
				});
			}

			SetDirty(false);
		}


		public void Save(bool withDialog = false) {
			Assert.True(CurrentCourse != null || withDialog,
				"CurrentCourse is null but Save() has invoked. it should not be happened. if the course file is null, it is disabled. use 'New Course' or 'Save Course As' instead.");


			if (LastOpenedCourseDoesntExist || withDialog) {
				string result = FileDialogTools.SaveCourseFileDialog(Path.GetDirectoryName(LastOpenedCoursePath));

				if (result.IsNullOrEmpty()) return; //no file selected

				PlayerSettings.LastOpenedEditorFilePath = result;
				LastOpenedCoursePath = result;
				courseFilename.text = Path.GetFileName(LastOpenedCoursePath);
			}


			var course = CurrentCourse ??= Course.Default;

			ApplyUIToCourse();

#if DEBUG
			course.GenerateReadonlyLevelsInfo();
#endif

			course.Settings.ThumbnailFile =
				ReplaceSlash(course.Settings.ThumbnailFile);

			for (var i = 0; i < course.Levels.Count; i++) {
				var level = course.Levels[i];
				level.Path = ReplaceSlash(level.Path);

				course.Levels[i] = level;
			}

			File.WriteAllText(LastOpenedCoursePath!, JsonConvert.SerializeObject(CurrentCourse, Formatting.Indented));

			SetDirty(false);

			return;

			string ReplaceSlash(string path) => path.Replace('\\', '/');
		}

		public void Export() {
			RunWithUnsavedCheck(EditorPopupType.UnsavedContinue, () => {
				var course = GetCourseWithAssertion();
				var courseDirectory = Path.GetDirectoryName(course.FilePath)!;

				var levelFiles = course.Levels
					.Select(level => ExportTools.GetLevelFiles(level.AbsolutePath));

				var files = new List<string>() {
					course.FilePath
				};

				if (!string.IsNullOrEmpty(_lastThumbnailPath) && File.Exists(_lastThumbnailPath))
					files.Add(_lastThumbnailPath);
				
				files.AddRange(levelFiles.SelectMany(f => f).Distinct());

				var filename = $"{string.Join("_", course.Name.Split(Path.GetInvalidFileNameChars()))}";

				using var stream = new FileStream(Path.Combine(courseDirectory, filename + ".zip"), FileMode.Create);
				using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

				foreach (var file in files) {
					archive.CreateEntryFromFile(file, GetTargetFileName(courseDirectory, filename, file));
				}
			});

			return;

			string GetTargetFileName(string relativeRoot, string parentDirectory, string file) {
				var targetFileName = Path.GetFileName(file);

				if (targetFileName.EndsWith(".course", StringComparison.OrdinalIgnoreCase))
					file = Path.Combine(Path.GetDirectoryName(file)!, "main.course");

				return Path.Combine(parentDirectory, Path.GetRelativePath(relativeRoot, file));
			}
		}

		private void TryApplyChanges() {
			if (CurrentCourse == null) return;

			ApplyChanges();
		}

		private void ApplyChanges() {
			SetDirty(true);
			ApplyUIToCourse();
		}

		private void ApplyUIToCourse() {
			Course course = GetCourseWithAssertion();
			CourseSettings settings = course.Settings;

			settings.ThumbnailFile =
				string.IsNullOrEmpty(_lastThumbnailPath) || !File.Exists(_lastThumbnailPath)
					? null
					: Path.GetRelativePath(Path.GetDirectoryName(course.FilePath)!, _lastThumbnailPath);

			course.Name = courseNameField.inputField.text;
			course.Creator = courseCreatorField.inputField.text;
			course.Description = courseDescriptionField.inputField.text;

			settings.CountdownSeconds = StringTools.GetNullOrParsedInt(countdownSecondsField.inputField.text);
			settings.AccuracyConstraint =
				StringTools.GetNullOrParsedDouble(accuracyConstraintField.inputFieldStyle.inputField.text) / 100;
			settings.DeathConstraint =
				StringTools.GetNullOrParsedInt(deathConstraintField.inputFieldStyle.inputField.text);
			settings.LifeConstraint =
				StringTools.GetNullOrParsedInt(lifeConstraintField.inputFieldStyle.inputField.text);

			course.Settings = settings;
			course.Levels = levelList.ToLevels();
			CurrentCourse = course;
		}

		public void Quit() {
			CurrentCourse = null;
			SceneManager.LoadScene("CourseSelect");
		}


		private Popup OpenPopup(EditorPopupType editorPopupType) {
			Assert.True(_popups.TryGetValue(editorPopupType, out var popup), $"No such popup {editorPopupType}");
			popup!.Open();
			return popup;
		}

		private void OnChangeThumbnailClicked() {
			var path = FileDialogTools.OpenThumbnailFileDialog(_lastThumbnailPath);

			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return;

			var course = GetCourseWithAssertion();
			var copyPath = Path.Combine(Path.GetDirectoryName(course.FilePath)!, Path.GetFileName(path));

			if (path != copyPath)
				File.Copy(path, copyPath, true);

			if (!TryChangeThumbnail(copyPath)) RemoveThumbnail();
		}

		private bool TryChangeThumbnail([System.Diagnostics.CodeAnalysis.NotNull] string path) {
			path = Path.Combine(Path.GetDirectoryName(LastOpenedCoursePath)!, path);

			var thumbnailSprite = ImageTools.OpenSprite(path);
			bool spriteExists = thumbnailSprite;
			if (!spriteExists) return false;

			thumbnail.sprite = thumbnailSprite;
			thumbnail.color = Color.white;
			thumbnailInfoObject.SetActive(true);
			thumbnailFileName.text = Path.GetFileName(path);
			thumbnailSize.text = GetThumbnailSize(new FileInfo(path).Length);
			thumbnailResolution.text = $"{thumbnailSprite.texture.width}×{thumbnailSprite.texture.height}";

			_lastThumbnailPath = path;
			deleteThumbnailButton.Disabled = false;
			return true;
		}

		private string GetThumbnailSize(long byteSize) {
			if (byteSize < 1024)
				return byteSize + " B";
			if (byteSize < 1024 * 1024)
				return (byteSize / 1024.0).ToString("F2") + " KB";
			if (byteSize < 1024 * 1024 * 1024)
				return (byteSize / (1024.0 * 1024.0)).ToString("F2") + " MB";
			return (byteSize / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " GB";
		}

		private void RemoveThumbnail() {
			thumbnail.sprite = null;
			thumbnail.color = Color.white.WithAlpha(0.2f);
			thumbnailInfoObject.SetActive(false);
			_lastThumbnailPath = null;
			deleteThumbnailButton.Disabled = true;
		}

		private void TestPlay() {
			List<CourseLevel> levelPaths = levelList.ToLevels();
			Assert.True(levelPaths.Count > 0, "Level doesn't exist");

			Play(levelPaths);
		}

		private void SelectionTestPlay() {
			EditorLevelItem[] selectedLevels = levelList.SelectedLevels;
			Assert.True(selectedLevels.Length > 0, "Selection doesn't exist");

			Play(selectedLevels.Select(e => e.levelData).ToList());
		}

		private void Play(List<CourseLevel> courseLevels) {
			if (!courseLevels.All(e => File.Exists(e.AbsolutePath))) {
				throw new ArgumentException(
					$"Some files don't exist. Files: [{string.Join(", ", courseLevels.Select(e => e.AbsolutePath))}]",
					nameof(courseLevels));
			}

			Course course = GetCourseWithAssertion();
			course.Levels = courseLevels;
			GameplayPatches.CourseState.SelectedCourse = course;
			DOTween.KillAll();
#if DEBUG
			CourseTransitionScene.CourseEnteredSceneName = SCENE_NAME;
#endif
			CourseTransitionScene.BeginCourse();
		}
	}
}