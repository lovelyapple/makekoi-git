using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using AstraydeFramework.Effect;
using System;
namespace AstraydeFramework.System.UI
{
    public class WindowManager
    {
        private static WindowManager _instance;
        public static WindowManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new WindowManager();
                }

                return _instance;
            }
        }
        public static async UniTask<T> OpenWindowAsync<T>(Func<string, UniTask<GameObject>> customLoadMethod = null) where T : WindowBase
        {
            var path = LocalAssetPathAttributeExtension.GetAssetPath<T>();
            return await Instance.OpenWindowInnerAsync(path, customLoadMethod: customLoadMethod) as T;
        }
        public static async UniTask<IWindow> OpenWindowAsync(string path, Func<string, UniTask<GameObject>> customLoadMethod = null)
        {
            return await Instance.OpenWindowInnerAsync(path, customLoadMethod);
        }
        public async UniTask<IWindow> OpenWindowInnerAsync(string path, Func<string, UniTask<GameObject>> customLoadMethod = null)
        {
            GameObject prefab = null;
            if (customLoadMethod != null)
            {
                prefab = await customLoadMethod(path) as GameObject;
            }
            else
            {
                prefab = await Resources.LoadAsync(path) as GameObject;
            }

            if (prefab == null)
            {
                throw new ArgumentException($"could not load window parefab from path {path}");
            }

            var instance = GameObject.Instantiate(prefab.gameObject);
            var canvansInitializer = instance.GetComponent<UICanvasInitializer>();

            if (canvansInitializer == null)
            {
                throw new MissingComponentException($"could not create a window without UIcanvasInitialize {path}");
            }

            canvansInitializer.Hide();
            var token = new CancellationToken();

            if (canvansInitializer.EnableBlur)
            {
                await ScreenBlurController.ShowAsync(canvansInitializer.OrderInLayer - 1, token);
            }

            canvansInitializer.Show();
            return instance.GetComponent<IWindow>();
        }
    }
}
