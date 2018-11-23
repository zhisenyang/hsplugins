#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HSUS
{
    public class ShortcutsDisabler : MonoBehaviour
    {
        private MonoBehaviour _shortcutsHSController;
        IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Type t = Type.GetType("ShortcutsHSParty.ShortcutsHSPartyController,ShortcutsHSParty");
            if (t != null)
                this._shortcutsHSController = FindObjectOfType(t) as MonoBehaviour;
        }
        void Update()
        {
            if (this._shortcutsHSController != null)
            {
                if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null)
                    this._shortcutsHSController.enabled = false;
                else
                    this._shortcutsHSController.enabled = true;
            }
        }
    }
}
#endif