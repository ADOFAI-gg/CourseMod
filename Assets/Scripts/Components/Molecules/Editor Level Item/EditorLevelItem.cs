using System;
using System.Collections.Generic;
using System.IO;
using CourseMod.Components.Atoms.Checkbox;
using CourseMod.Components.Molecules.DragHandle;
using CourseMod.DataModel;
using CourseMod.Utils;
using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.EditorLevelItem {
	public class EditorLevelItem : MonoBehaviour {
		private enum ItemChipType {
			DisableAccuracyConstraint,
			DisableDeathConstraint,
			DisableLifeConstraint,
			Mysterious,
			Cutscene
		}

		public class SelectedStateChangeEvent : UnityEvent<bool> {
		}

		private const float TransitionDuration = .2f;

		private static readonly Color DefaultColor = Color.black.SetAlpha(.2f);
		private static readonly Color SelectedColor = ColorTools.Html("276BCF").SetAlpha(.2f);
		private static readonly Color DeletedColor = Color.red.SetAlpha(.2f);
		public static readonly Color SelectedDeletedColor = Color.red.SetAlpha(.45f);

		public Image background;
		public Button backgroundButton;

		public TextMeshProUGUI order;
		public TextMeshProUGUI title;
		public TextMeshProUGUI subTitle;
		public GameObject subTitleObject;

		public TextMeshProUGUI artistLabel;
		public TextMeshProUGUI creatorLabel;

		public TextMeshProUGUI artist;
		public TextMeshProUGUI creator;

		public RectTransform chipContainer;
		public List<Chip.Chip> chips;

		public Chip.Chip chipPrefab;

		public ControlVisibilityController handleVisibilityController;
		public Checkbox checkbox;
		public DragHandler dragHandler;

		[NonSerialized] public CourseLevel levelData;
		private FileSystemWatcher _levelFileWatcher;
		public string TitleCache { get; private set; }
		private bool _selectedLevelFileExists = true;
		public bool LevelFileExists { get; private set; } = true;
		public int OrderNumber { get; private set; }

		public bool Selected {
			get => checkbox.state == Checkbox.State.Checked;
			set {
				checkbox.SetState(value ? Checkbox.State.Checked : Checkbox.State.Unchecked);
				handleVisibilityController.SetPersistingVisibility(value || dragHandler.Dragging);
				UpdateSelectionAppearance(false);

				onSelectedStateChanged.Invoke(value);
			}
		}

		public readonly SelectedStateChangeEvent onSelectedStateChanged = new();

		private readonly List<ItemChipType> _currentChips = new();

		private Tween _backgroundTween;

		private void Awake() {
			checkbox.onClick.AddListener(() => Selected = Selected);
			backgroundButton.onClick.AddListener(() => Selected = !Selected);

			dragHandler.onDragStateChanged.AddListener(dragging =>
				handleVisibilityController.SetPersistingVisibility(Selected || dragging));
		}

		private void Update() {
			if (LevelFileExists == _selectedLevelFileExists) return;

			bool exist = _selectedLevelFileExists = LevelFileExists;

			(exist ? title : subTitle).text = TitleCache;
			subTitleObject.SetActive(!exist);

			if (exist) LoadLevelInfo(ref levelData);
			else title.text = I18N.Get("editor-no-level-file");

			UpdateSelectionAppearance(true);
		}

		private void AddChip(ItemChipType chipType, [CanBeNull] string additionalText = null) {
			var chip = Instantiate(chipPrefab, chipContainer);
			chip.ChangeText(I18N.Get($"editor-chip-{chipType}"), additionalText);
			chips.Add(chip);
			_currentChips.Add(chipType);
		}

		private void ClearChips() {
			foreach (var chip in chips)
				Destroy(chip.gameObject);

			chips.Clear();
			_currentChips.Clear();
		}

		private void UpdateSelectionAppearance(bool skipTransition) {
			var targetColor = Selected
				? _selectedLevelFileExists ? SelectedColor : SelectedDeletedColor
				: _selectedLevelFileExists
					? DefaultColor
					: DeletedColor;

			if (skipTransition)
				background.color = targetColor;
			else {
				_backgroundTween?.Kill();
				_backgroundTween = DOTween.To(() => background.color, c => background.color = c,
						targetColor, TransitionDuration)
					.SetUpdate(true);
			}
		}

		public void ReloadChips() {
			ClearChips();

			if (levelData.DisableAccuracyConstraint)
				AddChip(ItemChipType.DisableAccuracyConstraint);

			if (levelData.DisableDeathConstraint)
				AddChip(ItemChipType.DisableDeathConstraint);

			if (levelData.DisableLifeConstraint)
				AddChip(ItemChipType.DisableLifeConstraint);

			if (levelData.Mysterious)
				AddChip(ItemChipType.Mysterious);

			var cutsceneFile = levelData.CutsceneFile;

			if (!string.IsNullOrEmpty(cutsceneFile))
				AddChip(ItemChipType.Cutscene, cutsceneFile);

			chipContainer.gameObject.SetActive(_currentChips.Count > 0);
		}

		public void FillLevelInfo(CourseLevel level) {
			LoadLevelInfo(ref level);
			levelData = level;

			_levelFileWatcher?.Dispose();

			_levelFileWatcher = new FileSystemWatcher(Path.GetDirectoryName(level.AbsolutePath)!);
			_levelFileWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
			                                 NotifyFilters.LastWrite | NotifyFilters.CreationTime;
			_levelFileWatcher.Filter = Path.GetFileName(level.AbsolutePath);
			_levelFileWatcher.EnableRaisingEvents = true;

			_levelFileWatcher.Created += (_, _) => {
				LevelFileExists = true;
			};
			_levelFileWatcher.Deleted += (_, _) => {
				LevelFileExists = false;
			};
			_levelFileWatcher.Renamed += (_, _) => {
				LevelFileExists = File.Exists(level.AbsolutePath);
			};

			LevelFileExists = File.Exists(level.AbsolutePath);
		}

		private void LoadLevelInfo(ref CourseLevel level) {
			if (level.LevelMeta is { } levelMeta) {
				artist.text = levelMeta.Artist;
				creator.text = levelMeta.Creator;
				TitleCache = levelMeta.Song;
			} else {
				artist.text = creator.text = null;
				TitleCache = I18N.Get("editor-unknown-level");
			}

			if (artist.text.IsNullOrEmpty())
				artist.text = "-";

			if (creator.text.IsNullOrEmpty())
				creator.text = "-";

			(_selectedLevelFileExists ? title : subTitle).text = TitleCache;
		}

		public void SetNumber(int number) {
			OrderNumber = number;
			order.text = number.ToString();
		}
	}
}