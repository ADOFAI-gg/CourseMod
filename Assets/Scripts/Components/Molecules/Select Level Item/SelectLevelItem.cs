using System;
using CourseMod.Components.Scenes;
using CourseMod.DataModel;
using CourseMod.Utils;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ReSharper disable CheckNamespace
// TODO accidentally named it level item, should be course item instead
namespace CourseMod.Components.Molecules.SelectLevelItem {
	public class SelectLevelItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
		IPointerExitHandler {
		[NonSerialized] public CourseSelectScene CourseSelect;
		public Image background;
		public Button button;

		public TextMeshProUGUI courseName;
		public TextMeshProUGUI courseCreator;

		public bool Selected => CourseSelect && CourseSelect.ChosenItem == this;
		public Course Course => _course!.Value;

		private Course? _course;

		private bool _hovered;
		private bool _active;

		private Tween _tween;

		private void Awake() {
			button.onClick.AddListener(() => CourseSelect.SelectItem(this));

			UpdateAppearance(true);
		}

		public void AssignCourse(Course course) {
			_course = course;

			courseName.text = course.Name;
			courseCreator.text = course.Creator;
		}

		public void UpdateAppearance(bool skipTransition = false) {
			if (_active || Selected) {
				SetBackground(Color.black.SetAlpha(.2f));
				return;
			}

			if (_hovered) {
				SetBackground(Color.black.SetAlpha(.1f));
				return;
			}

			SetBackground(Color.black.SetAlpha(0));
			return;

			void SetBackground(Color color) {
				if (skipTransition)
					background.color = color;
				else {
					_tween?.Kill();
					_tween = background.DOColor(color, .2f).SetUpdate(true);
				}
			}
		}

		public void OnPointerDown(PointerEventData _) {
			_active = true;
			UpdateAppearance(true);
		}

		public void OnPointerUp(PointerEventData _) {
			_active = false;
			UpdateAppearance(true);
		}

		public void OnPointerEnter(PointerEventData _) {
			_hovered = true;
			UpdateAppearance();
		}

		public void OnPointerExit(PointerEventData _) {
			_hovered = false;
			UpdateAppearance();
		}
	}
}