using System;
using UnityEngine.EventSystems;

namespace UILib
{
    public class PointerDownHandler : UIBehaviour, IPointerDownHandler
    {
        public event Action<PointerEventData> onPointerDown;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (this.onPointerDown != null)
                this.onPointerDown(eventData);
        }
    }
}
