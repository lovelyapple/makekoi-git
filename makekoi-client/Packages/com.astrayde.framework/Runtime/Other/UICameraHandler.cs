using UnityEngine;
namespace AstraydeFramework.System.Other
{
    public class UICameraHandler : MonoBehaviour
    {
        public Camera UICamera { get; private set; }
        private Camera GetUICamera()
        {
            if (UICamera == null)
            {
                UICamera = this.gameObject.GetComponent<Camera>();
            }

            return UICamera;
        }
        public static Camera GetCurrentUICamera()
        {
            var handler = GameObject.FindFirstObjectByType<UICameraHandler>();
            return handler?.GetUICamera();
        }
    }
}