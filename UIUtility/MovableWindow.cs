using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UILib
{
    public class MovableWindow : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Vector2 _cachedDragPosition;
        private Vector2 _cachedMousePosition;
        private bool _isDragging = false;
        private BaseCameraControl _cameraControl;
        private BaseCameraControl.NoCtrlFunc _noControlFunctionCached;

        public event Action<PointerEventData> onPointerDown;
        public event Action<PointerEventData> onDrag;
        public event Action<PointerEventData> onPointerUp;

        public RectTransform toDrag;
        public bool preventCameraControl;

        protected override void Awake()
        {
            base.Awake();
            this._cameraControl = GameObject.FindObjectOfType<BaseCameraControl>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (this.preventCameraControl && this._cameraControl)
            {
                this._noControlFunctionCached = this._cameraControl.NoCtrlCondition;
                this._cameraControl.NoCtrlCondition = () => true;
            }
            this._isDragging = true;
            this._cachedDragPosition = this.toDrag.position;
            this._cachedMousePosition = Input.mousePosition;
            this.onPointerDown?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            this._isDragging = true;
            this.toDrag.position = this._cachedDragPosition + ((Vector2)Input.mousePosition - this._cachedMousePosition);
            this.onDrag?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (this.preventCameraControl && this._cameraControl)
                this._cameraControl.NoCtrlCondition = this._noControlFunctionCached;
            this._isDragging = false;
            this.onPointerUp?.Invoke(eventData);
        }
    }
}
