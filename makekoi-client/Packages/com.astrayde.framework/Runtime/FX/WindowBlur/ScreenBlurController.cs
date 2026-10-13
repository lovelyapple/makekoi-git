using System;
using System.Threading;
using AstraydeFramework.System.Other;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
namespace AstraydeFramework.Effect
{
    public class ScreenBlurController : MonoBehaviour
    {
        private const int ScaleTimes = 2;
        private const float FadeDuration = 0.3f;
        [SerializeField] private RawImage BlurImage;
        [SerializeField] private Material BlurMaterial;
        private Canvas _canvas;
        private RenderTexture _renderTexture1 = null;
        private RenderTexture _renderTexture2 = null;
        private Texture2D _sourceTexture = null;
        private int _srcW = 0;
        private int _srcH = 0;

        private static ScreenBlurController _instance;
        private static CancellationTokenSource _blurCancellationTokenSource;
        private static CancellationTokenSource _blurFadeCancellationTokenSource;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _instance = this;

            _canvas = GetComponent<Canvas>();
            _canvas.worldCamera = UICameraHandler.GetCurrentUICamera();

            _srcW = Screen.width;
            _srcH = Screen.height;

            // 縮小 RT
            int w = _srcW / ScaleTimes;
            int h = _srcH / ScaleTimes;

            _renderTexture1 = new RenderTexture(w, h, 0);
            _renderTexture2 = new RenderTexture(w, h, 0);

            _renderTexture1.Create();
            _renderTexture2.Create();

            _sourceTexture = new Texture2D(_srcW, _srcH, TextureFormat.RGB24, false);
            ResetColor();
            BlurImage.gameObject.SetActive(false);
        }
        void OnDestroy()
        {
            if (_renderTexture1 != null)
            {
                _renderTexture1.Release();
                Destroy(_renderTexture1);
                _renderTexture1 = null;
            }

            if (_renderTexture2 != null)
            {
                _renderTexture2.Release();
                Destroy(_renderTexture2);
                _renderTexture2 = null;
            }

            if (_sourceTexture != null)
            {
                Destroy(_sourceTexture);
                _sourceTexture = null;
            }

            if (BlurImage != null && BlurImage.texture is RenderTexture renderTexture)
            {
                if (renderTexture != _renderTexture1 && renderTexture != _renderTexture2)
                {
                    renderTexture.Release();
                    Destroy(renderTexture);
                }

                BlurImage.texture = null;
            }
        }
        public static async UniTask ShowAsync(int layerOrder, CancellationToken token)
        {
            _blurCancellationTokenSource?.Cancel();
            _blurCancellationTokenSource?.Dispose();
            _blurCancellationTokenSource = null;

            var cts = CancellationTokenSource.CreateLinkedTokenSource(
                _instance.destroyCancellationToken, token);
            _blurCancellationTokenSource = cts;

            try
            {
                await _instance.TakeBlurAsync(layerOrder, cts.Token);
            }
            finally
            {
                cts.Dispose();

                if (_blurCancellationTokenSource == cts)
                {
                    _blurCancellationTokenSource = null;
                }
            }
        }
        public static void Close()
        {
            _instance?.Clear();
        }
        public static bool ShowingBlur()
        {
            return _instance.BlurImage.gameObject.activeInHierarchy;
        }
        private void ResetColor()
        {
            if (BlurImage != null)
            {
                var col = Color.white;
                col.a = 0;
                BlurImage.color = col;
            }
        }
        private async UniTask TakeBlurAsync(int layerOrder, CancellationToken token)
        {
            if (_canvas.worldCamera == null)
            {
                _canvas.worldCamera = UICameraHandler.GetCurrentUICamera();
            }

            var wasShowing = ShowingBlur();
            if (wasShowing)
            {
                // Re-capture should use the real screen (without previous blur overlay).
                BlurImage.gameObject.SetActive(false);
            }

            await UniTask.WaitForEndOfFrame(this, cancellationToken: token);

            if (token.IsCancellationRequested)
            {
                if (wasShowing)
                {
                    BlurImage.gameObject.SetActive(true);
                }
                return;
            }

            // iOS Metal では ReadPixels がフレームバッファをクリア後に読む場合があるため
            // ScreenCapture API を使用してプラットフォームの差異を吸収する
#if UNITY_IOS && !UNITY_EDITOR
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            Graphics.Blit(screenshot, _renderTexture1);
            Destroy(screenshot);
#else
            // get full screen size
            _sourceTexture.ReadPixels(new Rect(0, 0, _srcW, _srcH), 0, 0);
            _sourceTexture.Apply();
            // scale down
            Graphics.Blit(_sourceTexture, _renderTexture1);
#endif

            // mix 2 times
            Graphics.Blit(_renderTexture1, _renderTexture2, BlurMaterial);
            Graphics.Blit(_renderTexture2, _renderTexture1, BlurMaterial);

            BlurImage.texture = _renderTexture1;
            UpdateLayerOrderInner(layerOrder);

            BlurImage.gameObject.SetActive(true);
            if (wasShowing)
            {
                BlurImage.color = Color.white;
            }
            else
            {
                _instance.RunFadeAlphaOnece(token);
            }
        }
        public void RunFadeAlphaOnece(CancellationToken token)
        {
            if (BlurImage.color.a != 0)
            {
                return;
            }

            if (_blurFadeCancellationTokenSource != null)
            {
                _blurFadeCancellationTokenSource.Cancel();
                _blurFadeCancellationTokenSource.Dispose();
                _blurFadeCancellationTokenSource = null;
            }

            _blurFadeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.destroyCancellationToken);
            token = _blurFadeCancellationTokenSource.Token;
            RunAlphaAsync(token).Forget();
        }
        private async UniTask RunAlphaAsync(CancellationToken token)
        {
            var col = BlurImage.color;
            col.a = 0f;
            BlurImage.color = col;

            float duration = FadeDuration;
            float t = 0f;

            bool canceledByDestroy = false;
            using var _ = destroyCancellationToken.Register(() => canceledByDestroy = true);

            try
            {
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime; // UIならunscaled推奨
                    col.a = Mathf.Clamp01(t / duration);
                    BlurImage.color = col;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                BlurImage.color = Color.white;
            }
            catch (OperationCanceledException)
            {
                if (!canceledByDestroy)
                {
                    ResetColor();
                }
            }
            finally
            {
                _blurFadeCancellationTokenSource = null;
            }
        }
        private void Clear()
        {
            _blurCancellationTokenSource?.Cancel();
            _blurCancellationTokenSource?.Dispose();
            _blurCancellationTokenSource = null;

            ResetColor();

            if (BlurImage != null)
                BlurImage.gameObject.SetActive(false);
        }
        public static void UpdateLayerOrder(int layerOrder)
        {
            _instance?.UpdateLayerOrderInner(layerOrder);
        }
        public void UpdateLayerOrderInner(int layerOrder)
        {
            if (_canvas != null)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = layerOrder;
            }
        }
    }
}
