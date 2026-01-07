using System;
using System.Collections.Generic;
using System.Linq;
using CourseMod.Components.Molecules.ContextMenu;
using CourseMod.Components.Molecules.DragHandle;
using CourseMod.Components.Molecules.EditorLevelItem;
using CourseMod.Components.Scenes;
using CourseMod.DataModel;
using CourseMod.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Organisms.EditorLevelList {
	public class EditorLevelList : MonoBehaviour {
		public class ItemCountChangedEvent : UnityEvent<int> {
		}

		public class ItemSelectionChangedEvent : UnityEvent<EditorLevelItem[]> {
		}


		private static readonly Vector2 ContextMenuPositionOffset = new(-8, 0);


		private readonly List<EditorLevelItem> _items = new();

		public RectTransform contents;
		public EditorLevelItem itemPrefab;
		public LayoutElement ghostItem;

		public TextMeshProUGUI addLevelLabel;
		public LevelListContextMenu itemContextMenu;

		public EditorLevelItem[] SelectedLevels => _items.Where(item => item.Selected).ToArray();
		public readonly ItemSelectionChangedEvent onItemSelectionChanged = new();
		public readonly ItemCountChangedEvent onItemCountChanged = new();


		private void Awake() {
			OnItemCountChanged();
		}


		public void OpenContextMenu(EditorLevelItem item) {
			itemContextMenu.Open(item.transform.position.xy() + ContextMenuPositionOffset);
		}


		private void OnItemReordered(DragHandler.ReorderEventData eventData) {
			if (_items.Count == 0) return;
			if (eventData.OriginalIndex == eventData.NewIndex) return;

			var maxItemIndex = _items.Count - 1;
			var origIdx = Math.Clamp(eventData.OriginalIndex, 0, maxItemIndex);
			var newIdx = Math.Clamp(eventData.NewIndex, 0, maxItemIndex);

			_items.Insert(newIdx + (origIdx < newIdx ? 1 : 0), _items[origIdx]);
			_items.RemoveAt(origIdx + (origIdx > newIdx ? 1 : 0));

			var (minIndex, maxIndex) = (Math.Min(origIdx, newIdx), Math.Max(origIdx, newIdx));

			for (var i = minIndex; i <= maxIndex; i++) {
				var item = _items[i];
				item.SetNumber(i + 1);
			}
		}

		public void AddLevels(IEnumerable<CourseLevel> courseLevels) {
			foreach (var courseLevel in courseLevels) {
				InstantiateLevelItem(courseLevel);
			}

			OnItemCountChanged();
		}

		public EditorLevelItem[] GetItems() => _items.ToArray();

		public EditorLevelItem AddLevel(CourseLevel courseLevel) {
			EditorLevelItem item = InstantiateLevelItem(courseLevel);
			OnItemCountChanged();
			return item;
		}

		private EditorLevelItem InstantiateLevelItem(CourseLevel courseLevel) {
			var item = Instantiate(itemPrefab, contents);
			item.dragHandler.onReordered.AddListener(OnItemReordered);
			item.dragHandler.onDragStateChanged.AddListener(MoveNoLevelItemInBackground);
			item.dragHandler.onClicked.AddListener(() => {
				item.Selected = true;
				OpenContextMenu(item);
			});
			item.dragHandler.GhostItem = ghostItem;
			item.onSelectedStateChanged.AddListener(_ => onItemSelectionChanged.Invoke(SelectedLevels));

			item.FillLevelInfo(courseLevel);
			item.SetNumber(_items.Count + 1);
			item.ReloadChips();
			_items.Add(item);
			return item;
		}

		public EditorLevelItem[] GetNotFoundLevelItems() => _items.Where(item => !item.LevelFileExists).ToArray();

		public void RemoveNotFoundLevelItems() {
			for (int i = _items.Count - 1; i >= 0; i--) {
				if (_items[i].LevelFileExists) continue;
				EditorLevelItem item = _items[i];
				_items.RemoveAt(i);
				Destroy(item.gameObject);
			}

			OnItemCountChanged();
		}

		public void RemoveLevel(EditorLevelItem item) {
			_items.Remove(item);

			Destroy(item.gameObject);
		}

		public void RemoveLevels(EditorLevelItem[] items) {
			foreach (var item in items) {
				_items.Remove(item);
				Destroy(item.gameObject);
			}

			OnItemCountChanged();
		}

		public void Clear() {
			foreach (var item in _items)
				Destroy(item.gameObject);

			_items.Clear();

			OnItemCountChanged();
		}

		private void MoveNoLevelItemInBackground(bool _) {
			if (addLevelLabel)
				addLevelLabel.transform.SetAsLastSibling();
		}

		public void SelectAllLevels() => SelectLevels(_items);

		public void SelectLevels(IEnumerable<EditorLevelItem> items) {
			foreach (var item in items)
				item.Selected = true;
		}

		public void DeselectAllLevels() => DeselectLevels(_items);

		public void DeselectLevels(IEnumerable<EditorLevelItem> items) {
			foreach (var item in items)
				item.Selected = false;
		}

		public void DuplicateSelection() {
			var duplicates = SelectedLevels.Select(item => AddLevel(item.levelData)).ToList();

			DeselectAllLevels();
			SelectLevels(duplicates);
		}

		public List<CourseLevel> ToLevels() => _items.Select(level => level.levelData).ToList();

		public void OnItemCountChanged() {
			var count = _items.Count;
			addLevelLabel.gameObject.SetActive(count == 0);
			if (count == 0)
				addLevelLabel.text = I18N.Get(CourseEditorScene.CurrentCourse == null
					? "editor-no-levels-added-no-course"
					: "editor-no-levels-added");

			onItemCountChanged.Invoke(count);
		}
	}
}