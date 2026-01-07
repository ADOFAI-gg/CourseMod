using System.IO;
using UnityEngine;

namespace CourseMod.Utils {
	public static class FileTools {
		public static string CopyLevelToCourseDir(string coursePath, string srcLevelPath) {
			string courseDir = Path.GetDirectoryName(Path.GetFullPath(coursePath))!;
			string srcDir = Path.GetDirectoryName(srcLevelPath);
			string dstDir = Path.Combine(courseDir, Path.GetFileName(srcDir)!)!;

			Debug.Log(courseDir + " " + srcDir + " " + dstDir);
			if (srcDir == dstDir) return srcLevelPath; // has no effect

			if (Directory.Exists(dstDir)) Directory.Delete(dstDir, true);
			RDDirectory.Copy(srcDir, dstDir, true);
			return Path.Combine(dstDir, Path.GetFileName(srcLevelPath)!);
		}
	}
}