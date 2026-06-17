using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CourseMod.Components.Scenes;
using CourseMod.DataModel;
using CourseMod.Player;
using CourseMod.Utils;
using DG.Tweening;
using HarmonyLib;
using JetBrains.Annotations;
using MonsterLove.StateMachine;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CourseMod.Patches {
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class GameplayPatches {
		private static CourseTransitionScene Instance => CourseTransitionScene.Instance ? CourseTransitionScene.Instance : null;

		[HarmonyPatch(typeof(scnLevelSelect), "Update")]
		private static class EnterCourseScene {
			private static void Postfix() {
				if (RDInput.holdingControl && RDInput.holdingShift && Input.GetKeyDown(KeyCode.Comma))
					SceneManager.LoadScene(CourseSelectScene.SCENE_NAME);
			}
		}

		// public static class CourseState123dsgrayiurtasyurdgdqwredqwtyuxrfahdcg7u6rs5ga6y7drfbayst {
		// 	public enum FailReason {
		// 		Accuracy,
		// 		Death,
		// 		Life,
		// 	}
		//
		// 	public static bool PlayingCourse;
		//
		// 	private static Course? _selectedCourse;
		//
		// 	public static Course? SelectedCourse {
		// 		get => _selectedCourse;
		// 		set {
		// 			_selectedCourse = value;
		//
		// 			if (value is { } course) {
		// 				TotalLevels = course.Levels.Count;
		// 			} else {
		// 				TotalLevels = 0;
		// 			}
		// 		}
		// 	}
		//
		// 	public static int TotalLevels;
		//
		// 	public static int LevelIndex;
		// 	public static int PlayStartedLevelIndex;
		//
		// 	public static SerializableHitMargins TotalHitMargins = new();
		// 	public static int TotalFloors;
		//
		// 	public static double TotalXAccuracy;
		// 	public static int? DeathsLeft;
		// 	public static int? LivesLeft;
		//
		// 	public static bool Failed = true;
		// 	public static bool PauseStateFromPatch;
		// 	public static bool PauseRequested;
		// 	public static bool WonState;
		// 	public static bool FailState;
		//
		// 	public static LevelPlayer BoundLevelPlayer;
		//
		// 	public static readonly List<CourseLevelPlayRecord> LevelPlayRecords = new();
		// 	public static readonly HashSet<FailReason> FailReasons = new();
		//
		// 	public static void Reset() {
		// 		TerminateCourse();
		//
		// 		SelectedCourse = null;
		// 		TotalLevels = 0;
		//
		// 		ResetProgress();
		// 	}
		//
		// 	public static void ResetProgress() {
		// 		LogTools.Log("Resetting course state progress");
		//
		// 		LevelIndex = 0;
		// 		PlayStartedLevelIndex = 0;
		//
		// 		TotalFloors = 0;
		// 		TotalHitMargins = new();
		// 		TotalXAccuracy = 0;
		//
		// 		DeathsLeft = SelectedCourse?.Settings.DeathConstraint;
		// 		LivesLeft = SelectedCourse?.Settings.LifeConstraint;
		//
		// 		Failed = true;
		// 		PauseStateFromPatch = false;
		// 		PauseRequested = false;
		//
		// 		WonState = false;
		// 		FailState = false;
		//
		// 		LevelPlayRecords.Clear();
		// 		FailReasons.Clear();
		//
		// 		BoundLevelPlayer = null;
		//
		// 		CourseFailUpdate.DisplayedEndScreen = false;
		// 		CourseLevelCompleteUpdate.WonTime = null;
		// 	}
		//
		// 	public static CourseLevelPlayRecord DefaultPlayRecord =>
		// 		new() {
		// 			CourseChecksum = SelectedCourse.HasValue
		// 				? ChecksumTools.ComputeCourseChecksum(SelectedCourse.Value).Content
		// 				: string.Empty,
		// 			GameplayChecksum = string.Empty,
		// 			HitMargins = SerializableHitMargins.Default,
		// 			LevelNumber = 0,
		// 			XAccuracy = 0,
		// 			TotalFloors = 0
		// 		};
		//
		// 	// public static void SaveRecord() {
		// 	// 	if (SelectedCourse is not { } course)
		// 	// 		return;
		// 	//
		// 	// 	var json = JsonConvert.SerializeObject(new CoursePlayRecord { Records = LevelPlayRecords.ToArray() });
		// 	// 	File.WriteAllText(course.GetPlayRecordPath(), json);
		// 	// }
		//
		// 	public static void StoreRecord() {
		// 		var levelNumber = LevelIndex + 1;
		//
		// 		if (LevelPlayRecords.LastOrDefault(r => r.LevelNumber == levelNumber) != null)
		// 			return;
		//
		// 		var controller = scrController.instance;
		//
		// 		var tracker = controller.mistakesManager;
		// 		var hitMarginsCount = scrMistakesManager.hitMarginsCount;
		// 		var floors = tracker.lm.listFloors;
		//
		// 		var record = DefaultPlayRecord;
		//
		// 		record.LevelNumber = levelNumber;
		// 		record.GameplayChecksum = ChecksumTools.ComputeGameplayChecksum(tracker.customLevel.levelData).Hash;
		//
		// 		record.HitMargins =
		// 			SerializableHitMargins.FromHitMarginsCount(hitMarginsCount);
		// 		record.XAccuracy = ConstraintLimiter.GetObjectiveXAcc(floors, false);
		//
		// 		if (double.IsNaN(record.XAccuracy))
		// 			record.XAccuracy = 0;
		//
		// 		var totalFloors = Math.Max(0, floors.Count - 1);
		// 		record.TotalFloors = totalFloors;
		//
		// 		LevelPlayRecords.Add(record);
		//
		// 		TotalHitMargins += record.HitMargins;
		// 		TotalXAccuracy += record.XAccuracy;
		// 		TotalFloors += totalFloors;
		//
		// 		LogTools.Log($"stored; now there are {LevelPlayRecords.Count} records");
		// 	}
		//
		// 	public static void ProgressStateToNextLevel() {
		// 		LogTools.Log($"Progressing state to next: {LevelIndex + 1}");
		//
		// 		StoreRecord();
		// 		LevelIndex++;
		// 		scrController.instance.mistakesManager.Reset();
		// 	}
		//
		// 	public static void TerminateCourse() {
		// 		// SceneManager.UnloadSceneAsync("scnGame");
		// 		PlayingCourse = false;
		// 	}
		// }

		public static CoursePlayer CurrentCoursePlayer;

		public static void RequestLevelStart() => PatchStateStore.PauseRequested = false;
		public static void RequestLevelPause() => PatchStateStore.PauseRequested = true;

		public static void KillPlayer(string deathReason) {
			// ignore non-custom level environment
			if (!scnGame.instance)
				return;

			// ignore environments unrelated to course
			if (CurrentCoursePlayer == null)
				return;

			var controller = scrController.instance;
			if (!controller) return;

			controller.FailAction(false, false, "", true);
			LogTools.Log("Killed planetary system");

			if (!string.IsNullOrEmpty(deathReason))
				CourseFailUpdate.DesiredFailText = deathReason;
		}

		private static class PatchStateStore {
			public static bool Pause;
			public static bool PauseRequested;

			public static void Reset() {
				Pause = PauseRequested = false;
				
				EscapeCourse.PreventEscape = false;
				CourseFailUpdate.DisplayedEndScreen = false;
				LogTools.Log($"DisplayedEndScreen = false");
			}
		}

		private static void ResetEverything() {
			CurrentCoursePlayer = null;
			PatchStateStore.Reset();
		}

		[HarmonyPatch(typeof(scnGame), "Awake")]
		public static class SetupGameSceneParameters {
			public static void LoadLevel(LevelPlayer player, [CanBeNull] Action callback) {
				var absPath = player.Level.AbsolutePath;
				
				LogTools.Log($"Loading level at '{absPath}'");
				StartLevelAfterTwoFrames.ConsumableAction = callback;
				CourseFailUpdate.DisplayedEndScreen = false;
				LogTools.Log($"DisplayedEndScreen = false");

				ControllerFail2StateChangeActionOverrider.ExplodeTween?.Kill();

				var game = scnGame.instance;
				game.ResetScene(true);

				scrController.instance.paused = false;
				GCS.customLevelPaths = new[] { absPath };

				game.StartCoroutine(LoadAndPlayAfterTwoFramesDelay());
			}

			private static IEnumerator LoadAndPlayAfterTwoFramesDelay() {
				yield return null;
				yield return null;
				scnGame.instance.LoadAndPlayLevel(GCS.customLevelPaths[0]); // the rest are done with patches

				EscapeCourse.PreventEscape = false;
			}

			private static void Postfix() {
				PatchStateStore.Pause = false;
				CourseFailUpdate.DisplayedEndScreen = false;
				LogTools.Log($"DisplayedEndScreen = false");

				var settings = ModDataStorage.PlayerSettings;
				GCS.useNoFail = settings.UseNoFail;

#if DEBUG
				GCS.useNoFail &= !ModDataStorage.PlayerSettings.DebugSettings.DisableNoFail;
#endif

				GCS.difficulty = Difficulty.Strict;
				GCS.speedTrialMode = false;
				GCS.nextSpeedRun = 1f;
				GCS.checkpointNum = 0;
				
				LogTools.Log("GCS parameters ready");

				var uiController = scrUIController.instance;
				if (!uiController) return;

				uiController.noFailImage.enabled = false;
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
				if (CurrentCoursePlayer != null)
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

		// TODO this is probably redundant
		[HarmonyPatch(typeof(scrController), "QuitToMainMenu")]
		private static class QuitToMainMenu {
			private static bool Prefix() {
				if (CurrentCoursePlayer == null)
					return true;

				scrUIController.instance.WipeToBlack(WipeDirection.StartsFromRight);
				GCS.sceneToLoad = CourseSelectScene.SCENE_NAME;

				ResetEverything();

				return false;
			}
		}

		[HarmonyPatch(typeof(scrUIController), "Awake")]
		private static class OverrideDifficulty {
			private static void Postfix() {
				if (CurrentCoursePlayer == null) return;

				GCS.difficulty = Difficulty.Strict;
			}
		}

		// TODO this patch is advised to be reworked after r135
		[HarmonyPatch(typeof(PlanetarySystem), "Die")]
		private static class ControllerFail2StateChangeActionOverrider {
			public static Tween ExplodeTween;

			private static readonly MethodInfo SkipTarget = AccessTools.Method(typeof(DOVirtual), "DelayedCall");

			private static Tween DoThisInstead(float delay, TweenCallback callback, bool ignoreTimeScale = true) {
				if (CurrentCoursePlayer != null)
					return null;

				return ExplodeTween = DOVirtual.DelayedCall(delay, callback, ignoreTimeScale);
			}

			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
				foreach (var instruction in instructions) {
					if (instruction.Calls(SkipTarget)) {
						yield return new CodeInstruction(OpCodes.Call,
							AccessTools.Method(typeof(ControllerFail2StateChangeActionOverrider),
								nameof(DoThisInstead)));
						continue;
					}

					yield return instruction;
				}
			}
		}

		[HarmonyPatch(typeof(scrPressToStart), "ShowText")]
		private static class SuppressPressAnyKeyText {
			private static bool Prefix() {
				if (CurrentCoursePlayer == null)
					return true;

				StartLevelAfterTwoFrames.CalledFrames = 0;

				RevivePlanets();

				PauseControl.SetPaused(scrController.instance,
					false); // maybe there's a case where controller is disabled?
				LogTools.Log("CalledFrames has reset!");
				return false;
			}

			private static void RevivePlanets() {
				var controller = scrController.instance;
				var system = controller.planetarySystem;

				for (var i = 0; i < system.planetsUsed && i < system.allPlanets.Count; i++) {
					LogTools.Log($"check planet {i}");

					var planet = system.allPlanets[i];
					if (planet.dead) {
						LogTools.Log($"planet {i} rewind");
						planet.Rewind();
					}
				}
			}
		}

		[HarmonyPatch(typeof(scrController), "Update")]
		private static class StartLevelAfterTwoFrames {
			public static int? CalledFrames;
			[CanBeNull] public static Action ConsumableAction;

			private static void Postfix(scrController __instance) {
				if (CurrentCoursePlayer == null)
					return;

				if (CalledFrames is null)
					return;

				if (ConsumableAction != null) {
					ConsumableAction.Invoke();
					ConsumableAction = null;

					LogTools.Log("Consumed callback");
				}

				CalledFrames++;

				if (CalledFrames <= 2)
					return;

				if (PatchStateStore.Pause)
					return;

				if (PatchStateStore.PauseRequested)
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
				__result = CurrentCoursePlayer == null || !PatchStateStore.PauseRequested;
		}

		[HarmonyPatch(typeof(CourseTransitionScene), "Update")]
		private static class HoldEscapeToEscape {
			private static float _escapeHeldTime;

			private static void Postfix() {
				if (Input.GetKey(KeyCode.Escape))
					_escapeHeldTime += Time.unscaledDeltaTime;

				else _escapeHeldTime = 0;


				if (_escapeHeldTime >= 3) {
					ResetEverything();
					SceneManager.LoadScene(GCNS.sceneLevelSelect);
					_escapeHeldTime = 0;
				}
			}
		}

		[HarmonyPatch(typeof(scrPressToStart), "HideText")]
		private static class RollbackControllerChanges {
			private static void Postfix() {
				if (CurrentCoursePlayer == null)
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
		private static class ConstraintCheckerHook {
			private static void Postfix(scrMistakesManager __instance, HitMargin hit) {
				if (CurrentCoursePlayer == null) return;

				var payload = new LevelProgressFromPatch() {
					CurrentFloor = __instance.controller.currFloor.seqID,
					CurrentHitMargin = hit,
					HitMarginsCount = scrMistakesManager.hitMarginsCount,
				};

				CurrentCoursePlayer.CurrentLevelPlayer.CurrentValue?.Stats.ScoreUpdated.OnNext(payload);
			}
		}


		// [HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
		// private static class ConstraintLimiter {
		// 	private static void Postfix(scrMistakesManager __instance, HitMargin hit) {
		// 		if (CurrentCoursePlayer == null)
		// 			return;
		//
		// 		var course = CurrentCoursePlayer.Course;
		//
		// 		var settings = course.Settings;
		// 		var level = course.Levels[CurrentCoursePlayer.Index.Value];
		//
		// 		if (settings.AccuracyConstraint is { } accConstraint) {
		// 			if (!level.DisableAccuracyConstraint) {
		// 				var maxXAcc = GetObjectiveXAcc(__instance.lm.listFloors, true);
		// 				var failCourse = maxXAcc < accConstraint;
		//
		// 				if (failCourse) {
		// 					LogTools.Log(
		// 						$"FAIL COURSE because max possible acc is {maxXAcc} while the constraint is {accConstraint}");
		// 					FailCourse(CourseState.FailReason.Accuracy);
		// 				}
		//
		// 				UpdateSidebarConstraintChip(CourseState.FailReason.Accuracy, failCourse, maxXAcc);
		// 			}
		// 		}
		//
		// 		if (!level.DisableDeathConstraint && Deaths.Contains(hit)) {
		// 			if (CourseState.DeathsLeft is not null) {
		// 				if (--CourseState.DeathsLeft <= 0) {
		// 					LogTools.Log("FAIL COURSE because no allowed deaths left");
		// 					FailCourse(CourseState.FailReason.Death);
		// 				}
		//
		// 				LogTools.Log($"DEATH set to {CourseState.DeathsLeft}");
		// 				UpdateSidebarConstraintChip(CourseState.FailReason.Death);
		// 			}
		// 		}
		//
		// 		if (!level.DisableLifeConstraint && !Perfects.Contains(hit)) {
		// 			if (CourseState.LivesLeft is not null) {
		// 				if (--CourseState.LivesLeft <= 0) {
		// 					LogTools.Log("FAIL COURSE because no allowed lives left");
		// 					FailCourse(CourseState.FailReason.Life);
		// 				}
		//
		// 				LogTools.Log($"LIFE set to {CourseState.LivesLeft}");
		// 				UpdateSidebarConstraintChip(CourseState.FailReason.Life);
		// 			}
		// 		}
		//
		// 		return;
		//
		// 		void UpdateSidebarConstraintChip(CourseState.FailReason chipType, bool flash = true,
		// 			double? currentMaxPossibleXAcc = null) {
		// 			if (!Instance.EnableSidebarMenuOnGameScene)
		// 				return;
		//
		// 			Instance.UpdateConstraintChip(chipType, flash, currentMaxPossibleXAcc);
		// 		}
		// 	}
		//
		// 	private static readonly HitMargin[] Perfects = { HitMargin.Perfect, HitMargin.Auto, };
		//
		// 	private static readonly HitMargin[] SemiPerfects = { HitMargin.EarlyPerfect, HitMargin.LatePerfect, };
		//
		// 	private static readonly HitMargin[] Bares = { HitMargin.VeryEarly, HitMargin.VeryLate, };
		//
		// 	private static readonly HitMargin[] Misses = { HitMargin.TooEarly, HitMargin.TooLate, };
		//
		// 	private static readonly HitMargin[] Deaths = { HitMargin.FailMiss, HitMargin.FailOverload, };
		//
		// 	private static int _lastFailedFrame;
		//
		// 	private static int GetHitCount(int[] marginCount, HitMargin[] margins) {
		// 		var count = 0;
		// 		foreach (var margin in margins) {
		// 			if (HitMarginTools.TryGetHitMarginCount(marginCount, margin, out var c))
		// 				count += c;
		// 		}
		//
		// 		return count;
		// 	}
		//
		// 	private static int GetHitCount(int[] marginCount, HitMargin margin) {
		// 		if (HitMarginTools.TryGetHitMarginCount(marginCount, margin, out var c))
		// 			return c;
		//
		// 		return 0;
		// 	}
		//
		// 	public static double GetObjectiveXAcc(List<scrFloor> floors, bool maxPossible) {
		// 		var floorsCount = Math.Max(0, floors.Count - 1);
		// 		var hitMarginsCount = scrMistakesManager.hitMarginsCount;
		//
		// 		var perfects = GetHitCount(hitMarginsCount, Perfects);
		// 		var semiPerfects = GetHitCount(hitMarginsCount, SemiPerfects);
		// 		var bares = GetHitCount(hitMarginsCount, Bares);
		// 		var misses = GetHitCount(hitMarginsCount, Misses);
		//
		// 		var failMisses = GetHitCount(hitMarginsCount, HitMargin.FailMiss);
		// 		var failOverloads = GetHitCount(hitMarginsCount, HitMargin.FailOverload);
		//
		// 		var leftovers = floorsCount - (perfects + semiPerfects + bares + failMisses);
		// 		var divisor = floorsCount + misses + failOverloads;
		//
		// 		// to get max possible: weighted (+ leftovers) / (divisor = total floors + misses + deaths)
		// 		// to get normalized: weighted / (divisor)
		//
		// 		if (maxPossible)
		// 			perfects += leftovers;
		//
		// 		var rawResult = (perfects
		// 		                 + semiPerfects * .75
		// 		                 + bares * .4
		// 		                 + misses * .2) / Math.Max(1, divisor);
		//
		// 		return Math.Clamp(0, rawResult, 1);
		// 	}
		//
		// 	private static void FailCourse(CourseState.FailReason reason) {
		// 		CourseState.FailReasons.Add(reason);
		//
		// 		if (_lastFailedFrame == Time.frameCount)
		// 			return;
		//
		// 		_lastFailedFrame = Time.frameCount;
		//
		// 		var controller = scrController.instance;
		// 		if (!controller)
		// 			return;
		//
		// 		// TODO register text and change countdown text
		// 		controller.FailAction(false, false, "", true);
		// 		CourseFailUpdate.DesiredFailText = I18N.Get($"general-fail-{reason}");
		// 		CourseState.FailState = true;
		// 	}
		// }
		
		[HarmonyPatch(typeof(StateBehaviour), "ChangeState", typeof(Enum))]
		private static class ControllerStateTracker {
			public static States FutureState;
			
			private static void Postfix(StateBehaviour __instance, Enum newState) 
			{
				if (__instance is not scrController) return;
				if (newState is not States state) return;
				
				FutureState = state;
			}
		}
		
		[HarmonyPatch(typeof(scrController), "FailAction")]
		private static class CourseFailDetector {
			private static void Postfix() {
				if (CurrentCoursePlayer is not { Failed: false }) return;
				if (ControllerStateTracker.FutureState is not (States.Fail or States.Fail2))
					return;
				
				CurrentCoursePlayer.FailFromGameMechanics();
			}
		}

		[HarmonyPatch(typeof(scrController), "Fail2_Update")]
		private static class CourseFailUpdate {
			public static bool DisplayedEndScreen;
			public static string DesiredFailText;

			private static bool Prefix(scrController __instance) {
				if (CurrentCoursePlayer == null)
					return true;

				Update(__instance);
				return false;
			}

			public static void Update(scrController controller) {
				if (!DesiredFailText.IsNullOrEmpty()) {
					var targetText = controller.txtTryCalibrating;

					targetText.gameObject.SetActive(true);
					targetText.text = DesiredFailText;
					DesiredFailText = null;
				}

				if (!controller.ValidInputWasTriggered() || scrUIController.instance.isWipingToBlack)
					return;

				ShowEndScreen();
			}

			public static void ShowEndScreen(bool failWithIntent = false) {
				if (CurrentCoursePlayer == null)
					return;
				
				if (DisplayedEndScreen)
					return;

				if (failWithIntent)
					CurrentCoursePlayer.FailFromPlayerIntent();

				Instance?.ShowEndScreen();
				
				DisplayedEndScreen = true;
				LogTools.Log($"DisplayedEndScreen = true");
			}
		}

		[HarmonyPatch(typeof(scrController), "Update")]
		private static class CourseFailBackupUpdate {
			private static void Prefix(scrController __instance) {
				if (CurrentCoursePlayer == null)
					return;

				if (__instance.currentState != States.Fail)
					return;

				CourseFailUpdate.Update(__instance);
			}
		}

		[HarmonyPatch(typeof(scrController), "Won_Update")]
		private static class CourseLevelCompleteUpdate {
			public static float? WonTime;

			private static bool Prefix() {
				if (CurrentCoursePlayer == null)
					return true;

				Update();

				return false;
			}

			private static void Update() {
				const float NextLevelAwaitTime = 3;

				WonTime ??= Time.unscaledTime;
				var secondsSinceWon = Time.unscaledTime - WonTime.Value;
				if (secondsSinceWon < 1)
					return;

				var levelIndex = CurrentCoursePlayer.Index.Value;
				var maxLevelIndex = CurrentCoursePlayer.LevelPlayers.Length - 1;

				if (!RDInput.mainPress &&
				    (secondsSinceWon < NextLevelAwaitTime || levelIndex == maxLevelIndex))
					return;

				WonTime = float.PositiveInfinity;
				
				var currentPlayer = CurrentCoursePlayer.CurrentLevelPlayer.CurrentValue;
				currentPlayer.Complete();
				
				LogTools.Log($"Completed level player {currentPlayer.Index}");
				LogTools.Log($"new check value {currentPlayer.Index == maxLevelIndex} | existing value {CurrentCoursePlayer.IsOnLastLevel.CurrentValue}");

				if (currentPlayer.Index == maxLevelIndex)
					Instance?.ShowEndScreen();
				else
					Instance?.ShowCountdown();
			}
		}

		[HarmonyPatch(typeof(scnGame), "Update")]
		private static class EscapeCourse {
			public static bool PreventEscape;
			
			private static void Postfix() {
				if (CurrentCoursePlayer == null)
					return;

				if (PreventEscape)
					return;

				if (Input.GetKeyDown(KeyCode.Escape)) {
					PauseControl.SetPaused(scrController.instance, true);
					CourseFailUpdate.ShowEndScreen(true);
				}
			}
		}

		[HarmonyPatch(typeof(scrController), "TogglePauseGame")]
		private static class PauseControl {
			private static bool Prefix() => CurrentCoursePlayer == null;

			public static void SetPaused(scrController controller, bool pauseState) {
				controller.paused = pauseState;
				controller.audioPaused = pauseState;
				controller.enabled = !pauseState;
				// Time.timeScale = pauseState ? 0 : 1;

				PatchStateStore.Pause = pauseState;

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
			private static bool Prefix(ref IEnumerator __result) {
				if (CurrentCoursePlayer == null)
					return true;

				if (CurrentCoursePlayer.Index.Value != 0) {
					// CourseState.ResetProgress();
					Instance?.ProceedToLevel();

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

				if (CurrentCoursePlayer == null)
					return;

				CourseLevelCompleteUpdate.WonTime = null;
				EscapeCourse.PreventEscape = true;
			}
		}

		[HarmonyPatch(typeof(Persistence), "set_language")]
		private static class SetupI18NOnSetLanguage {
			private static void Postfix() => I18N.SetupLanguage();
		}
	}
}