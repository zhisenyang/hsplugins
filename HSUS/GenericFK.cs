using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using HSUS;
using UnityEngine;

namespace Studio
{
    [HarmonyPatch(typeof(AddObjectItem), "Load", new[] { typeof(OIItemInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
    public class AddObjectItem_Load_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS.self.enableGenericFK;
        }

        public static void Postfix(OCIItem __result, OIItemInfo _info, ObjectCtrlInfo _parent, TreeNodeObject _parentNode, bool _addInfo, int _initialPosition)
        {
            if (__result.itemFKCtrl != null || __result.objectItem.transform.childCount <= 0)
                return;
            __result.itemFKCtrl = __result.objectItem.AddComponent<ItemFKCtrl>();
            __result.itemFKCtrl.InitBone(__result, null, _addInfo);
            __result.dynamicBones = __result.objectItem.GetComponentsInChildren<DynamicBone>(true);
        }
    }

    [HarmonyPatch(typeof(ItemFKCtrl), "InitBone", new[] { typeof(OCIItem), typeof(Info.ItemLoadInfo), typeof(bool) })]
    public class ItemFKCtrl_InitBone_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS.self.enableGenericFK;
        }

        public static bool Prefix(ItemFKCtrl __instance, OCIItem _ociItem, Info.ItemLoadInfo _loadInfo, bool _isNew)
        {
            if (_loadInfo != null && _loadInfo.bones.Count > 0)
                return true;
            Transform transform = _ociItem.objectItem.transform;
            HashSet<Transform> activeBones = new HashSet<Transform>();
            Renderer[] children = transform.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < children.Length; index++)
            {
                Renderer renderer = children[index];
                SkinnedMeshRenderer skinnedMeshRenderer;
                if ((skinnedMeshRenderer = renderer as SkinnedMeshRenderer) != null)
                {
                    Mesh mesh = skinnedMeshRenderer.sharedMesh;
                    for (int j = 0; j < mesh.boneWeights.Length; j++)
                    {
                        BoneWeight weight = mesh.boneWeights[j];
                        Transform bone;
                        if (weight.boneIndex0 >= 0)
                        {
                            bone = skinnedMeshRenderer.bones[weight.boneIndex0];
                            if (activeBones.Contains(bone) == false)
                                activeBones.Add(bone);
                        }
                        if (weight.boneIndex1 >= 0)
                        {
                            bone = skinnedMeshRenderer.bones[weight.boneIndex1];
                            if (activeBones.Contains(bone) == false)
                                activeBones.Add(bone);
                        }
                        if (weight.boneIndex2 >= 0)
                        {
                            bone = skinnedMeshRenderer.bones[weight.boneIndex2];
                            if (activeBones.Contains(bone) == false)
                                activeBones.Add(bone);
                        }
                        if (weight.boneIndex3 >= 0)
                        {
                            bone = skinnedMeshRenderer.bones[weight.boneIndex3];
                            if (activeBones.Contains(bone) == false)
                                activeBones.Add(bone);
                        }
                    }
                }
                else if (renderer is MeshRenderer)
                {
                    activeBones.Add(renderer.transform);
                }
            }
            _ociItem.listBones = new List<OCIChar.BoneInfo>();
            object listBones = __instance.GetPrivate("listBones");
            Type type = listBones.GetType().GetGenericArguments()[0];
            ConstructorInfo ctor = type.GetConstructor(new[] { typeof(GameObject), typeof(ChangeAmount), typeof(bool) });
            int i = 0;
            Recurse(transform, t =>
            {
                if (t.gameObject.isStatic || activeBones.Contains(t) == false)
                    return;
                OIBoneInfo oIBoneInfo = null;
                string path = t.GetPathFrom(transform);
                if (!_ociItem.itemInfo.bones.TryGetValue(path, out oIBoneInfo))
                {
                    oIBoneInfo = new OIBoneInfo(Studio.GetNewIndex())
                    {
                        changeAmount =
                        {
                            pos = t.localPosition,
                            rot = t.localEulerAngles,
                            scale = t.localScale
                        }
                    };
                    _ociItem.itemInfo.bones.Add(path, oIBoneInfo);
                }
                GuideObject guideObject = Singleton<GuideObjectManager>.Instance.Add(t, oIBoneInfo.dicKey);
                guideObject.enablePos = false;
                guideObject.enableScale = false;
                guideObject.enableMaluti = false;
                guideObject.calcScale = false;
                guideObject.scaleRate = 0.5f;
                guideObject.scaleRot = 0.025f;
                guideObject.scaleSelect = 0.05f;
                guideObject.parentGuide = _ociItem.guideObject;
                _ociItem.listBones.Add(new OCIChar.BoneInfo(guideObject, oIBoneInfo));
                guideObject.SetActive(false, true);

                object instance = ctor.Invoke(new object[] { t.gameObject, oIBoneInfo.changeAmount, _isNew });
                listBones.CallPrivate("Add", instance);
                ++i;
            });
            __instance.GetType().GetProperty("count", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy).SetValue(__instance, i, null);
            if (_isNew)
            {
                __instance.ExecuteDelayed(() =>
                {
                    _ociItem.ActiveFK(false);
                });
            }
            else
            {
                __instance.ExecuteDelayed(() =>
                {
                    _ociItem.ActiveFK(_ociItem.itemFKCtrl.enabled);
                });
            }
            return false;
        }


        private static void Recurse(Transform t, Action<Transform> action)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                action(t.GetChild(i));
                Recurse(t.GetChild(i), action);
            }
        }
        public static void Postfix(ItemFKCtrl __instance, OCIItem _ociItem, Info.ItemLoadInfo _loadInfo, bool _isNew)
        {
            IList listBones = (IList)__instance.GetPrivate("listBones");
            if (listBones.Count > 0)
            {
                FieldInfo fi = listBones[0].GetType().GetField("changeAmount", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                MethodInfo mi = listBones[0].GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                foreach (object bone in listBones)
                {
                    ChangeAmount ca = (ChangeAmount)fi.GetValue(bone);
                    ca.onChangeRot = (Action)Delegate.CreateDelegate(typeof(Action), bone, mi);
                    ca.onChangeRot();
                }
            }
        }
    }

    [HarmonyPatch(typeof(ItemFKCtrl), "LateUpdate")]
    public class ItemFKCtrl_LateUpdate_Patches
    {
        public static bool Prepare()
        {
            return HSUS.HSUS.self.enableGenericFK;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
