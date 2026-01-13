using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CourseMod.Components.Scenes;
using CourseMod.DataModel;
using CourseMod.Utils;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourseMod.Patches {
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class GameplayPatches {
		private static CourseTransitionScene Instance => CourseTransitionScene.Instance;

		public static class CourseState {
			public enum FailReason {
				Accuracy,
				Death,
				Life,
			}

			public static bool PlayingCourse;
			
			private static Course? _selectedCourse;
			public static Course? SelectedCourse {
				get => _selectedCourse;
				set {
					_selectedCourse = value;

					if (value is { } course) {
						TotalLevels = course.Levels.Count;
					} else {
						TotalLevels = 0;
					}
				}
			}

			public static int TotalLevels;

			public static int LevelIndex;
			public static int PlayStartedLevelIndex;

			public static SerializableHitMargins TotalHitMargins = new();
			public static int TotalFloors;

			public static double TotalXAccuracy;
			public static int? DeathsLeft;
			public static int? LivesLeft;

			public static bool Failed = true;
			public static bool PauseStateFromPatch;
			public static bool PauseRequested;
			public static bool WonState;
			public static bool FailState;

			public static readonly List<CourseLevelPlayRecord> LevelPlayRecords = new();
			public static readonly HashSet<FailReason> FailReasons = new();

			public static void Reset() {
				TerminateCourse();

				SelectedCourse = null;
				TotalLevels = 0;

				ResetProgress();
			}

			public static void ResetProgress() {
				LogTools.Log("Resetting course state progress");

				LevelIndex = 0;
				PlayStartedLevelIndex = 0;

				TotalFloors = 0;
				TotalHitMargins = new();
				TotalXAccuracy = 0;

				DeathsLeft = SelectedCourse?.Settings.DeathConstraint;
				LivesLeft = SelectedCourse?.Settings.LifeConstraint;

				Failed = true;
				PauseStateFromPatch = false;
				PauseRequested = false;

				WonState = false;
				FailState = false;

				LevelPlayRecords.Clear();
				FailReasons.Clear();

				CourseFailUpdate.DisplayedEndScreen = false;
				CourseLevelCompleteUpdate.WonTime = null;
			}

			public static CourseLevelPlayRecord DefaultPlayRecord =>
				new() {
					CourseChecksum = SelectedCourse.HasValue
						? ChecksumTools.ComputeCourseChecksum(SelectedCourse.Value).Content
						: string.Empty,
					GameplayChecksum = string.Empty,
					HitMargins = SerializableHitMargins.Default,
					LevelNumber = 0,
					XAccuracy = 0,
					TotalFloors = 0
				};

			public static void SaveRecord() {
				if (SelectedCourse is not { } course)
					return;

				var json = JsonConvert.SerializeObject(new CoursePlayRecord { Records = LevelPlayRecords.ToArray() });
				File.WriteAllText(course.GetPlayRecordPath(), json);
			}

			public static void StoreRecord() {
				var levelNumber = LevelIndex + 1;

				if (LevelPlayRecords.LastOrDefault(r => r.LevelNumber == levelNumber) != null)
					return;

				var controller = scrController.instance;

				var tracker = controller.mistakesManager;
				var hitMarginsCount = scrMistakesManager.hitMarginsCount;
				var floors = tracker.lm.listFloors;

				var record = DefaultPlayRecord;

				record.LevelNumber = levelNumber;
				record.GameplayChecksum = ChecksumTools.ComputeGameplayChecksum(tracker.customLevel.levelData).Hash;

				record.HitMargins =
					SerializableHitMargins.FromHitMarginsCount(hitMarginsCount, scrMistakesManager.hitMargins.Count);
				record.XAccuracy = ConstraintLimiter.GetObjectiveXAcc(floors, false);

				if (double.IsNaN(record.XAccuracy))
					record.XAccuracy = 0;

				var totalFloors = Math.Max(0, floors.Count - 1);
				record.TotalFloors = totalFloors;

				LevelPlayRecords.Add(record);

				TotalHitMargins += record.HitMargins;
				TotalXAccuracy += record.XAccuracy;
				TotalFloors += totalFloors;

				LogTools.Log($"stored; now there are {LevelPlayRecords.Count} records");
			}

			public static void ProgressStateToNextLevel() {
				LogTools.Log($"Progressing state to next: {LevelIndex + 1}");

				StoreRecord();
				LevelIndex++;
				scrController.instance.mistakesManager.Reset();
			}

			public static void TerminateCourse() {
				// SceneManager.UnloadSceneAsync("scnGame");
				PlayingCourse = false;
			}
		}

		[HarmonyPatch(typeof(scnGame), "Awake")]
		private static class PreventDefaultLevel {
			private static readonly FieldInfo Wtf = AccessTools.Field(typeof(GCS), nameof(GCS.internalLevelName));

			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				var result = new List<CodeInstruction>();

				foreach (var instruction in instructions) {
					if (instruction.StoresField(Wtf)) {
						result.RemoveAt(result.Count - 1);
						continue;
					}

					result.Add(instruction);
				}

				return result;
			}
		}

		[HarmonyPatch(typeof(scnGame), "Update")]
		private static class OverrideLoadAndPlayLevel {
			private static readonly MethodInfo SkipTarget = AccessTools.Method(typeof(scnGame), "LoadAndPlayLevel");

			private static bool DoThisInstead(scnGame instance, string name) {
				if (CourseState.PlayingCourse)
					return false;

				return instance.LoadAndPlayLevel(name);
				// scnGame.instance.LoadAndPlayLevel(GCS.internalLevelName ?? GCS.customLevelPaths[GCS.customLevelIndex]);
			}

			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				foreach (var instruction in instructions) {
					if (instruction.Calls(SkipTarget)) {
						yield return new CodeInstruction(OpCodes.Call,
							AccessTools.Method(typeof(OverrideLoadAndPlayLevel), nameof(DoThisInstead)));
						continue;
					}

					yield return instruction;
				}
			}
		}

		[HarmonyPatch(typeof(scnLevelSelect), "Update")]
		private static class EnterCourseScene {
			private static void Postfix() {
				if (RDInput.holdingControl && RDInput.holdingShift && Input.GetKeyDown(KeyCode.Comma))
					SceneManager.LoadScene(CourseSelectScene.SCENE_NAME);
			}
		}

		// TODO this is probably redundant
		[HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
		private static class QuitToMainMenu {
			private static bool Prefix() {
				if (!CourseState.PlayingCourse)
					return true;

				scrUIController.instance.WipeToBlack(WipeDirection.StartsFromRight);
				GCS.sceneToLoad = CourseSelectScene.SCENE_NAME;
				CourseState.Reset();

				return false;
			}
		}
		
		[HarmonyPatch(typeof(scrUIController), "Awake")]
		public static class OverrideDifficulty {
			private static void Postfix() {
				if (!CourseState.PlayingCourse) return;

				GCS.difficulty = Difficulty.Strict;
			}
		}

		[HarmonyPatch(typeof(scnGame), "Awake")]
		public static class SetupGameSceneParameters {
			private static void Postfix() {
				if (!CourseState.PlayingCourse) return;

				CourseState.PauseStateFromPatch = false;

				var settings = ModDataStorage.PlayerSettings;
				GCS.useNoFail = settings.UseNoFail;

#if DEBUG
				GCS.useNoFail &= !ModDataStorage.PlayerSettings.DebugSettings.DisableNoFail;
#endif

				GCS.difficulty = Difficulty.Strict;
				GCS.speedTrialMode = false;
				GCS.nextSpeedRun = 1f;
				GCS.checkpointNum = 0;

				var uiController = scrUIController.instance;
				if (!uiController) return;

				uiController.noFailImage.enabled = false;
			}

			public static void LoadLevel(string levelAbsPath, [CanBeNull] Action callback) {
				LogTools.Log($"Loading level at '{levelAbsPath}'");
				StartLevelAfterTwoFrames.ConsumableAction = callback;

				var game = scnGame.instance;
				game.ResetScene(true);

				scrController.instance.paused = false;
				GCS.customLevelPaths = new[] { levelAbsPath };

				game.StartCoroutine(LoadAndPlayAfterTwoFramesDelay());
			}

			private static IEnumerator LoadAndPlayAfterTwoFramesDelay() {
				yield return null;
				yield return null;
				scnGame.instance.LoadAndPlayLevel(GCS.customLevelPaths[0]); // the rest are done with patches
				
				CourseState.WonState = false;
				CourseState.FailState = false;
			}
		}

		[HarmonyPatch(typeof(scrPressToStart), "ShowText")]
		private static class SuppressPressAnyKeyText {
			private static bool Prefix() {
				if (!CourseState.PlayingCourse)
					return true;

				StartLevelAfterTwoFrames.CalledFrames = 0;

				PauseControl.SetPaused(scrController.instance,
					false); // maybe there's a case where controller is disabled?
				LogTools.Log("CalledFrames has reset!");
				return false;
			}
		}

		[HarmonyPatch(typeof(scrController), "Update")]
		private static class StartLevelAfterTwoFrames {
			public static int? CalledFrames;
			[CanBeNull] public static Action ConsumableAction;

			private static void Postfix(scrController __instance) {
				if (!CourseState.PlayingCourse)
					return;

				if (CalledFrames is null)
					return;

				if (ConsumableAction != null) {
					ConsumableAction.Invoke();
					ConsumableAction = null;
				}

				CalledFrames++;

				if (CalledFrames <= 2)
					return;

				if (CourseState.PauseStateFromPatch)
					return;

				if (CourseState.PauseRequested)
					return;

				LogTools.Log("Begin the level immediately!");

				PauseControl.SetPaused(__instance, false);
				__instance.levelWasSkipped = true;
				CourseLevelCompleteUpdate.WonTime = null;
			}
		}

		[HarmonyPatch(typeof(scrController), "ValidInputWasTriggered")]
		private static class PreventLevelStartUntilCompletingCountdownTransition {
			private static bool Prefix(ref bool __result) =>
				__result = !CourseState.PlayingCourse || !CourseState.PauseRequested;
		}

		[HarmonyPatch(typeof(CourseTransitionScene), "Update")]
		private static class HoldEscapeToEscape {
			private static float _escapeHeldTime;

			private static void Postfix() {
				if (Input.GetKey(KeyCode.Escape))
					_escapeHeldTime += Time.unscaledDeltaTime;

				else _escapeHeldTime = 0;


				if (_escapeHeldTime >= 3) {
					CourseState.Reset();
					SceneManager.LoadScene(GCNS.sceneLevelSelect);
					_escapeHeldTime = 0;
				}
			}
		}

		[HarmonyPatch(typeof(scrPressToStart), "HideText")]
		private static class RollbackControllerChanges {
			private static void Postfix() {
				if (!CourseState.PlayingCourse)
					return;

				if (!StartLevelAfterTwoFrames.CalledFrames.HasValue)
					return;

				var controller = scrController.instance;
				if (!controller)
					return;

				StartLevelAfterTwoFrames.CalledFrames = null;
				controller.levelWasSkipped = false;

				LogTools.Log("level begin flag reset");
			}
		}

		[HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
		private static class ConstraintLimiter {
			private static void Postfix(scrMistakesManager __instance, HitMargin hit) {
				if (!CourseState.PlayingCourse)
					return;

				if (CourseState.SelectedCourse is not { } course)
					return;

				var settings = course.Settings;
				var level = course.Levels[CourseState.LevelIndex];

				if (settings.AccuracyConstraint is { } accConstraint) {
					if (!level.DisableAccuracyConstraint) {
						var maxXAcc = GetObjectiveXAcc(__instance.lm.listFloors, true);
						var failCourse = maxXAcc < accConstraint;

						if (failCourse) {
							LogTools.Log(
								$"FAIL COURSE because max possible acc is {maxXAcc} while the constraint is {accConstraint}");
							FailCourse(CourseState.FailReason.Accuracy);
						}

						UpdateSidebarConstraintChip(CourseState.FailReason.Accuracy, failCourse, maxXAcc);
					}
				}

				if (!level.DisableDeathConstraint && Deaths.Contains(hit)) {
					if (CourseState.DeathsLeft is not null) {
						if (--CourseState.DeathsLeft <= 0) {
							LogTools.Log("FAIL COURSE because no allowed deaths left");
							FailCourse(CourseState.FailReason.Death);
						}

						LogTools.Log($"DEATH set to {CourseState.DeathsLeft}");
						UpdateSidebarConstraintChip(CourseState.FailReason.Death);
					}
				}

				if (!level.DisableLifeConstraint && !Perfects.Contains(hit)) {
					if (CourseState.LivesLeft is not null) {
						if (--CourseState.LivesLeft <= 0) {
							LogTools.Log("FAIL COURSE because no allowed lives left");
							FailCourse(CourseState.FailReason.Life);
						}

						LogTools.Log($"LIFE set to {CourseState.LivesLeft}");
						UpdateSidebarConstraintChip(CourseState.FailReason.Life);
					}
				}

				return;

				void UpdateSidebarConstraintChip(CourseState.FailReason chipType, bool flash = true,
					double? currentMaxPossibleXAcc = null) {
					if (!Instance.EnableSidebarMenuOnGameScene)
						return;

					Instance.UpdateConstraintChip(chipType, flash, currentMaxPossibleXAcc);
				}
			}

			private static readonly HitMargin[] Perfects = { HitMargin.Perfect, HitMargin.Auto, };

			private static readonly HitMargin[] SemiPerfects = { HitMargin.EarlyPerfect, HitMargin.LatePerfect, };

			private static readonly HitMargin[] Bares = { HitMargin.VeryEarly, HitMargin.VeryLate, };

			private static readonly HitMargin[] Misses = { HitMargin.TooEarly, HitMargin.TooLate, };

			private static readonly HitMargin[] Deaths = { HitMargin.FailMiss, HitMargin.FailOverload, };

			private static int _lastFailedFrame;

			private static int GetHitCount(int[] marginCount, HitMargin[] margins) {
				var count = 0;
				foreach (var margin in margins) {
					if (HitMarginTools.TryGetHitMarginCount(marginCount, margin, out var c))
						count += c;
				}

				return count;
			}

			private static int GetHitCount(int[] marginCount, HitMargin margin) {
				if (HitMarginTools.TryGetHitMarginCount(marginCount, margin, out var c))
					return c;

				return 0;
			}

			public static double GetObjectiveXAcc(List<scrFloor> floors, bool maxPossible) {
				var floorsCount = Math.Max(0, floors.Count - 1);
				var hitMarginsCount = scrMistakesManager.hitMarginsCount;

				var perfects = GetHitCount(hitMarginsCount, Perfects);
				var semiPerfects = GetHitCount(hitMarginsCount, SemiPerfects);
				var bares = GetHitCount(hitMarginsCount, Bares);
				var misses = GetHitCount(hitMarginsCount, Misses);

				var failMisses = GetHitCount(hitMarginsCount, HitMargin.FailMiss);
				var failOverloads = GetHitCount(hitMarginsCount, HitMargin.FailOverload);

				var leftovers = floorsCount - (perfects + semiPerfects + bares + failMisses);
				var divisor = floorsCount + misses + failOverloads;

				// to get max possible: weighted (+ leftovers) / (divisor = total floors + misses + deaths)
				// to get normalized: weighted / (divisor)

				if (maxPossible)
					perfects += leftovers;

				var rawResult = (perfects
				                 + semiPerfects * .75
				                 + bares * .4
				                 + misses * .2) / Math.Max(1, divisor);

				return Math.Clamp(0, rawResult, 1);
			}

			private static void FailCourse(CourseState.FailReason reason) {
				CourseState.FailReasons.Add(reason);

				if (_lastFailedFrame == Time.frameCount)
					return;

				_lastFailedFrame = Time.frameCount;

				var controller = scrController.instance;
				if (!controller)
					return;

				// TODO register text and change countdown text
				controller.FailAction(false, false, "", true);
				CourseFailUpdate.DesiredFailText = I18N.Get($"general-fail-{reason}");
				CourseState.FailState = true;
			}
		}

		[HarmonyPatch(typeof(scrController), "Fail2_Update")]
		private static class CourseFailUpdate {
			public static bool DisplayedEndScreen;
			public static string DesiredFailText;

			private static bool Prefix(scrController __instance) {
				if (!CourseState.PlayingCourse)
					return true;

				Update(__instance);
				return false;
			}

			private static void Update(scrController controller) {
				if (!DesiredFailText.IsNullOrEmpty()) {
					var targetText = controller.txtCongrats;

					targetText.gameObject.SetActive(true);
					targetText.text = DesiredFailText;
				}

				if (!controller.ValidInputWasTriggered() || scrUIController.instance.isWipingToBlack)
					return;

				ShowEndScreen();
			}

			public static void ShowEndScreen() {
				if (DisplayedEndScreen)
					return;

				CourseState.StoreRecord();
				Instance.ShowEndScreen();
				DisplayedEndScreen = true;
			}
		}

		[HarmonyPatch(typeof(scrController), "Won_Update")]
		private static class CourseLevelCompleteUpdate {
			public static float? WonTime;

			private static bool Prefix() {
				if (!CourseState.PlayingCourse)
					return true;

				Update();

				return false;
			}

			private static void Update() {
				if (CourseState.LevelIndex != CourseState.PlayStartedLevelIndex)
					return;

				WonTime ??= Time.unscaledTime;
				var secondsSinceWon = Time.unscaledTime - WonTime.Value;
				if (secondsSinceWon < 1)
					return;

				if (!RDInput.mainPress && (secondsSinceWon < 3 && CourseState.LevelIndex != CourseState.TotalLevels - 1))
					return;

				WonTime = float.PositiveInfinity;
				CourseState.ProgressStateToNextLevel();

				if (CourseState.LevelIndex >= CourseState.SelectedCourse!.Value.Levels.Count)
					Instance.ShowEndScreen();
				else
					Instance.ShowCountdown();
			}
		}

		[HarmonyPatch(typeof(scnGame), "Update")]
		private static class EscapeCourse {
			private static void Postfix() {
				if (!CourseState.PlayingCourse)
					return;

				if (CourseState.WonState)
					return;

				if (Input.GetKeyDown(KeyCode.Escape)) {
					PauseControl.SetPaused(scrController.instance, true);
					CourseFailUpdate.ShowEndScreen();
				}
			}
		}

		[HarmonyPatch(typeof(scrController), "TogglePauseGame")]
		private static class PauseControl {
			private static bool Prefix() => !CourseState.PlayingCourse;

			public static void SetPaused(scrController controller, bool pauseState) {
				controller.paused = pauseState;
				controller.audioPaused = pauseState;
				controller.enabled = !pauseState;
				// Time.timeScale = pauseState ? 0 : 1;

				CourseState.PauseStateFromPatch = pauseState;

				if (!pauseState) return;

				var vfxPlus = scrVfxPlus.instance;
				if (vfxPlus) {
					var videoBg = vfxPlus.videoBG;
					if (videoBg) videoBg.Stop();
				}
			}
		}

		// TODO this is probably redundant
		[HarmonyPatch(typeof(scrController), "ResetCustomLevel")]
		private static class CheckRestart {
			private static bool Prefix(scrController __instance, ref IEnumerator __result) {
				if (!CourseState.PlayingCourse)
					return true;

				if (CourseState.LevelIndex != 0) {
					CourseState.ResetProgress();
					Instance.ProceedToLevel();

					__result = new EmptyEnumerator();
					return false;
				}

				return true;
			}

			private class EmptyEnumerator : IEnumerator {
				public bool MoveNext() => false;

				public void Reset() {
				}

				public object Current => null;
			}
		}

		[HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
		private static class MarkSuccess {
			private static void Postfix(scrController __instance) {
				if (!__instance.gameworld)
					return;

				if (!CourseState.PlayingCourse)
					return;

				if (CourseState.SelectedCourse is not { } course)
					return;

				CourseLevelCompleteUpdate.WonTime = null;
				CourseState.WonState = true;

				if (course.Levels.Count <= CourseState.LevelIndex + 1)
					CourseState.Failed = false;
			}
		}

		[HarmonyPatch(typeof(Persistence), "set_language")]
		private static class SetupI18NOnSetLanguage {
			private static void Postfix() => I18N.SetupLanguage();
		}
	}
}