using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

// ReSharper disable InconsistentNaming

namespace CourseMod.Utils {
	public class ExtendedLayoutElement : UIBehaviour, ILayoutElement {
		/// <inheritdoc/>
		public void CalculateLayoutInputHorizontal() { }

		/// <inheritdoc/>
		public void CalculateLayoutInputVertical() { }

		public float flexibleWidth => -1;
		public float flexibleHeight => -1;
		public float minWidth => -1;
		public float minHeight => -1;

		public float preferredWidth =>
			Mathf.Min(GetLayoutProperty((RectTransform) transform, element => element.preferredWidth, -1), maxWidth);

		public float preferredHeight =>
			Mathf.Min(GetLayoutProperty((RectTransform) transform, element => element.preferredHeight, -1), maxHeight);

		public float maxWidth {
			get => m_maxWidth;
			set {
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (m_maxWidth == value) return;

				m_maxWidth = value;
				SetDirty();
			}
		}

		public float maxHeight {
			get => m_maxHeight;
			set {
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (m_maxHeight == value) return;

				m_maxHeight = value;
				SetDirty();
			}
		}

		public int layoutPriority {
			get => m_layoutPriority;
			set {
				if (m_layoutPriority == value) return;

				m_layoutPriority = value;
				SetDirty();
			}
		}

		[SerializeField] private float m_maxWidth = -1;
		[SerializeField] private float m_maxHeight = -1;
		[SerializeField] private int m_layoutPriority = 100;

		/// <summary>
		/// Mark the LayoutElement as dirty.
		/// </summary>
		/// <remarks>
		/// This will make the auto layout system process this element on the next layout pass. This method should be called by the LayoutElement whenever a change is made that potentially affects the layout.
		/// </remarks>
		private void SetDirty() {
			if (!IsActive())
				return;

			LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
		}

		/// <summary>
		/// Gets a calculated layout property for the layout element with the given RectTransform.
		/// </summary>
		/// <param name="rect">The RectTransform of the layout element to get a property for.</param>
		/// <param name="property">The property to calculate.</param>
		/// <param name="defaultValue">The default value to use if no component on the layout element supplies the given property</param>
		/// <returns>The calculated value of the layout property.</returns>
		private float GetLayoutProperty(RectTransform rect, System.Func<ILayoutElement, float> property,
			float defaultValue) {
			if (rect == null)
				return 0;
			float min = defaultValue;
			int maxPriority = System.Int32.MinValue;
			var components = ListPool<Component>.Get();
			rect.GetComponents(typeof(ILayoutElement), components);

			var componentsCount = components.Count;
			for (int i = 0; i < componentsCount; i++) {
				var layoutComp = components[i] as ILayoutElement;
				if (layoutComp is Behaviour { isActiveAndEnabled: false })
					continue;

				int priority = layoutComp!.layoutPriority;
				// If this layout components has lower priority than a previously used, ignore it.
				if (priority < maxPriority)
					continue;

				// Skip components with higher priorities
				if (priority >= layoutPriority)
					continue;

				float prop = property(layoutComp);
				// If this layout property is set to a negative value, it means it should be ignored.
				if (prop < 0)
					continue;

				// If this layout component has higher priority than all previous ones,
				// overwrite with this one's value.
				if (priority > maxPriority) {
					min = prop;
					maxPriority = priority;
				}
				// If the layout component has the same priority as a previously used,
				// use the largest of the values with the same priority.
				else if (prop > min) {
					min = prop;
				}
			}

			ListPool<Component>.Release(components);
			return min;
		}

#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();
			SetDirty();
		}
#endif
	}
}