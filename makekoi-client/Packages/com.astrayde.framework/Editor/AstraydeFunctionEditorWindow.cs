using System;
using System.Collections.Generic;
using System.Linq;
using AstraydeFramework.Sample;
using AstraydeFramework.System.UI;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace AstraydeFramework.Editor
{
    public class AstraydeFunctionEditorWindow : EditorWindow
    {
        [Serializable]
        private class WindowEntry
        {
            public GameObject WindowPrefab;
            [NonSerialized] public WindowBase OpenedWindow;
        }

        private class WindowValidation
        {
            public bool IsValid;
            public string Message;
            public Type WindowType;
        }

        private static AstraydeFunctionEditorWindow _instance;
        [SerializeField] private List<WindowEntry> _windowEntries = new();
        private Vector2 _stackCanvasScroll;
        private double _nextRepaintAt;

        [MenuItem("Tools/AstraydeFramework/DebugFunctionEditorWindow")]
        public static void Open()
        {
            if (_instance == null)
            {
                _instance = GetWindow<AstraydeFunctionEditorWindow>();
            }
        }

        private void OnEnable()
        {
            if (_windowEntries.Count == 0)
            {
                _windowEntries.Add(new WindowEntry());
            }

            _nextRepaintAt = EditorApplication.timeSinceStartup + 1.0d;
        }

        private void OnGUI()
        {
            GUILayout.Label("Run this Function in DevelopmentScene!");

            if (GUILayout.Button("Open blur window"))
            {
                WindowManager.OpenWindowAsync<Testwindow_with_blur>().Forget();
            }

            if (GUILayout.Button("Open non-blur window"))
            {
                WindowManager.OpenWindowAsync<Testwindow_no_blur>().Forget();
            }

            GUILayout.Space(12);
            GUILayout.Label("Window Prefab List");

            var validations = BuildValidations();

            for (var i = 0; i < _windowEntries.Count; i++)
            {
                DrawWindowRow(i, validations[i]);
            }

            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Window Field"))
                {
                    _windowEntries.Add(new WindowEntry());
                }

                using (new EditorGUI.DisabledScope(_windowEntries.Count == 0))
                {
                    if (GUILayout.Button("Remove Last"))
                    {
                        _windowEntries.RemoveAt(_windowEntries.Count - 1);
                    }
                }
            }

            DrawStackCanvasView();
        }

        private void Update()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now < _nextRepaintAt)
            {
                return;
            }

            _nextRepaintAt = now + 1.0d;
            Repaint();
        }
        private void DrawStackCanvasView()
        {
            GUILayout.Space(12);
            GUILayout.Label("Current StackCanvas (OrderInLayer desc)");

            var canvases = UICanvasInitializer.StackCanvas
                .Where(x => x != null)
                .OrderByDescending(x => x.OrderInLayer)
                .ToList();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("Count", canvases.Count);
            }

            _stackCanvasScroll = EditorGUILayout.BeginScrollView(_stackCanvasScroll, GUILayout.Height(220));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntField("LastOrderInLayer", UICanvasInitializer.CurrentLastOrderInLayer);
            }
            GUILayout.Space(4);

            if (canvases.Count == 0)
            {
                GUILayout.Label("No active UICanvasInitializer in StackCanvas.");
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("#", GUILayout.Width(30));
                    GUILayout.Label("Order", GUILayout.Width(70));
                    GUILayout.Label("WindowType", GUILayout.Width(180));
                    GUILayout.Label("GameObject");
                }

                for (var i = 0; i < canvases.Count; i++)
                {
                    var canvas = canvases[i];
                    var window = canvas.GetComponent<WindowBase>();
                    var windowTypeName = window != null ? window.GetType().Name : "-";

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label((i + 1).ToString(), GUILayout.Width(30));
                        GUILayout.Label(canvas.OrderInLayer.ToString(), GUILayout.Width(70));
                        GUILayout.Label(windowTypeName, GUILayout.Width(180));
                        EditorGUILayout.ObjectField(canvas.gameObject, typeof(GameObject), true);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private List<WindowValidation> BuildValidations()
        {
            var validations = new List<WindowValidation>(_windowEntries.Count);
            var typeCounts = new Dictionary<Type, int>();

            foreach (var entry in _windowEntries)
            {
                var validation = ValidateEntry(entry);
                validations.Add(validation);

                if (validation.IsValid && validation.WindowType != null)
                {
                    if (!typeCounts.ContainsKey(validation.WindowType))
                    {
                        typeCounts[validation.WindowType] = 0;
                    }

                    typeCounts[validation.WindowType]++;
                }
            }

            foreach (var validation in validations.Where(v => v.IsValid && v.WindowType != null))
            {
                if (typeCounts[validation.WindowType] > 1)
                {
                    validation.IsValid = false;
                    validation.Message = $"duplicated window class: {validation.WindowType.Name}";
                }
            }

            return validations;
        }

        private static WindowValidation ValidateEntry(WindowEntry entry)
        {
            if (entry.WindowPrefab == null)
            {
                return new WindowValidation
                {
                    IsValid = false,
                    Message = "assign a window prefab"
                };
            }

            if (entry.WindowPrefab.GetComponent<UICanvasInitializer>() == null)
            {
                return new WindowValidation
                {
                    IsValid = false,
                    Message = "invalid prefab (requires: UICanvasInitializer)"
                };
            }

            var windowComponents = entry.WindowPrefab
                .GetComponents<WindowBase>()
                .Where(x => x != null && x.GetType() != typeof(WindowBase))
                .ToArray();

            if (windowComponents.Length == 0)
            {
                return new WindowValidation
                {
                    IsValid = false,
                    Message = "invalid prefab (requires: WindowBase derived class)"
                };
            }

            if (windowComponents.Length > 1)
            {
                return new WindowValidation
                {
                    IsValid = false,
                    Message = "invalid prefab (multiple WindowBase derived classes)"
                };
            }

            return new WindowValidation
            {
                IsValid = true,
                WindowType = windowComponents[0].GetType()
            };
        }

        private void DrawWindowRow(int index, WindowValidation validation)
        {
            var entry = _windowEntries[index];

            if (entry.OpenedWindow == null || entry.OpenedWindow.Equals(null))
            {
                entry.OpenedWindow = null;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var previousPrefab = entry.WindowPrefab;
                entry.WindowPrefab = EditorGUILayout.ObjectField(
                    entry.WindowPrefab,
                    typeof(GameObject),
                    false) as GameObject;
                if (previousPrefab != entry.WindowPrefab)
                {
                    entry.OpenedWindow = null;
                    Repaint();
                }

                GUILayout.Space(8);

                if (entry.OpenedWindow != null)
                {
                    GUILayout.Label("opened...", GUILayout.Width(90));
                    if (GUILayout.Button("Close", GUILayout.Width(120)))
                    {
                        entry.OpenedWindow.Close();
                        entry.OpenedWindow = null;
                    }
                }
                else if (validation.IsValid)
                {
                    if (GUILayout.Button("Open", GUILayout.Width(120)))
                    {
                        OpenEntryAsync(entry, validation.WindowType).Forget();
                    }
                }
                else
                {
                    GUILayout.Label(validation.Message, GUILayout.Width(280));
                }
            }
        }

        private async UniTaskVoid OpenEntryAsync(WindowEntry entry, Type windowType)
        {
            try
            {
                var opened = await WindowManager.OpenWindowAsync(
                    windowType.FullName,
                    _ => UniTask.FromResult(entry.WindowPrefab));
                entry.OpenedWindow = opened as WindowBase;
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to open window prefab: {e.Message}");
            }
        }
    }
}
