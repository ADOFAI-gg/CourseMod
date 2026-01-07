using System;
using System.Collections.Generic;
using CourseMod.Components.Atoms;
using CourseMod.Components.Atoms.Button;
using CourseMod.Exceptions;
using CourseMod.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.ContextMenu {
	public class ContextMenuItem<T, THandle> where T : Enum {
		public delegate void OnClick();

		public delegate ItemStatus Validator();


		private ButtonStyle _target;
		private TextI18N _textI18N;
		public readonly T Key;
		private string _i18NKey;
		public bool Danger;
		private readonly OnClick _onClick;
		private readonly Validator _validatorFunc;

		public ContextMenuItem(T key, string i18NKeyPrefix, bool danger, OnClick onClick, Validator validator = null) {
			Key = key;
			_i18NKey = i18NKeyPrefix + key;
			Danger = danger;
			_onClick = onClick;
			_validatorFunc = validator;
		}

		~ContextMenuItem() {
			Object.Destroy(_target.gameObject);
		}


		public void BindButtonStyle(ButtonStyle target) {
			Assert.False(_target, "BindObject() can only be called once");
			_target = target;
			TextI18N textI18N = target.buttonText.gameObject.AddComponent<TextI18N>();
			textI18N.key = _i18NKey;
			_textI18N = textI18N;
			_target.button.onClick.AddListener(_onClick.Invoke);
		}

		public bool SetI18NKey(string i18NKey) {
			CheckBinding();
			if (_i18NKey == i18NKey) return false;
			_textI18N.key = i18NKey;
			_i18NKey = i18NKey;
			return true;
		}

		public bool SetDanger(bool danger) {
			CheckBinding();
			if (Danger == danger) return false;
			Danger = danger;
			_target.selectedStyle = danger ? ButtonStyle.StyleType.ItemDanger : ButtonStyle.StyleType.Item;
			return true;
		}

		public void SetStatus(ItemStatus status) {
			CheckBinding();
			switch (status) {
				case ItemStatus.Show:
					_target.gameObject.SetActive(true);
					_target.Disabled = false;
					break;
				case ItemStatus.Disable:
					_target.gameObject.SetActive(true);
					_target.Disabled = true;
					break;
				case ItemStatus.Hide:
					_target.gameObject.SetActive(false);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, null);
			}
		}

		public override bool Equals(object obj) {
			return obj is ContextMenuItem<T, THandle> t && Key.Equals(t.Key);
		}

		public ItemStatus Validate() {
			CheckBinding();
			return _validatorFunc?.Invoke() ?? ItemStatus.Show;
		}

		private void CheckBinding() {
			Assert.True(_target,
				"Unbound item cannot invoke RefreshVisibility(object); Use BindObject(GameObject) first");
		}

		public override int GetHashCode() {
			return Key.GetHashCode();
		}
	}
}