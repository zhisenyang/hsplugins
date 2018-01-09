using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using CustomMenu;
using Harmony;
using IllusionUtility.GetUtility;
using IllusionUtility.SetUtility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoreAccessories
{
    [HarmonyPatch(typeof(CharFile))]
    [HarmonyPatch("ChangeCoordinateType")]
    [HarmonyPatch(new[] {typeof(CharDefine.CoordinateType) })]
    public class CharFile_ChangeCoordinateType_Patches
    {
        public static void Postfix(CharFile __instance, bool __result, CharDefine.CoordinateType type)
        {
            if (__result)
            {
                MoreAccessories.CharAdditionalData additionalData;
                if (MoreAccessories.self.accessoriesByChar.TryGetValue(__instance, out additionalData) == false)
                {
                    additionalData = new MoreAccessories.CharAdditionalData();
                    MoreAccessories.self.accessoriesByChar.Add(__instance, additionalData);
                }
                if (additionalData.rawAccessoriesInfos.TryGetValue(type, out additionalData.clothesInfoAccessory) == false)
                {
                    additionalData.clothesInfoAccessory = new List<CharFileInfoClothes.Accessory>();
                    additionalData.rawAccessoriesInfos[type] = additionalData.clothesInfoAccessory;
                }
                while (additionalData.infoAccessory.Count < additionalData.clothesInfoAccessory.Count)
                    additionalData.infoAccessory.Add(null);
                while (additionalData.objAccessory.Count < additionalData.clothesInfoAccessory.Count)
                    additionalData.objAccessory.Add(null);
                while (additionalData.objAcsMove.Count < additionalData.clothesInfoAccessory.Count)
                    additionalData.objAcsMove.Add(null);
                MoreAccessories.self.UpdateGUI();
            }
        }

    }
    [HarmonyPatch(typeof(CharBody))]
    [HarmonyPatch("ChangeAccessory")]
    [HarmonyPatch(new[] { typeof(bool) })]
    public class CharBody_ChangeAccessory_Patches
    {
        public static void Postfix(CharBody __instance, bool forceChange)
        {
            MoreAccessories.CharAdditionalData additionalData;
            if (MoreAccessories.self.accessoriesByChar.TryGetValue(__instance.chaFile, out additionalData))
            {
                int i;
                for (i = 0; i < additionalData.clothesInfoAccessory.Count; i++)
                {
                    CharFileInfoClothes.Accessory accessory = additionalData.clothesInfoAccessory[i];
                    ChangeAccessoryAsync(__instance, additionalData, i, accessory.type, accessory.id, accessory.parentKey, forceChange);
                }
                for (; i < additionalData.objAccessory.Count; i++)
                    CleanRemainingAccessory(additionalData, i);
            }
        }

        public static void ChangeAccessoryAsync(CharBody self, MoreAccessories.CharAdditionalData data, int _slotNo, int _acsType, int _acsId, string parentKey, bool forceChange = false)
        {
            ListTypeFbx ltf = null;
            bool load = true;
            bool release = true;
            int typeNum = Enum.GetNames(typeof(CharaListInfo.TypeAccessoryFbx)).Length;
            if (_acsType == -1 || !MathfEx.RangeEqualOn(0, _acsType, typeNum - 1))
            {
                release = true;
                load = false;
            }
            else
            {
                if (_acsId == -1)
                {
                    release = false;
                    load = false;
                }
                if (!forceChange && null != data.objAccessory[_slotNo] && _acsType == data.clothesInfoAccessory[_slotNo].type && _acsId == data.clothesInfoAccessory[_slotNo].id)
                {
                    load = false;
                    release = false;
                }
                if (_acsId != -1)
                {
                    Dictionary<int, ListTypeFbx> work = self.chaInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx) _acsType);
                    if (work == null)
                    {
                        release = true;
                        load = false;
                    }
                    else if (!work.TryGetValue(_acsId, out ltf))
                    {
                        release = true;
                        load = false;
                    }
                }
            }
            if (release)
            {
                if (!load)
                {
                    data.clothesInfoAccessory[_slotNo].MemberInitialize();
                }
                if (data.objAccessory[_slotNo])
                {
                    Object.Destroy(data.objAccessory[_slotNo]);
                    data.objAccessory[_slotNo] = null;
                    data.infoAccessory[_slotNo] = null;
                    CharInfo_ReleaseTagObject(data, _slotNo);
                    data.objAcsMove[_slotNo] = null;
                }
            }
            if (load)
            {
                byte weight = 0;
                Transform trfParent = null;
                if (ltf.Parent == "0")
                {
                    weight = 2;
                    trfParent = self.objTop.transform;
                }
                data.objAccessory[_slotNo] = (GameObject) self.CallPrivate("LoadCharaFbxData", typeof(CharaListInfo.TypeAccessoryFbx), _acsType, _acsId, "ca_slot" + (_slotNo + 10).ToString("00"), false, weight, trfParent, 0, false);
                if (data.objAccessory[_slotNo])
                {
                    ListTypeFbxComponent ltfComponent = data.objAccessory[_slotNo].GetComponent<ListTypeFbxComponent>();
                    ltf = (data.infoAccessory[_slotNo] = ltfComponent.ltfData);
                    data.clothesInfoAccessory[_slotNo].type = _acsType;
                    data.clothesInfoAccessory[_slotNo].id = ltf.Id;
                    data.objAcsMove[_slotNo] = data.objAccessory[_slotNo].transform.FindLoop("N_move");
                    CharInfo_CreateTagInfo(data, _slotNo, data.objAccessory[_slotNo]);
                }
                else
                {
                    data.clothesInfoAccessory[_slotNo].type = -1;
                    data.clothesInfoAccessory[_slotNo].id = 0;
                }
            }
            if (data.objAccessory[_slotNo])
            {
                CharClothes_ChangeAccessoryColor(data, _slotNo);
                if (string.IsNullOrEmpty(parentKey))
                {
                    parentKey = ltf.Parent;
                }
                CharClothes_ChangeAccessoryParent(self, data, _slotNo, parentKey);
                CharClothes_UpdateAccessoryMoveFromInfo(data, _slotNo);
            }
        }

        public static void CleanRemainingAccessory(MoreAccessories.CharAdditionalData data, int _slotNo)
        {
            if (data.objAccessory[_slotNo])
            {
                Object.Destroy(data.objAccessory[_slotNo]);
                data.objAccessory[_slotNo] = null;
                data.infoAccessory[_slotNo] = null;
                CharInfo_ReleaseTagObject(data, _slotNo);
                data.objAcsMove[_slotNo] = null;
            }
        }

        private static void CharInfo_CreateTagInfo(MoreAccessories.CharAdditionalData data, int key, GameObject objTag)
        {
            if (objTag == null)
                return;
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(objTag.transform);
            CharInfo_AddListToTag(data, key, findAssist.GetObjectFromTag("ObjColor"));
        }

        private static void CharInfo_ReleaseTagObject(MoreAccessories.CharAdditionalData data, int key)
        {
            if (data.charInfoDictTagObj.ContainsKey(key))
                data.charInfoDictTagObj[key].Clear();
        }

        public static List<GameObject> CharInfo_GetTagInfo(MoreAccessories.CharAdditionalData data, int key)
        {
            List<GameObject> collection;
            if (data.charInfoDictTagObj.TryGetValue(key, out collection))
            {
                return new List<GameObject>(collection);
            }
            return new List<GameObject>();
        }


        private static void CharInfo_AddListToTag(MoreAccessories.CharAdditionalData data, int key, List<GameObject> add)
        {
            if (add == null)
                return;
            List<GameObject> gameObjectList;
            if (data.charInfoDictTagObj.TryGetValue(key, out gameObjectList))
                gameObjectList.AddRange(add);
            else
                data.charInfoDictTagObj[key] = add;
        }

        public static void CharClothes_ChangeAccessoryColor(MoreAccessories.CharAdditionalData data, int slotNo)
        {
            List<GameObject> tagInfo = CharInfo_GetTagInfo(data, slotNo);
            ColorChange.SetHSColor(tagInfo, data.clothesInfoAccessory[slotNo].color, true, true, data.clothesInfoAccessory[slotNo].color2, true, true);
            ColorChange.SetHSColor(tagInfo, data.clothesInfoAccessory[slotNo].color, true, true, data.clothesInfoAccessory[slotNo].color2, true, true);
        }

        public static bool CharClothes_ChangeAccessoryParent(CharBody charBody, MoreAccessories.CharAdditionalData data, int slotNo, string parentStr)
        {
            GameObject gameObject = data.objAccessory[slotNo];
            if (null == gameObject)
            {
                return false;
            }
            ListTypeFbxComponent component = gameObject.GetComponent<ListTypeFbxComponent>();
            ListTypeFbx ltfData = component.ltfData;
            if ("0" == ltfData.Parent)
            {
                return false;
            }
            try
            {
                CharReference.RefObjKey key = (CharReference.RefObjKey)(int)Enum.Parse(typeof(CharReference.RefObjKey), parentStr);
                GameObject referenceInfo = charBody.chaInfo.GetReferenceInfo(key);
                if (null == referenceInfo)
                {
                    return false;
                }
                gameObject.transform.SetParent(referenceInfo.transform, false);
                data.clothesInfoAccessory[slotNo].parentKey = parentStr;
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }

        public static bool CharClothes_UpdateAccessoryMoveFromInfo(MoreAccessories.CharAdditionalData data, int slotNo)
        {
            GameObject gameObject = data.objAcsMove[slotNo];
            if (null == gameObject)
            {
                return false;
            }
            gameObject.transform.SetLocalPosition(data.clothesInfoAccessory[slotNo].addPos.x, data.clothesInfoAccessory[slotNo].addPos.y, data.clothesInfoAccessory[slotNo].addPos.z);
            gameObject.transform.SetLocalRotation(data.clothesInfoAccessory[slotNo].addRot.x, data.clothesInfoAccessory[slotNo].addRot.y, data.clothesInfoAccessory[slotNo].addRot.z);
            gameObject.transform.SetLocalScale(data.clothesInfoAccessory[slotNo].addScl.x, data.clothesInfoAccessory[slotNo].addScl.y, data.clothesInfoAccessory[slotNo].addScl.z);
            return true;
        }

    }

    [HarmonyPatch(typeof(SubMenuControl), "ChangeSubMenu", new []{typeof(string)})]
    public class SubMenuControl_ChangeSubMenu_Patches
    {
        public static bool Prefix(SubMenuControl __instance, string subMenuStr)
        {
            if (subMenuStr.StartsWith("SM_MoreAccessories_"))
            {
                int nowSubMenuTypeId = int.Parse(subMenuStr.Substring(19));
                if (nowSubMenuTypeId < MoreAccessories.self.charaMakerAdditionalData.clothesInfoAccessory.Count)
                {
                    bool sameSubMenu = __instance.nowSubMenuTypeStr == subMenuStr;
                    __instance.nowSubMenuTypeStr = subMenuStr;
                    __instance.nowSubMenuTypeId = (int)SubMenuControl.SubMenuType.SM_Delete + 1 + nowSubMenuTypeId;
                    for (int i = 0; i < __instance.smItem.Length; i++)
                        if (__instance.smItem[i] != null && !(null == __instance.smItem[i].objTop))
                            __instance.smItem[i].objTop.SetActive(false);
                    if (MoreAccessories.self.smItem != null)
                    {
                        if (null != __instance.textTitle)
                            __instance.textTitle.text = MoreAccessories.self.smItem.menuName;
                        if (null != MoreAccessories.self.smItem.objTop)
                        {
                            MoreAccessories.self.smItem.objTop.SetActive(true);
                            __instance.SetPrivate("objActiveSubItem", MoreAccessories.self.smItem.objTop);
                            if (null != __instance.rtfBasePanel)
                            {
                                RectTransform rectTransform = MoreAccessories.self.smItem.objTop.transform as RectTransform;
                                Vector2 sizeDelta = rectTransform.sizeDelta;
                                __instance.SetPrivate("sizeBasePanelHeight", sizeDelta.y);
                            }
                        }
                    }
                    SubMenuBase component = ((GameObject)__instance.GetPrivate("objActiveSubItem")).GetComponent<SubMenuBase>();
                    if (null != component)
                    {
                        component.SetCharaInfo(__instance.nowSubMenuTypeId, sameSubMenu);
                    }
                    int cosStateFromSelect = __instance.GetCosStateFromSelect();
                    if (cosStateFromSelect != -1 && __instance.customCtrlPanel && __instance.customCtrlPanel.autoClothesState)
                    {
                        __instance.customCtrlPanel.ChangeCosStateSub(cosStateFromSelect);
                    }
                }
                else
                {
                    MoreAccessories.self.UIFallbackToCoordList();
                    
                }
                return false;
            }
            if (__instance.nowSubMenuTypeStr.StartsWith("SM_MoreAccessories_"))
                MoreAccessories.self.smItem.objTop.SetActive(false);
            return true;
        }
    }
}
