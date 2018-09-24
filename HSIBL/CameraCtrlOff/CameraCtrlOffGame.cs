using UnityEngine;
using System.Collections;

namespace HSIBL
{
    public class CameraCtrlOffGame : MonoBehaviour
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
                if (cameraCtrl.NoCtrlCondition != overwriteNoCtrlFunc)
                {
                    cameraCtrl.NoCtrlCondition = overwriteNoCtrlFunc;
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
            cameraCtrl = Camera.main.GetComponent<CameraControl_Ver2>();

            if (cameraCtrl.NoCtrlCondition != new BaseCameraControl_Ver2.NoCtrlFunc(CheckCameraCtrlOff))
            {
                defaultNoCtrlFunc = cameraCtrl.NoCtrlCondition;
                overwriteNoCtrlFunc = new BaseCameraControl_Ver2.NoCtrlFunc(CheckCameraCtrlOff);
                cameraCtrl.NoCtrlCondition = overwriteNoCtrlFunc;
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

        private CameraControl_Ver2 cameraCtrl;
        private bool cameraCtrlOffNextFrame;
        private BaseCameraControl_Ver2.NoCtrlFunc defaultNoCtrlFunc;
        private BaseCameraControl_Ver2.NoCtrlFunc overwriteNoCtrlFunc;
    }
}
