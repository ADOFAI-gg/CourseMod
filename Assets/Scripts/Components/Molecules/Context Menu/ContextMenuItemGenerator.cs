//Resharper disable CheckNamespace

using System;

namespace CourseMod.Components.Molecules.ContextMenu {
	public abstract class ContextMenuItemGenerator<T, THandle> where T : unmanaged, Enum {
		protected readonly THandle Handler;
		protected readonly T Key;
		private readonly string _i18NKeyPrefix;
		protected readonly bool Danger;

		protected ContextMenuItemGenerator(THandle handler, T key, string i18NKeyPrefix, bool danger) {
			Handler = handler;
			Key = key;
			_i18NKeyPrefix = i18NKeyPrefix;
			Danger = danger;
		}


		public ContextMenuItem<T, THandle> CreateItem() {
			return new ContextMenuItem<T, THandle>(
				key: Key,
				i18NKeyPrefix: _i18NKeyPrefix,
				danger: Danger,
				onClick: OnClick,
				validator: Validate
			);
		}

		protected abstract void OnClick();

		protected abstract ItemStatus Validate();
	}
}