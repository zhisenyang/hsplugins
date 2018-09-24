using UnityEngine;
using System.Collections;

namespace HSIBL
{
    public class CameraCtrlOffStudio : MonoBehaviour
    {
        private void Start()
        {
            StopAllCoroutines();
            StartCoroutine(CheckCameraCtrlOffFlagCo());
        }
        private void Update()
        {
            if (gameObject.GetComponent<HSIBL>().cameraCtrlOff)
            {
                cameraCtrlOffNextFrame = true;
                if (cameraCtrl.noCtrlCondition != overwriteNoCtrlFunc)
                {
                    cameraCtrl.noCtrlCondition = overwriteNoCtrlFunc;
                }
            }
            else
            {
                cameraCtrlOffNextFrame = false;
            }
            gameObject.GetComponent<HSIBL>().cameraCtrlOff = false;
        }

        private IEnumerator CheckCameraCtrlOffFlagCo()
        {
            for (;;)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        private void Awake()
        {
            cameraCtrl = Singleton<Studio.Studio>.Instance.cameraCtrl;

            if (cameraCtrl.noCtrlCondition != new Studio.CameraControl.NoCtrlFunc(CheckCameraCtrlOff))
            {
                defaultNoCtrlFunc = cameraCtrl.noCtrlCondition;
                overwriteNoCtrlFunc = new Studio.CameraControl.NoCtrlFunc(CheckCameraCtrlOff);
                cameraCtrl.noCtrlCondition = overwriteNoCtrlFunc;
            }
        }

        public bool CheckCameraCtrlOff()
        {
            bool flag = cameraCtrlOffNextFrame;
            if (defaultNoCtrlFunc != null)
            {
                flag |= defaultNoCtrlFunc();
            }
            return flag;
        }
        
        private bool cameraCtrlOffNextFrame;
        private Studio.CameraControl cameraCtrl;
        private Studio.CameraControl.NoCtrlFunc defaultNoCtrlFunc;
        private Studio.CameraControl.NoCtrlFunc overwriteNoCtrlFunc;
    }
}
