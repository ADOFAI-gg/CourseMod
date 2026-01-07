using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace CourseMod.Editor {
	[ScriptedImporter(1, "ftl")]
	public class FtlImporter : ScriptedImporter {
		public override void OnImportAsset(AssetImportContext ctx) {
			var content = File.ReadAllText(ctx.assetPath);
			var result = new TextAsset(content);

			ctx.AddObjectToAsset("TextAsset", result);
			ctx.SetMainObject(result);
		}
	}
}