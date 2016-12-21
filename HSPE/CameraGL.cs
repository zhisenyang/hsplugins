using UnityEngine;

namespace HSPE
{
    public class CameraGL : MonoBehaviour
    {
        public delegate void OnPostRenderDelegate();

        public event OnPostRenderDelegate onPostRender;

        void OnPostRender()
        {
            if (this.onPostRender != null)
                this.onPostRender();
        }
    }
}
