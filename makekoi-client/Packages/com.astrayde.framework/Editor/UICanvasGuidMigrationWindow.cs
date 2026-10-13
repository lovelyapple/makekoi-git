using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AstraydeFramework.Editor
{
    public class UICanvasGuidMigrationWindow : EditorWindow
    {
        private string _projectRoot = "";
        private string _packageRoot = "";
        private string _oldMetaRel = "Runtime/WindowSystem/UICanvasIntializer.cs.meta";
        private string _newMetaRel = "Runtime/WindowSystem/UICanvasInitializer.cs.meta";
        private string _oldGuid = "";
        private string _newGuid = "";
        private bool _autoDetect = true;
        private bool _dryRun = true;
        private Vector2 _logScroll;
        private string _logOutput = "";

        [MenuItem("Tools/AstraydeFramework/UICanvas GUID Migration")]
        public static void Open()
        {
            GetWindow<UICanvasGuidMigrationWindow>("UICanvas GUID Migration");
        }

        private void OnEnable()
        {
            _projectRoot = FindProjectRoot();
            _packageRoot = FindPackageRoot();
        }

        private void OnGUI()
        {
            GUILayout.Label("UICanvasIntializer → UICanvasInitializer GUID Migration Tool", EditorStyles.boldLabel);
            GUILayout.Space(12);

            // Project Root
            EditorGUILayout.LabelField("Project Root", EditorStyles.boldLabel);
            EditorGUILayout.TextField(_projectRoot);

            // Package Root
            EditorGUILayout.LabelField("Package Root", EditorStyles.boldLabel);
            EditorGUILayout.TextField(_packageRoot);

            GUILayout.Space(12);

            // Auto-detect Toggle
            _autoDetect = EditorGUILayout.Toggle("Auto-detect GUIDs", _autoDetect);

            if (_autoDetect)
            {
                EditorGUILayout.HelpBox("GUIDs will be auto-detected from package metadata.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Manual GUID Input", EditorStyles.boldLabel);
                _oldGuid = EditorGUILayout.TextField("Old GUID (UICanvasIntializer)", _oldGuid);
                _newGuid = EditorGUILayout.TextField("New GUID (UICanvasInitializer)", _newGuid);
            }

            GUILayout.Space(12);

            // Dry Run Toggle
            _dryRun = EditorGUILayout.Toggle("Dry Run (preview only)", _dryRun);

            GUILayout.Space(12);

            // Execute Button
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Execute Migration", GUILayout.Height(40)))
                {
                    ExecuteMigration();
                }

                if (GUILayout.Button("Clear Log", GUILayout.Width(100)))
                {
                    _logOutput = "";
                }
            }

            GUILayout.Space(12);

            // Log Output
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(200));
            EditorGUILayout.TextArea(_logOutput, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private string FindProjectRoot()
        {
            var assets = Path.Combine(Directory.GetCurrentDirectory(), "Assets");
            return Directory.Exists(assets) ? Directory.GetCurrentDirectory() : "";
        }

        private string FindPackageRoot()
        {
            var manifest = Path.Combine(FindProjectRoot(), "Packages", "manifest.json");
            if (!File.Exists(manifest))
            {
                return "";
            }

            try
            {
                var manifestText = File.ReadAllText(manifest);
                var startIdx = manifestText.IndexOf("\"com.astrayde.framework\":", StringComparison.OrdinalIgnoreCase);
                if (startIdx == -1)
                    return "";

                var valueStart = manifestText.IndexOf("\"", startIdx + 24);
                var valueEnd = manifestText.IndexOf("\"", valueStart + 1);
                var refPath = manifestText.Substring(valueStart + 1, valueEnd - valueStart - 1);

                if (refPath.StartsWith("file:"))
                {
                    refPath = refPath.Substring(5);
                    if (!Path.IsPathRooted(refPath))
                    {
                        refPath = Path.Combine(FindProjectRoot(), refPath);
                    }

                    return Path.GetFullPath(refPath);
                }
            }
            catch
            {
                // Silent fail
            }

            return "";
        }

        private void ExecuteMigration()
        {
            _logOutput = "";
            LogLine("Starting migration...");

            if (string.IsNullOrEmpty(_projectRoot) || !Directory.Exists(_projectRoot))
            {
                LogLine("[ERROR] Invalid project root.");
                return;
            }

            if (_autoDetect)
            {
                if (string.IsNullOrEmpty(_packageRoot) || !Directory.Exists(_packageRoot))
                {
                    LogLine("[ERROR] Could not find package root.");
                    return;
                }

                LogLine($"Auto-detecting GUIDs from: {_packageRoot}");
                if (!AutoDetectGuids())
                {
                    return;
                }
            }

            if (string.IsNullOrEmpty(_oldGuid) || string.IsNullOrEmpty(_newGuid))
            {
                LogLine("[ERROR] GUIDs not specified.");
                return;
            }

            if (_oldGuid == _newGuid)
            {
                LogLine("[ERROR] Old and new GUIDs are identical.");
                return;
            }

            LogLine($"Old GUID: {_oldGuid}");
            LogLine($"New GUID: {_newGuid}");
            LogLine($"Mode: {(_dryRun ? "DRY-RUN" : "APPLY")}");
            LogLine("");

            var files = FindFilesWithGuid(_projectRoot, _oldGuid);
            LogLine($"Found {files.Count} file(s) with old GUID.");

            if (files.Count == 0)
            {
                LogLine("No migration needed.");
                return;
            }

            foreach (var file in files)
            {
                LogLine($"  - {file}");
            }

            if (!_dryRun)
            {
                LogLine("");
                LogLine("Applying replacements...");
                int replaced = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var newContent = content.Replace(_oldGuid, _newGuid);
                        if (content != newContent)
                        {
                            File.WriteAllText(file, newContent);
                            var count = (content.Length - newContent.Length) / (_oldGuid.Length - _newGuid.Length);
                            LogLine($"Updated: {Path.GetFileName(file)} ({count} replacement(s))");
                            replaced += count;
                        }
                    }
                    catch (Exception e)
                    {
                        LogLine($"[ERROR] Failed to update {file}: {e.Message}");
                    }
                }

                LogLine("");
                LogLine($"Done. Total replacements: {replaced}");
                LogLine("Next: open Unity and let it reimport assets.");
                AssetDatabase.Refresh();
            }
            else
            {
                LogLine("Dry-run complete. Uncheck 'Dry Run' and execute again to apply.");
            }
        }

        private bool AutoDetectGuids()
        {
            try
            {
                var newMetaPath = Path.Combine(_packageRoot, _newMetaRel);
                if (File.Exists(newMetaPath))
                {
                    _newGuid = ExtractGuidFromMeta(newMetaPath);
                    if (string.IsNullOrEmpty(_newGuid))
                    {
                        LogLine("[ERROR] Failed to extract new GUID from meta file.");
                        return false;
                    }

                    LogLine($"New GUID detected: {_newGuid}");
                }
                else
                {
                    LogLine("[ERROR] New meta file not found: " + newMetaPath);
                    return false;
                }

                // Try to get old GUID from git history
                _oldGuid = GetGuidFromGitHistory();
                if (string.IsNullOrEmpty(_oldGuid))
                {
                    LogLine("[WARNING] Could not get old GUID from git history.");
                    LogLine("Please enter manually or check git log.");
                    return false;
                }

                LogLine($"Old GUID detected from git: {_oldGuid}");
                return true;
            }
            catch (Exception e)
            {
                LogLine($"[ERROR] Auto-detection failed: {e.Message}");
                return false;
            }
        }

        private string ExtractGuidFromMeta(string metaPath)
        {
            try
            {
                var lines = File.ReadAllLines(metaPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("guid:"))
                    {
                        return line.Substring("guid: ".Length).Trim();
                    }
                }
            }
            catch
            {
                // Silent fail
            }

            return "";
        }

        private string GetGuidFromGitHistory()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"cd '{_packageRoot}' && git log --format=%H -- '{_oldMetaRel}' | head -n 5\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var commits = output.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                    foreach (var commit in commits)
                    {
                        try
                        {
                            var psi2 = new ProcessStartInfo
                            {
                                FileName = "/bin/bash",
                                Arguments = $"-c \"cd '{_packageRoot}' && git show {commit}:{_oldMetaRel} | grep '^guid:' | awk '{{print $2}}'\"",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };

                            using (var process2 = Process.Start(psi2))
                            {
                                var guid = process2.StandardOutput.ReadToEnd().Trim();
                                if (!string.IsNullOrEmpty(guid) && guid.Length == 32)
                                {
                                    return guid;
                                }
                            }
                        }
                        catch
                        {
                            // Try next commit
                        }
                    }
                }
            }
            catch
            {
                // Silent fail
            }

            return "";
        }

        private List<string> FindFilesWithGuid(string rootPath, string guid)
        {
            var results = new List<string>();

            try
            {
                var extensions = new[] { ".prefab", ".unity", ".asset", ".controller", ".overrideController" };
                var searchDirs = new[] { Path.Combine(rootPath, "Assets"), Path.Combine(rootPath, "Packages") };

                foreach (var dir in searchDirs)
                {
                    if (!Directory.Exists(dir))
                        continue;

                    foreach (var ext in extensions)
                    {
                        var files = Directory.GetFiles(dir, $"*{ext}", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            try
                            {
                                var content = File.ReadAllText(file);
                                if (content.Contains(guid))
                                {
                                    results.Add(file);
                                }
                            }
                            catch
                            {
                                // Skip unreadable files
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silent fail
            }

            return results;
        }

        private void LogLine(string message)
        {
            _logOutput += message + "\n";
        }
    }
}

