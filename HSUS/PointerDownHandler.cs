using UnityEngine.EventSystems;

namespace HSUS
{
    public class PointerDownHandler : UIBehaviour, IPointerDownHandler
    {
        public delegate void PointerDownDelegate(PointerEventData eventData = null);

        public event PointerDownDelegate onPointerDown;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (this.onPointerDown != null)
                this.onPointerDown(eventData);
        }
    }
}
