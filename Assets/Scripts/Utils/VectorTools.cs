using JetBrains.Annotations;
using UnityEngine;

namespace CourseMod.Utils {
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class VectorTools {
		public static Vector2 SetY(this Vector2 vector, float y) {
			vector.y = y;
			return vector;
		}

		public static Vector3 SetZ(this Vector3 vector, float z) {
			vector.z = z;
			return vector;
		}

		public static Vector3 SetZ(this Vector2 vector, float z)
			=> new(vector.x, vector.y, z);
	}
}