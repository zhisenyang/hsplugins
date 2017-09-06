using System;
using JetBrains.Annotations;
using UnityEngine;

namespace HSPE
{
    public class CameraGL : MonoBehaviour
    {
        public event Action onPostRender;
        public event Action onPreCull; 
        public Camera camera { get; private set; }

        void Awake()
        {
            this.camera = this.GetComponent<Camera>();
        }

        void OnPostRender()
        {
            if (this.onPostRender != null)
                this.onPostRender();
        }

        void OnPreCull()
        {
            if (this.onPreCull != null)
                this.onPreCull();
        }
    }
}
