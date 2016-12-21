using UnityEngine.EventSystems;

namespace HSPE
{
    public class PointerDownHandler : UIBehaviour, IPointerDownHandler
    {
        public delegate void PointerDownDelegate();

        public event PointerDownDelegate onPointerDown;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (this.onPointerDown != null)
                this.onPointerDown();
        }
    }
}
