using System;
using System.Linq;
using CourseMod.Components.Atoms.Backdrop;
using CourseMod.Components.Atoms.Button;
using CourseMod.Components.Scenes;
using CourseMod.Exceptions;
using CourseMod.Utils;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.ContextMenu {
	public abstract class ContextMenu<T, THandler> : BackdropContainer
		where T : unmanaged, Enum where THandler : BackdropScene {
		protected THandler Handler;

		protected abstract string I18NKeyPrefix { get; }

		private ContextMenuItem<T, THandler>[] _items;

		private RectTransform _rectTransform;

		public ButtonStyle prefab;

		protected event Action OnOpen = () => { };
		protected event Action OnClose = () => { };

		public void Init(THandler handler, in BackdropSettings backdropSettings) {
			base.Init(handler, backdropSettings);
			Handler = handler;
			_rectTransform = transform as RectTransform;
			_items = new ContextMenuItem<T, THandler>[Enum.GetValues(typeof(T)).Length];
			InitializeItems();
			CheckItems();
		}

		protected abstract void InitializeItems();

		private void CheckItems() {
			Assert.True(_items.All(e => e != null), "Some items aren't initialized");
		}

		protected void InitItem(ContextMenuItemGenerator<T, THandler> itemGenerator) {
			ContextMenuItem<T, THandler> item = itemGenerator.CreateItem();
			int index = UnsafeUtility.EnumToInt(item.Key);

			Assert.True(_items[index] == null, $"The item {item.Key} cannot be overwritten");

			ButtonStyle itemPrefabInstance = Instantiate(prefab, transform);
			item.BindButtonStyle(itemPrefabInstance);

			itemPrefabInstance.selectedStyle =
				item.Danger ? ButtonStyle.StyleType.ItemDanger : ButtonStyle.StyleType.Item;
			itemPrefabInstance.button.onClick.AddListener(CloseBackdropManually);

			_items[index] = item;
		}

		public ContextMenuItem<T, THandler> GetItem(T key) {
			int index = UnsafeUtility.EnumToInt(key);
			return _items[index];
		}

		public void Open(Vector2? position) {
			foreach (ContextMenuItem<T, THandler> item in _items) {
				ItemStatus status = item.Validate();
				item.SetI18NKey($"{I18NKeyPrefix}{item.Key}");
				item.SetStatus(status);
			}

			OpenBackdrop();
			gameObject.SetActive(true);
			if (position != null) _rectTransform.position = position.Value;
			OnOpen();
		}


		protected override void OnBackdropClose() {
			OnClose();
			gameObject.SetActive(false);
		}
	}
}