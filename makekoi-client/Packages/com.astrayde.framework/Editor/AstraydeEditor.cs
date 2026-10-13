
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using AstraydeFramework.System.UI;

namespace Astrayde.Editor
{
    public static class AstraydeEditor
    {
        [MenuItem("Assets/Create/Astrayde/NewWindowPrefab", priority = 0)]
        public static void CreateEmptyWindow()
        {
            var go = new GameObject("NewWindow");
            SetupWindowRoot(go);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Astrayde/NewWindow", false, 10)]
        public static void CreateEmptyWindowInHierarchy()
        {
            var go = new GameObject("NewWindow");
            Undo.RegisterCreatedObjectUndo(go, "Create NewWindow");

            if (Selection.activeTransform != null)
            {
                go.transform.SetParent(Selection.activeTransform, false);
            }

            SetupWindowRoot(go);
            Selection.activeObject = go;
        }

        private static void SetupWindowRoot(GameObject go)
        {
            ApplyUILayer(go);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.pixelPerfect = false;
            canvas.overrideSorting = false;

            var canvasScaler = go.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<UICanvasInitializer>();
        }

        private static void ApplyUILayer(GameObject go)
        {
            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                Debug.LogWarning("UI layer was not found. NewWindow keeps its default layer.");
                return;
            }

            SetLayerRecursively(go.transform, uiLayer);
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }
    }
}
