using UnityEngine;
using UnityEngine.EventSystems;

namespace HSPE
{
    public class MovableWindow : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Vector2 _cachedDragPosition;
        private Vector2 _cachedMousePosition;
        private bool _moving = false;

        public RectTransform toDrag;

        public void OnPointerDown(PointerEventData eventData)
        {
            this._cachedDragPosition = this.toDrag.position;
            this._cachedMousePosition = Input.mousePosition;
            this._moving = true;
            Studio.Studio.Instance.cameraCtrl.noCtrlCondition = this.Condition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            this._moving = true;
            this.toDrag.position = this._cachedDragPosition + ((Vector2)Input.mousePosition - this._cachedMousePosition);
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
