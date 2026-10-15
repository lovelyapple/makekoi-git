using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MakekoiEditor
{
    /// <summary>
    /// CI/CLI 用の Android ビルド。
    /// 環境変数で上書き可能: BUILD_OUTPUT / SCENES / BUNDLE_ID / APP_BUNDLE
    /// 署名を指定しない場合は Unity の debug keystore が使われる。
    /// </summary>
    public static class BuildAndroid
    {
        [MenuItem("Build/Android (APK)")]
        public static void Build()
        {
            var outputPath = Env("BUILD_OUTPUT",
                Path.Combine(Directory.GetCurrentDirectory(), "Builds/Android/makekoi-client.apk"));
            var bundleId = Env("BUNDLE_ID", null);

            // APK か AAB か。既定は APK。
            EditorUserBuildSettings.buildAppBundle = Env("APP_BUNDLE", "0") == "1";

            if (!string.IsNullOrEmpty(bundleId))
            {
                PlayerSettings.SetApplicationIdentifier(
                    UnityEditor.Build.NamedBuildTarget.Android, bundleId);
            }

            // SCENES が指定されていればそれを優先（先頭が起動シーン）。
            var sceneOverride = Env("SCENES", null);
            var scenes = string.IsNullOrEmpty(sceneOverride)
                ? EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray()
                : sceneOverride.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

            if (scenes.Length == 0)
            {
                throw new Exception("ビルド対象のシーンがありません。");
            }

            foreach (var scene in scenes)
            {
                if (!File.Exists(scene))
                {
                    throw new Exception($"シーンが見つかりません: {scene}");
                }
            }
            Debug.Log($"[BuildAndroid] scenes: {string.Join(", ", scenes)}");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[BuildAndroid] result={summary.result} output={summary.outputPath} " +
                      $"errors={summary.totalErrors} time={summary.totalTime}");

            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
            EditorApplication.Exit(0);
        }

        static string Env(string key, string fallback)
        {
            var v = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(v) ? fallback : v;
        }
    }
}
