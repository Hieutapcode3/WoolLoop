using Sirenix.OdinInspector;
using UnityEngine;

namespace AntiStress.MiniGame.MiniGameCamera
{
    public class MiniGameCamera : MonoBehaviour
    {
        Camera cam;
        [ShowInInspector]
        Camera ThisCamera
        {
            get
            {
                if (cam == null)
                {
                    cam = GetComponent<Camera>();
                }
                return cam;
            }
        }
        [SerializeField] public float ZoomIndex = 0.6f;
        [ShowInInspector] public float ScreenRatio => (float)Screen.currentResolution.height / (float)Screen.currentResolution.width;
        [ShowInInspector] float hight => (float)Screen.currentResolution.height;
        [ShowInInspector] float width => (float)Screen.currentResolution.width;

        [SerializeField, ReadOnly] float defaultOrthoSize;
        [SerializeField, ReadOnly] public float defaultFOV;

        private void Awake()
        {
            defaultOrthoSize = ThisCamera.orthographicSize;
            defaultFOV = ThisCamera.fieldOfView;
            Calculate();
            //DPDebug.Log($"<color=green>freeLookCamera</color>");
        }

        [Button]
        public void Calculate()
        {
            if (IsLongScreen())
            {
                if (ThisCamera.orthographic)
                {
                    ThisCamera.orthographicSize = defaultOrthoSize * (ScreenRatio * ZoomIndex);

                }
                else
                    ThisCamera.fieldOfView = defaultFOV * (ScreenRatio * ZoomIndex);
            }

            // DPDebug.Log($"MiniGameCamera.Calculate() - ScreenRatio: {ScreenRatio}, ZoomIndex: {ZoomIndex}, " +
            //     $"OrthographicSize: {ThisCamera.orthographicSize}, FieldOfView: {ThisCamera.fieldOfView}", this);
        }

        public bool IsLongScreen()
        {
            Debug.Log($"ScreenRatio: {ScreenRatio}, width: {(float)Screen.currentResolution.width}, hight: {(float)Screen.currentResolution.height}");
            return width / hight < 9f / 16f;
        }
    }
}