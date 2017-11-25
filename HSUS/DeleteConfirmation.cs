using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public class DeleteConfirmation : MonoBehaviour
    {
        #region Private Variables
        private Button.ButtonClickedEvent _deleteAction;
        #endregion

        #region Unity Methods
        void Awake()
        {
            this.transform.FindDescendant("YesButton").GetComponent<Button>().onClick.AddListener(this.YesPressed);
            this.transform.FindDescendant("NoButton").GetComponent<Button>().onClick.AddListener(this.NoPressed);
            Button deleteButton = GameObject.Find("StudioScene").transform.FindChild("Canvas Object List/Image Bar/Button Delete").GetComponent<Button>();
            this._deleteAction = deleteButton.onClick;
            deleteButton.onClick = new Button.ButtonClickedEvent();
            deleteButton.onClick.AddListener(this.DisplayDialog);
        }
        #endregion

        #region Private Methods
        private void DisplayDialog()
        {
            this.gameObject.SetActive(true);
        }

        private void NoPressed()
        {
            this.gameObject.SetActive(false);
        }

        private void YesPressed()
        {
            this._deleteAction.Invoke();
            this.gameObject.SetActive(false);
        }
        #endregion
    }
}
