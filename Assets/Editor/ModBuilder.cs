using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CourseMod.Editor {
	public class ModBuilder {
		public bool SkipAssetBundleBuild;
		public bool DevelopmentBuild;
		public bool GenerateDebugSymbols;

		private List<string> _defines;
		private string _buildPath;

		public bool IsBuilding { get; private set; }

		public async Task<string> Build([CanBeNull] string copyDestination, bool allPlatforms, [CanBeNull] HashSet<BuildTarget> buildTargets) {
			try {
				IsBuilding = true;
				_defines = new List<string>();
				AssetDatabase.SaveAssets();
				var now = DateTime.Now - DateTime.UnixEpoch;
				_buildPath = Path.Combine("Builds", $"{Math.Round(now.TotalMilliseconds)}");
				Directory.CreateDirectory(_buildPath);

				if (DevelopmentBuild) _defines.Add("DEBUG");

				Debug.Log($"extra defines: {string.Join(", ", _defines)}");

				WriteModInfo();
				await BuildAssemblies();
				if (allPlatforms) {
					BuildAllAssetBundles();
				} else if (buildTargets == null || buildTargets.Count == 0) {
					BuildAssetBundlesForCurrentPlatform();
				} else {
					BuildAssetBundles(buildTargets);
				}

				if (copyDestination != null) {
					if (Directory.Exists(copyDestination))
						Directory.Delete(copyDestination, true);

					FileUtil.CopyFileOrDirectory(_buildPath, copyDestination);
				}

				return _buildPath;
			} catch (Exception e) {
				Debug.LogError(e);
				throw;
			} finally {
				IsBuilding = false;
			}
		}

		private async Task BuildAssemblies() {
			var names = new[] {
				"CourseMod", "UniTask", "R3.Unity", "Newtonsoft.Json", "R3", "Microsoft.Bcl.TimeProvider",
				"Microsoft.Bcl.AsyncInterfaces", "ObservableCollections", "ObservableCollections.R3",
				"System.Runtime.CompilerServices.Unsafe", "Microsoft.NET.StringTools", "System.Threading.Channels",
				"Reflex", "System.Diagnostics.DiagnosticSource", "ImGui.NET", "System.Numerics.Vectors",
				"System.Collections.Immutable", "Fluent.Net"
			};

			var namesSuffixed = names.Select(x => x + ".dll").ToList();
			var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
				.Where(x => names.Contains(x.name)).ToArray();
			// Debug.Log(string.Join(", ", CompilationPipeline.GetPrecompiledAssemblyNames()));
			var prebuilts =
				CompilationPipeline.GetPrecompiledAssemblyNames()
					.Where(x => namesSuffixed.Contains(x))
					.Select(CompilationPipeline.GetPrecompiledAssemblyPathFromAssemblyName)
					.ToList();

			foreach (var prebuilt in prebuilts) {
				var name = Path.GetFileName(prebuilt);
				File.Copy(prebuilt, Path.Combine(_buildPath, name), overwrite: true);
			}

			try {
				EditorUtility.DisplayProgressBar("building assemblies", $"Building {assemblies.Length} assemblies", 1);

				foreach (var assembly in assemblies) {
					await BuildAssembly(assembly);
				}
			} finally {
				EditorUtility.ClearProgressBar();
			}
		}

		public static void Copy(string sourceDirectory, string targetDirectory) {
			var diSource = new DirectoryInfo(sourceDirectory);
			var diTarget = new DirectoryInfo(targetDirectory);

			CopyAll(diSource, diTarget);
		}

		private static void CopyAll(DirectoryInfo source, DirectoryInfo target) {
			Directory.CreateDirectory(target.FullName);

			// Copy each file into the new directory.
			foreach (var fi in source.GetFiles()) {
				fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
			}

			// Copy each subdirectory using recursion.
			foreach (var diSourceSubDir in source.GetDirectories()) {
				var nextTargetSubDir =
					target.CreateSubdirectory(diSourceSubDir.Name);
				CopyAll(diSourceSubDir, nextTargetSubDir);
			}
		}

		private Task BuildAssembly(Assembly assembly) {
			return Task.Run(() => {
				var trees = new List<SyntaxTree>();

				var defines = assembly.defines.Concat(_defines).ToList();

				var parseOptions = new CSharpParseOptions(preprocessorSymbols: defines);

				foreach (var scriptPath in assembly.sourceFiles) {
					var txt = File.ReadAllText(scriptPath);
					var tree = CSharpSyntaxTree.ParseText(txt, parseOptions, scriptPath, Encoding.UTF8);
					trees.Add(tree);
				}

				var references = assembly.allReferences.Select(location => MetadataReference.CreateFromFile(location))
					.ToList();

				var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
						optimizationLevel: OptimizationLevel.Release)
					.WithAllowUnsafe(assembly.compilerOptions.AllowUnsafeCode)
					.WithPlatform(Platform.AnyCpu);

				var compilation = CSharpCompilation.Create(assembly.name, trees, references, options);

				if (!Directory.Exists(_buildPath)) Directory.CreateDirectory(_buildPath);

				using var dllStream = File.Create(Path.Combine(_buildPath, assembly.name + ".dll"));

				var pdbPath = Path.Combine(_buildPath, assembly.name + ".pdb");
				using var pdbStream = GenerateDebugSymbols ? File.Create(pdbPath) : null;

				var result = compilation.Emit(dllStream, pdbStream: pdbStream,
					options: new EmitOptions(pdbFilePath: GenerateDebugSymbols ? pdbPath + '\0' : null,
						debugInformationFormat: DebugInformationFormat.PortablePdb));

				foreach (var resultDiagnostic in result.Diagnostics) {
					switch (resultDiagnostic.Severity) {
						case DiagnosticSeverity.Error:
							Debug.LogError(resultDiagnostic.ToString());
							break;
						case DiagnosticSeverity.Hidden:
						case DiagnosticSeverity.Info:
						default:
							break;
					}
				}

				if (!result.Success) {
					throw new Exception("compilation failed");
				}
			});
		}

		private void WriteModInfo() => ModInfo.Info.WriteToFile(Path.Combine(_buildPath, "Info.json"));

		private void BuildAssetBundlesForCurrentPlatform() {
			BuildAssetBundlesForPlatform(PlatformToBuildTarget(Application.platform));
		}

		private void BuildAssetBundles(HashSet<BuildTarget> buildTargets) {
			foreach (var target in buildTargets)
				BuildAssetBundlesForPlatform(target);
		}

		private void BuildAllAssetBundles() {
			BuildAssetBundlesForPlatform(BuildTarget.StandaloneLinux64);
			BuildAssetBundlesForPlatform(BuildTarget.StandaloneWindows64);
			BuildAssetBundlesForPlatform(BuildTarget.StandaloneOSX);
		}

		private void BuildAssetBundlesForPlatform(BuildTarget target) {
			var ns = target switch {
				BuildTarget.StandaloneWindows64 => "win",
				BuildTarget.StandaloneLinux64 => "linux",
				BuildTarget.StandaloneOSX => "mac",
				_ => throw new ArgumentOutOfRangeException(nameof(target))
			};
			var buildPath = Path.Combine("Temp", "Build", "AssetBundles", ns);
			var destDir = Path.Combine(_buildPath, ns);
			if (!Directory.Exists(buildPath))
				Directory.CreateDirectory(buildPath);
			var directoryExists = Directory.Exists(destDir);
			if (!directoryExists) Directory.CreateDirectory(destDir);

			if (!SkipAssetBundleBuild || !directoryExists)
				BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.None, target);

			var files = new[] { "course_assets.bundle", "course_scenes.bundle" };

			foreach (var file in files) {
				var source = Path.Combine(buildPath, file);
				var destination = Path.Combine(destDir, file);

				File.Copy(source, destination, true);
			}
		}

		public static BuildTarget PlatformToBuildTarget(RuntimePlatform runtimePlatform) =>
			runtimePlatform switch {
				RuntimePlatform.WindowsEditor => BuildTarget.StandaloneWindows64,
				RuntimePlatform.OSXEditor => BuildTarget.StandaloneOSX,
				RuntimePlatform.LinuxEditor => BuildTarget.StandaloneLinux64,
				_ => BuildTarget.NoTarget
			};
	}
}