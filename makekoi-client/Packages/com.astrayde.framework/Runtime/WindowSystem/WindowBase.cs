using UnityEngine;
using UnityEngine.UI;
using R3;
using AstraydeFramework.System;

namespace AstraydeFramework.System.UI
{
    public abstract class WindowInitializeInfoBase
    {

    }
    public interface IWindow
    {
        public void Initialize(WindowInitializeInfoBase initInfoBase);
        public void OnPreClose();
        public void OnPostClose();
        public void Close();
    }
    [RequireComponent(typeof(UICanvasInitializer))]
    [LocalAssetPath("empty")]
    public class WindowBase : MonoBehaviour, IWindow
    {
        public virtual void Initialize(WindowInitializeInfoBase initInfoBase)
        {

        }
        /// <summary>
        /// Before window gameobject destory
        /// </summary>
        public virtual void OnPreClose() { }
        /// <summary>
        /// After window gameobject destory
        /// </summary>
        public virtual void OnPostClose() { }
        public virtual void Close()
        {
            OnPreClose();
            Destroy(gameObject);
            OnPostClose();
        }
    }
}