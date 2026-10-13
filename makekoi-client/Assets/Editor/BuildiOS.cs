using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MakekoiEditor
{
    /// <summary>
    /// CI/CLI 用の iOS Xcode プロジェクト出力。
    /// 環境変数で上書き可能: BUILD_OUTPUT / BUNDLE_ID / TEAM_ID / PROVISIONING_PROFILE_ID
    /// </summary>
    public static class BuildiOS
    {
        [MenuItem("Build/iOS (Xcode Project)")]
        public static void Build()
        {
            var outputPath = Env("BUILD_OUTPUT", Path.Combine(Directory.GetCurrentDirectory(), "Builds/iOS"));
            var bundleId = Env("BUNDLE_ID", null);
            var teamId = Env("TEAM_ID", null);
            var profileId = Env("PROVISIONING_PROFILE_ID", null);

            // SDK=simulator でシミュレータ向けXcodeプロジェクトを出力する
            PlayerSettings.iOS.sdkVersion = Env("SDK", "device") == "simulator"
                ? iOSSdkVersion.SimulatorSDK
                : iOSSdkVersion.DeviceSDK;

            if (!string.IsNullOrEmpty(bundleId))
            {
                PlayerSettings.SetApplicationIdentifier(
                    UnityEditor.Build.NamedBuildTarget.iOS, bundleId);
            }

            if (!string.IsNullOrEmpty(teamId))
            {
                PlayerSettings.iOS.appleDeveloperTeamID = teamId;
            }

            if (!string.IsNullOrEmpty(profileId))
            {
                PlayerSettings.iOS.appleEnableAutomaticSigning = false;
                PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Distribution;
                PlayerSettings.iOS.iOSManualProvisioningProfileID = profileId;
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
            Debug.Log($"[BuildiOS] scenes: {string.Join(", ", scenes)}");

            Directory.CreateDirectory(outputPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[BuildiOS] result={summary.result} output={summary.outputPath} " +
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
