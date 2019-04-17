using System;
using UnityEngine;

namespace NodesConstraints
{
    public class CameraEventsDispatcher : MonoBehaviour
    {
        public event Action onPreCull;
        private void OnPreCull()
        {
            if (this.onPreCull != null)
                this.onPreCull();
        }

        public event Action onPreRender;
        private void OnPreRender()
        {
            if (this.onPreRender != null)
                this.onPreRender();
        }
#if HONEYSELECT
        public event Action onGUI;
        private void OnGUI()
        {
            if (this.onGUI != null)
                this.onGUI();
        }
#endif
    }
}
