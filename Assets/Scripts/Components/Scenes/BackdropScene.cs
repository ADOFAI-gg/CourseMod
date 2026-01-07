using System.Collections;
using System.Linq;
using CourseMod.Components.Atoms.Backdrop;
using CourseMod.Components.Molecules.ContextMenu;
using UnityEngine;

namespace CourseMod.Components.Scenes {
	public class BackdropScene : MonoBehaviour {
		public RectTransform backdropContainer;
		public Backdrop backdropPrefab;

		protected bool ActiveBackdropExists {
			get {
				return backdropContainer.transform.Cast<Transform>().Any(e => e.gameObject.activeSelf);
			}
		}
	}
}