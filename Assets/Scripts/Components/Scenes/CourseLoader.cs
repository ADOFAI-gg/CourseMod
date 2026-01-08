using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CourseMod.Components.Molecules.SelectLevelItem;
using CourseMod.DataModel;
using CourseMod.Utils;
using JetBrains.Annotations;
using UnityEngine;

namespace CourseMod.Components.Scenes {
	public class CourseLoader : MonoBehaviour {
		public CourseSelectScene courseSelect;

		private int _currentLoadedIndex;
		private int _totalCourses = -1;

		private readonly Queue<string> _requireReplyCourses = new();
		private readonly List<SetupOtherCourseDataTask> _pendingTasks = new();

		private AsyncInstantiateOperation<SelectLevelItem> _instantiateOperation;

		private void Awake() {
			CourseCollection.Reset();
			Task.Run(ReadCourseTask);
		}

		public async void ReadCourseTask() {
			try {
				string coursePath = ModDataStorage.CourseDirectory;
				if (!File.Exists(coursePath)) Directory.CreateDirectory(coursePath);
				string[] courseFiles = CourseCollection.GetCoursePaths(coursePath);
				_totalCourses = courseFiles.Length;
				foreach (string path in courseFiles) {
					Course course = await CourseCollection.ReadSingleCourseAsync(path);
					CourseCollection.RegisterCourse(course);
					LoadAfter(ref course);
				}
			} catch (Exception e) {
				LogTools.LogException("Error reading courses", e);
			}
		}

		private void Update() {
			if (_instantiateOperation == null) {
				if (_requireReplyCourses.Count > 0) {
					_instantiateOperation = InstantiateAsync(courseSelect.levelItemPrefab, _requireReplyCourses.Count,
						courseSelect.coursesContainer);
				}
			} else if (_instantiateOperation.isDone) {
				foreach (SelectLevelItem selectLevelItem in _instantiateOperation.Result) {
					selectLevelItem.CourseSelect = courseSelect;
					selectLevelItem.transform.localScale = new Vector3(1, 1, 1);
					selectLevelItem.AssignCourse(CourseCollection.Courses[_requireReplyCourses.Dequeue()]);
					if (!courseSelect.ChosenItem) courseSelect.SelectItem(selectLevelItem);
				}

				_instantiateOperation = null;
			}

			for (int i = 0; i < _pendingTasks.Count; i++) {
				SetupOtherCourseDataTask pendingTask = _pendingTasks[i];
				string courseId = pendingTask.CourseId;

				if (pendingTask.PlayRecordSetupState == 1) {
					pendingTask.PlayRecordSetupState = 2;
					if (courseSelect.ChosenItem?.Course.Id == courseId)
						courseSelect.UpdateCoursePlayRecord();
				}

				if (pendingTask.LevelMetasCompleted > pendingTask.LevelMetasSetupted) {
					if (courseSelect.ChosenItem?.Course.Id == courseId) {
						List<CourseLevel> levels = CourseCollection.Courses[courseId].Levels;
						while (pendingTask.LevelMetasCompleted > pendingTask.LevelMetasSetupted) {
							CourseLevel level = levels[pendingTask.LevelMetasSetupted];
							courseSelect.ResultItems[pendingTask.LevelMetasSetupted].levelName.text =
								CourseCollection.LevelMetas[level.Path].Song;
							pendingTask.LevelMetasSetupted++;
						}
					} else pendingTask.LevelMetasSetupted = pendingTask.LevelMetasCompleted;
				}

				if (pendingTask.PlayRecordSetupState == 2 &&
				    pendingTask.LevelMetasSetupted >= CourseCollection.Courses[courseId].Levels.Count)
					_pendingTasks.RemoveAt(i--);
			}

			if (_currentLoadedIndex == _totalCourses && _pendingTasks.Count == 0) DestroyImmediate(this);
		}

		public void LoadAfter(ref Course course) {
			_requireReplyCourses.Enqueue(course.Id);
			_currentLoadedIndex++;

			SetupOtherCourseDataTask task = new(this, course.Id);
			Task.Run(task.SetupPlayRecord);
			Task.Run(task.SetupLevelMetas);
			_pendingTasks.Add(task);

			foreach (CourseLevel courseLevel in course.Levels)
				CourseCollection.LevelMetas.TryAdd(courseLevel.Path, null);
		}

		private class SetupOtherCourseDataTask {
			private readonly CourseLoader _instance;
			[NotNull] public readonly string CourseId;
			public int PlayRecordSetupState;
			public int LevelMetasCompleted;
			public int LevelMetasSetupted;

			public SetupOtherCourseDataTask(CourseLoader instance, string courseId) {
				_instance = instance;
				CourseId = courseId;
			}

			public void SetupPlayRecord() {
				try {
					if (!CourseCollection.CourseRecords.ContainsKey(CourseId)) {
						Course course = CourseCollection.Courses[CourseId];
						CoursePlayRecord record = course.GetPlayRecord();
						lock (_instance) CourseCollection.CourseRecords[CourseId] = record;
					}

					PlayRecordSetupState = 1;
				} catch (Exception e) {
					LogTools.LogException("Error loading play record for course " + CourseId, e);
				}
			}

			public void SetupLevelMetas() {
				try {
					LogTools.Log("Setting up level metas for course " + CourseId);
					Course course = CourseCollection.Courses[CourseId];
					foreach (CourseLevel level in course.Levels) {
						LogTools.Log("Setting up level meta for level " + level.Path);
						if (CourseCollection.LevelMetas[level.Path] == null) {
							try {
								LogTools.Log("Loading level meta for level at path " + level.Path);
								LevelMeta meta = new(level.AbsolutePath);
								CourseCollection.LevelMetas[level.Path] = meta;
							} catch (Exception e) {
								LogTools.LogException($"Error loading level meta for level at path {level.Path}", e);
							}
						}

						LevelMetasCompleted++;
						LogTools.Log("Completed level metas: " + LevelMetasCompleted);
					}
				} catch (Exception e) {
					LogTools.LogException("Error loading level metas for course " + CourseId, e);
				}
			}
		}
	}
}