using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace CourseMod.Utils {
	public static class LogTools {
		public static void Log(object message) {
			var log = GenerateLogString(message);

#if UNITY_EDITOR
			UnityEngine.Debug.Log(log);
#else
			Entry.ModEntry.Logger.Log(log);
#endif
		}

		public static void LogWarning(object message) {
			var log = GenerateLogString(message);
			
#if UNITY_EDITOR
			UnityEngine.Debug.LogWarning(log);
#else
			Entry.ModEntry.Logger.Warning(log);
#endif
		}

		private static string GenerateLogString(object message) {
			var method = new StackFrame(2).GetMethod();
			var declaringName = method.DeclaringType == null ? "" : method.DeclaringType.Name + ".";

			return $"[{Time.frameCount} | {Thread.CurrentThread.Name ?? "Native Thread"}({Thread.CurrentThread.ManagedThreadId}) | {DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}] {declaringName}{method.Name}() -> {message}";
		}

		public static void LogException(string key, Exception e) {
			Log($"{key}: {e.GetType().Name} - {e.Message}");
			Console.WriteLine(e.ToString());
		}
	}
}