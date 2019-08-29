using ToolBox;
using UILib;
using UnityEngine;
using UnityEngine.UI;

namespace HSUS
{
    public static class DeleteConfirmation
    {
        public static void Do()
        {
            HSUS._self._routines.ExecuteDelayed(() =>
            {
                Canvas c = UIUtility.CreateNewUISystem("HSUSDeleteConfirmation");
                c.sortingOrder = 40;
                c.transform.SetParent(GameObject.Find("StudioScene").transform);
                c.transform.localPosition = Vector3.zero;
                c.transform.localScale = Vector3.one;
                c.transform.SetRect();
                c.transform.SetAsLastSibling();

                Image bg = UIUtility.CreateImage("Background", c.transform);
                bg.rectTransform.SetRect();
                bg.sprite = null;
                bg.color = new Color(0f, 0f, 0f, 0.5f);
                bg.raycastTarget = true;

                Image panel = UIUtility.CreatePanel("Panel", bg.transform);
                panel.rectTransform.SetRect(Vector2.zero, Vector2.one, new Vector2(640f / 2, 360f / 2), new Vector2(-640f / 2, -360f / 2));
                panel.color = Color.gray;

                Text text = UIUtility.CreateText("Text", panel.transform, "Are you sure you want to delete this object?");
                text.rectTransform.SetRect(new Vector2(0f, 0.5f), Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
                text.color = Color.white;
                text.resizeTextForBestFit = true;
                text.resizeTextMaxSize = 100;
                text.alignByGeometry = true;
                text.alignment = TextAnchor.MiddleCenter;

                Button yes = UIUtility.CreateButton("YesButton", panel.transform, "Yes");
                (yes.transform as RectTransform).SetRect(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(10f, 10f), new Vector2(-10f, -10f));
                text = yes.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = true;
                text.resizeTextMaxSize = 100;
                text.alignByGeometry = true;
                text.alignment = TextAnchor.MiddleCenter;

                Button no = UIUtility.CreateButton("NoButton", panel.transform, "No");
                (no.transform as RectTransform).SetRect(new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(10f, 10f), new Vector2(-10f, -10f));
                text = no.GetComponentInChildren<Text>();
                text.resizeTextForBestFit = true;
                text.resizeTextMaxSize = 100;
                text.alignByGeometry = true;
                text.alignment = TextAnchor.MiddleCenter;

                c.gameObject.AddComponent<DeleteConfirmationComponent>();
                c.gameObject.SetActive(false);

            }, 20);
        }
    }

    public class DeleteConfirmationComponent : MonoBehaviour
    {
        #region Private Variables
        private Button.ButtonClickedEvent _deleteAction;
        #endregion

        #region Unity Methods
        void Awake()
        {
            this.transform.FindDescendant("YesButton").GetComponent<Button>().onClick.AddListener(this.YesPressed);
            this.transform.FindDescendant("NoButton").GetComponent<Button>().onClick.AddListener(this.NoPressed);
            Button deleteButton = GameObject.Find("StudioScene/Canvas Object List/Image Bar/Button Delete").GetComponent<Button>();
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
