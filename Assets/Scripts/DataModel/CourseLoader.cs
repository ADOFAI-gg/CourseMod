using System;
using System.Collections;
using System.IO;
using CourseMod.Components.Scenes;
using CourseMod.Utils;
using UnityEngine;

namespace CourseMod.DataModel {
    public class CourseLoader : MonoBehaviour {
        public CourseSelectScene courseSelect;

        private IEnumerator Start() {
            CourseCollection.Reset();

            var directory = ModDataStorage.CourseDirectory;
            Directory.CreateDirectory(directory);

            string[] paths;
            try {
                paths = CourseCollection.GetCoursePaths(directory);
            } catch (Exception e) {
                LogTools.LogException("Error finding courses", e);
                yield break;
            }

            foreach (var path in paths) {
                Course course;

                try {
                    course = CourseCollection.ReadSingleCourse(path);
                } catch (Exception e) {
                    LogTools.LogException($"Error reading course at {path}", e);
                    continue;
                }

                foreach (var level in course.Levels) {
                    try {
                        if (!CourseCollection.LevelMetas.ContainsKey(level.Path)) {
                            CourseCollection.LevelMetas[level.Path] =
                                new LevelMeta(level.AbsoluteFilePath);
                        }
                    } catch (Exception e) {
                        LogTools.LogException(
                            $"Error loading level meta at {level.Path}", e);
                        CourseCollection.LevelMetas.TryAdd(level.Path, null);
                    }

                    yield return null;
                }

                try {
                    var record = course.GetPlayRecord();
                    if (record is { } value)
                        CourseCollection.CourseRecords[course.Id] = value;

                    CourseCollection.RegisterCourse(course);

                    var item = Instantiate(
                        courseSelect.levelItemPrefab,
                        courseSelect.coursesContainer);

                    item.CourseSelect = courseSelect;
                    item.AssignCourse(course);

                    if (!courseSelect.ChosenItem)
                        courseSelect.SelectItem(item);
                } catch (Exception e) {
                    LogTools.LogException(
                        $"Error setting up course at {path}", e);
                }

                yield return null;
            }

            Destroy(this);
        }
    }
}
