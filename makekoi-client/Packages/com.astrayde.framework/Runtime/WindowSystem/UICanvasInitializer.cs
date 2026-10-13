using System.Collections.Generic;
using System.Linq;
using AstraydeFramework.Effect;
using AstraydeFramework.System.Other;
using UnityEngine;
using UnityEngine.UI;
namespace AstraydeFramework.System.UI
{
    public class UICanvasInitializer : MonoBehaviour
    {
        private static int LastOrderInLayer = 100;
        public static int CurrentLastOrderInLayer => LastOrderInLayer;
        private Canvas _canvas;
        private CanvasScaler _canvasScaler;
        public int OrderInLayer { get; private set; }
        [SerializeField] bool IsGlobalUI;
        [SerializeField] bool UseBlur;
        public bool EnableBlur => UseBlur;
        public static List<UICanvasInitializer> StackCanvas = new List<UICanvasInitializer>();
        [SerializeField] private bool _useLandscapeMode = true;
        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.worldCamera = UICameraHandler.GetCurrentUICamera();

            _canvasScaler = GetComponent<CanvasScaler>();
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = _useLandscapeMode
                ? new Vector2(1920f, 1080f)
                : new Vector2(1080f, 1920f);
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            if (IsGlobalUI)
            {
                _canvas.sortingOrder = 999;
            }
            else
            {
                LastOrderInLayer += 2;
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = LastOrderInLayer;
                StackCanvas.Add(this);
            }

            OrderInLayer = _canvas.sortingOrder;
        }
        public void Hide()
        {
            _canvas.enabled = false;
        }
        public void Show()
        {
            _canvas.enabled = true;
        }
        private void OnDestroy()
        {
            if (!IsGlobalUI)
            {
                StackCanvas.Remove(this);
            }

            var orderedCanvas = StackCanvas.OrderByDescending(x => x.OrderInLayer);
            var lastOne = orderedCanvas.FirstOrDefault();

            if (lastOne == null)
            {
                ScreenBlurController.Close();
                return;
            }

            LastOrderInLayer = lastOne.OrderInLayer;
            ScreenBlurController.UpdateLayerOrder(lastOne.OrderInLayer - 1);

            if (orderedCanvas.All(x => x.UseBlur == false))
            {
                ScreenBlurController.Close();
            }
        }
    }
}