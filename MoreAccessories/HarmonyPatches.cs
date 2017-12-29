using System;
using System.Collections.Generic;
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
                for (int i = 0; i < additionalData.clothesInfoAccessory.Count; i++)
                {
                    CharFileInfoClothes.Accessory accessory = additionalData.clothesInfoAccessory[i];
                    ChangeAccessoryAsync(__instance, additionalData, accessory, i, forceChange);
                }
            }
        }

        private static void ChangeAccessoryAsync(CharBody self, MoreAccessories.CharAdditionalData data, CharFileInfoClothes.Accessory accessory, int _slotNo, bool forceChange = false)
        {
            ListTypeFbx ltf = null;
            bool load = true;
            bool release = true;
            int typeNum = Enum.GetNames(typeof(CharaListInfo.TypeAccessoryFbx)).Length;
            if (accessory.type == -1 || !MathfEx.RangeEqualOn(0, accessory.type, typeNum - 1))
            {
                release = true;
                load = false;
            }
            else
            {
                if (accessory.id == -1)
                {
                    release = false;
                    load = false;
                }
                if (!forceChange && null != data.objAccessory[_slotNo] && accessory.type == data.clothesInfoAccessory[_slotNo].type && accessory.id == data.clothesInfoAccessory[_slotNo].id)
                {
                    load = false;
                    release = false;
                }
                if (accessory.id != -1)
                {
                    Dictionary<int, ListTypeFbx> work = self.chaInfo.ListInfo.GetAccessoryFbxList((CharaListInfo.TypeAccessoryFbx) accessory.type);
                    if (work == null)
                    {
                        release = true;
                        load = false;
                    }
                    else if (!work.TryGetValue(accessory.id, out ltf))
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
                if ((bool) data.objAccessory[_slotNo])
                {
                    Object.Destroy(data.objAccessory[_slotNo]);
                    data.objAccessory[_slotNo] = null;
                    data.infoAccessory[_slotNo] = null;
                    ReleaseTagObject(data, _slotNo);
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
                data.objAccessory[_slotNo] = (GameObject) self.CallPrivate("LoadCharaFbxData", typeof(CharaListInfo.TypeAccessoryFbx), accessory.type, accessory.id, "ca_slot" + (_slotNo + 10).ToString("00"), false, weight, trfParent, 0, false);
                if ((bool) data.objAccessory[_slotNo])
                {
                    ListTypeFbxComponent ltfComponent = data.objAccessory[_slotNo].GetComponent<ListTypeFbxComponent>();
                    ltf = (data.infoAccessory[_slotNo] = ltfComponent.ltfData);
                    data.clothesInfoAccessory[_slotNo].type = accessory.type;
                    data.clothesInfoAccessory[_slotNo].id = ltf.Id;
                    data.objAcsMove[_slotNo] = data.objAccessory[_slotNo].transform.FindLoop("N_move");
                    CreateTagInfo(data, _slotNo, data.objAccessory[_slotNo]);
                }
                else
                {
                    data.clothesInfoAccessory[_slotNo].type = -1;
                    data.clothesInfoAccessory[_slotNo].id = 0;
                }
            }
            if ((bool) data.objAccessory[_slotNo])
            {
                ChangeAccessoryColor(data, _slotNo);
                if (string.IsNullOrEmpty(accessory.parentKey))
                {
                    accessory.parentKey = ltf.Parent;
                }
                ChangeAccessoryParent(self, data, _slotNo, accessory.parentKey);
                UpdateAccessoryMoveFromInfo(data, _slotNo);
            }
        }

        private static void CreateTagInfo(MoreAccessories.CharAdditionalData data, int key, GameObject objTag)
        {
            if (objTag == null)
                return;
            FindAssist findAssist = new FindAssist();
            findAssist.Initialize(objTag.transform);
            AddListToTag(data, key, findAssist.GetObjectFromTag("ObjColor"));
        }

        private static void ReleaseTagObject(MoreAccessories.CharAdditionalData data, int key)
        {
            data.charInfoDictTagObj[key].Clear();
        }

        private static List<GameObject> GetTagInfo(MoreAccessories.CharAdditionalData data, int key)
        {
            List<GameObject> collection = null;
            if (data.charInfoDictTagObj.TryGetValue(key, out collection))
            {
                return new List<GameObject>(collection);
            }
            return null;
        }


        private static void AddListToTag(MoreAccessories.CharAdditionalData data, int key, List<GameObject> add)
        {
            if (add == null)
                return;
            List<GameObject> gameObjectList;
            if (data.charInfoDictTagObj.TryGetValue(key, out gameObjectList))
                gameObjectList.AddRange(add);
            else
                data.charInfoDictTagObj[key] = add;
        }

        private static void ChangeAccessoryColor(MoreAccessories.CharAdditionalData data, int slotNo)
        {
            List<GameObject> tagInfo = GetTagInfo(data, slotNo);
            ColorChange.SetHSColor(tagInfo, data.clothesInfoAccessory[slotNo].color, true, true, data.clothesInfoAccessory[slotNo].color2, true, true);
        }

        private static bool ChangeAccessoryParent(CharBody charBody, MoreAccessories.CharAdditionalData data, int slotNo, string parentStr)
        {
            if (!MathfEx.RangeEqualOn(0, slotNo, 9))
            {
                return false;
            }
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

        private static bool UpdateAccessoryMoveFromInfo(MoreAccessories.CharAdditionalData data, int slotNo)
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
}
