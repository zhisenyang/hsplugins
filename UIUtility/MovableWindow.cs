using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UILib
{
    public class MovableWindow : UIBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Vector2 _cachedDragPosition;
        private Vector2 _cachedMousePosition;

        public event Action<PointerEventData> onPointerDown;
        public event Action<PointerEventData> onDrag;
        public event Action<PointerEventData> onPointerUp;

        public RectTransform toDrag;

        public void OnPointerDown(PointerEventData eventData)
        {
            this._cachedDragPosition = this.toDrag.position;
            this._cachedMousePosition = Input.mousePosition;
            if (this.onPointerDown != null)
                this.onPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            this.toDrag.position = this._cachedDragPosition + ((Vector2)Input.mousePosition - this._cachedMousePosition);
            if (this.onDrag != null)
                this.onDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (this.onPointerUp != null)
                this.onPointerUp(eventData);
        }
    }
}
