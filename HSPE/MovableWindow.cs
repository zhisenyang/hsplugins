using UnityEngine;
using UnityEngine.EventSystems;

namespace HSPE
{
    public class MovableWindow : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Vector2 _cachedDragPosition;
        private Vector2 _cachedMousePosition;
        private CameraControl _camController;
        private bool _moving = false;

        public RectTransform toDrag;

        protected override void Awake()
        {
            base.Awake();
            this._camController = FindObjectOfType<CameraControl>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            this._cachedDragPosition = toDrag.position;
            this._cachedMousePosition = Input.mousePosition;
            this._moving = true;
            this._camController.NoCtrlCondition = this.Condition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            this._moving = true;
            toDrag.position = this._cachedDragPosition + ((Vector2)Input.mousePosition - this._cachedMousePosition);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            this._moving = false;
        }

        private bool Condition()
        {
            return this._moving;
        }
    }
}
