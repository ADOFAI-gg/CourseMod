using System;
using System.Collections.Generic;
using CourseMod.Utils;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.DragHandle {
	public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler,
		IPointerUpHandler {
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public record ReorderEventData {
			public readonly int OriginalIndex;
			public readonly int NewIndex;

			public ReorderEventData(int originalIndex, int newIndex) {
				OriginalIndex = originalIndex + (originalIndex > newIndex ? -1 : 0);
				NewIndex = newIndex;
			}
		}

		public class OnClickedEvent : UnityEvent {
		}

		public class OnReorderedEvent : UnityEvent<ReorderEventData> {
		}

		public class OnDragStateChangeEvent : UnityEvent<bool> {
		}

		public LayoutElement layoutElement;

		[NonSerialized] public LayoutElement GhostItem;

		private readonly List<MaskableGraphic> _mutatedGraphics = new();
		private bool _lastIgnoreLayout;

		public readonly OnClickedEvent onClicked = new();
		public readonly OnReorderedEvent onReordered = new();
		public readonly OnDragStateChangeEvent onDragStateChanged = new();

		private Vector2 _pressMPos;
		private const float PressReleaseOffsetThreshold = 5f;

		public bool Dragging { get; private set; }

		public void OnPointerDown(PointerEventData eventData) {
			_pressMPos = eventData.position;
		}

		public void OnPointerUp(PointerEventData eventData) {
			Vector2 delta = eventData.position - _pressMPos;
			if (delta.magnitude > PressReleaseOffsetThreshold) return;

			onClicked.Invoke();
		}

		public void OnBeginDrag(PointerEventData eventData) {
			_mutatedGraphics.Clear();

			var graphics = transform.GetComponentsInChildren<MaskableGraphic>();
			graphics.ForEach(g => {
				if (!g.maskable)
					return;

				g.maskable = false;
				_mutatedGraphics.Add(g);
			});

			if (layoutElement == null)
				return;

			_lastIgnoreLayout = layoutElement.ignoreLayout;
			layoutElement.ignoreLayout = true;

			if (!GhostItem)
				return;

			GhostItem.transform.SetSiblingIndex(layoutElement.transform.GetSiblingIndex());
			GhostItem.preferredHeight = ((RectTransform) layoutElement.transform).rect.height;

			GhostItem.gameObject.SetActive(true);

			onDragStateChanged.Invoke(Dragging = true);
		}

		public void OnEndDrag(PointerEventData eventData) {
			_mutatedGraphics.ForEach(g => g.maskable = true);
			_mutatedGraphics.Clear();

			if (layoutElement == null)
				return;

			layoutElement.ignoreLayout = _lastIgnoreLayout;

			if (!GhostItem)
				return;

			var itemTransform = layoutElement.transform;
			var ghostTransform = GhostItem.transform;

			var maxIndex = layoutElement.transform.parent.childCount - 2;

			var oldIndex = Math.Clamp(itemTransform.GetSiblingIndex(), 0, maxIndex);
			var newIndex = Math.Clamp(ghostTransform.GetSiblingIndex() - 1, 0, maxIndex);

			itemTransform.SetSiblingIndex(newIndex);

			GhostItem.gameObject.SetActive(false);
			ghostTransform.SetAsLastSibling();

			onReordered.Invoke(new(oldIndex, newIndex));

			onDragStateChanged.Invoke(Dragging = false);
		}

		public void OnDrag(PointerEventData eventData) {
			if (layoutElement == null || !GhostItem)
				return;

			var listRt = layoutElement.transform.parent as RectTransform;
			var itemRt = (RectTransform) layoutElement.transform;
			var handleRt = (RectTransform) transform;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(listRt,
				eventData.position, eventData.pressEventCamera, out var eventPosValue);

			eventPosValue -= handleRt.anchoredPosition;

			var dragPosY = eventPosValue.y;
			itemRt.anchoredPosition = itemRt.anchoredPosition.SetY(dragPosY);

			GhostItem.transform.SetSiblingIndex(GetClosestSiblingIndex(listRt, dragPosY));
		}

		private int GetClosestSiblingIndex(RectTransform listRt, float y) {
			var maxChildIndex = listRt.childCount - 1;
			var selectedSiblingIndex = layoutElement.transform.GetSiblingIndex();

			for (var i = 0; i <= maxChildIndex; i++) {
				var child = listRt.GetChild(i);
				var childRt = (RectTransform) child.transform;

				var diff = y - (childRt.anchoredPosition.y + childRt.sizeDelta.y / 2);
				if (diff < 0 || i == selectedSiblingIndex)
					continue;

				return i;
			}

			return maxChildIndex;
		}
	}
}