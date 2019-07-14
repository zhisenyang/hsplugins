using System;
using UnityEngine;

namespace BonesFramework
{
    public class OnDestroyTrigger : MonoBehaviour
    {
        public Action<OnDestroyTrigger> onStart;

        private void Start()
        {
            if (this.onStart != null)
                this.onStart(this);
        }

        public Action<GameObject> onDestroy;

        private void OnDestroy()
        {
            if (this.onDestroy != null)
                this.onDestroy(this.gameObject);
        }
    }
}
