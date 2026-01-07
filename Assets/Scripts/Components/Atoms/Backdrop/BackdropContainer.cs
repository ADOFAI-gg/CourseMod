using System;
using CourseMod.Components.Scenes;
using CourseMod.Utils;
using UnityEngine;

//Resharper disable CheckNamespace
namespace CourseMod.Components.Atoms.Backdrop {
	public abstract class BackdropContainer : MonoBehaviour {
		[NonSerialized] private Backdrop _backdrop;
		private BackdropScene _scene;
		private BackdropSettings _backdropSettings;
		private bool _initialized;

		protected void Init(BackdropScene scene, BackdropSettings backdropSettings) {
			Assert.False(_initialized, $"{GetType()}.Init() can only be called once");
			_scene = scene;
			_backdropSettings = backdropSettings;
			_initialized = true;
		}


		protected void OpenBackdrop() {
			CheckInit();
			if (Open()) {
				_backdrop.OnBackdropEnd.AddListener(OnBackdropClose);
			}
		}

		protected void CloseBackdropManually() {
			Close();
			OnBackdropClose();
		}


		private void CheckInit() {
			Assert.True(_initialized, "ContextMenu is not initialized. Did you call ContextMenu.Init()?");
		}

		private bool Open() {
			if (!_backdrop) {
				_backdrop = Instantiate(_scene.backdropPrefab, _scene.backdropContainer);
				_backdrop.ResetBackdrop(_backdropSettings);
				return true;
			}

			_backdrop.Start();
			_backdrop.ResetBackdrop(_backdropSettings);
			return false;
		}

		private void Close() {
			Assert.True(_backdrop, "Backdrop is not initialized. Did you call .Open()?");
			_backdrop.End();
		}

		protected abstract void OnBackdropClose();
	}
}