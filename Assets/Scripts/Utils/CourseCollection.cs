using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CourseMod.DataModel;
using Newtonsoft.Json;

namespace CourseMod.Utils {
	public static class CourseCollection {
		public static readonly Dictionary<string, Course> Courses = new();
		public static readonly Dictionary<string, CoursePlayRecord> CourseRecords = new();
		public static readonly Dictionary<string, LevelMeta> LevelMetas = new(); // key: path
		private static FileSystemWatcher _courseDirectoryWatcher;

		// TODO implement fs watcher and load/unload the courses
		// public static Course[] GetCourses() => Courses.Values.ToArray();
		//
		// public static void Refresh()
		// {
		// 	Courses.Clear();
		//
		// 	var courseDir = ModDataStorage.CourseDirectory;
		// 	if (!Directory.Exists(courseDir))
		// 		Directory.CreateDirectory(courseDir);
		// 	
		// 	_courseDirectoryWatcher?.Dispose();
		// 	
		// 	_courseDirectoryWatcher = new FileSystemWatcher(courseDir);
		// 	_courseDirectoryWatcher.NotifyFilter = NotifyFilters.LastWrite;
		// 	// _courseDirectoryWatcher.;
		//
		// 	
		// }

		public static void Reset() {
			Courses.Clear();
			CourseRecords.Clear();
			LevelMetas.Clear();
		}

		// beware that other threads may call this method
		public static void RegisterCourse(Course course) {
			lock (Courses) {
				Courses[course.Id] = course;
				LogTools.Log($"Registered course [{course.Id}]");
			}
		}

		public static void UnregisterCourse(Course course) {
			lock (Courses) {
				if (!Courses.Remove(course.Id))
					LogTools.Log($"Failed to unregister course [{course.Id}] because it doesn't exist");
			}
		}

		public static string[] GetCoursePaths(string coursesPath) => Directory
			.GetFiles(coursesPath, "*.course", SearchOption.AllDirectories);

		public static async Task<Course> ReadSingleCourseAsync(string courseFilePath) {
			var text = await File.ReadAllTextAsync(courseFilePath);
			return ParseCourse(courseFilePath, text);
		}

		public static Course ReadSingleCourse(string courseFilePath) {
			var text = File.ReadAllText(courseFilePath);
			return ParseCourse(courseFilePath, text);
		}

		private static Course ParseCourse(string path, string content) {
			var result = JsonConvert.DeserializeObject<Course>(content);

			result.FilePath = path;
			var courseDirectory = Path.GetDirectoryName(path)!;

			for (var i = 0; i < result.Levels.Count; i++) {
				var level = result.Levels[i];
				level.AbsolutePath = Path.Combine(courseDirectory, level.Path);

				result.Levels[i] = level;
			}

			return result;
		}

		public static string ParseDestinationDirectory(string name, string parent = null) {
			parent ??= ModDataStorage.CourseDirectory;
			if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);

			string destinationDirectory = Path.Combine(parent, name);
			int duplicateIndex = 1;
			while (Directory.Exists(destinationDirectory))
				destinationDirectory = Path.Combine(parent, $"{name} ({duplicateIndex++})");
			return destinationDirectory;
		}
	}
}