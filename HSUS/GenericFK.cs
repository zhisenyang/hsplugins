#if HONEYSELECT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using HSUS;
using IllusionUtility.GetUtility;
using ToolBox;
using Unity.Linq;
using UnityEngine;

namespace Studio
{
    [HarmonyPatch(typeof(AddObjectItem), "Load", new[] { typeof(OIItemInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
    public class AddObjectItem_Load_Patches
    {
        private static Type _itemFKCtrl;
        public static bool Prepare()
        {
            _itemFKCtrl = Type.GetType("Studio.ItemFKCtrl,Assembly-CSharp");
            return HSUS.HSUS._self._enableGenericFK && HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo && _itemFKCtrl != null;
        }

        public static bool Prefix(OIItemInfo _info, ObjectCtrlInfo _parent, TreeNodeObject _parentNode, bool _addInfo, int _initialPosition, ref OCIItem __result)
        {
            OCIItem ociitem = new OCIItem();
            Info.ItemLoadInfo loadInfo = GetLoadInfo(_info.no);
            if (loadInfo == null)
                loadInfo = GetLoadInfo(0);
            ociitem.objectInfo = _info;
            GameObject gameObject = CommonLib.LoadAsset<GameObject>(loadInfo.bundlePath, loadInfo.fileName, true, loadInfo.manifest);
            if (gameObject == null)
            {
                Debug.LogError($"読み込み失敗 : {loadInfo.manifest} : {loadInfo.bundlePath} : {loadInfo.fileName}");
                Studio.DeleteIndex(_info.dicKey);
                __result = null;
                return false;
            }
            gameObject.transform.SetParent(Singleton<Manager.Scene>.Instance.commonSpace.transform);
            ociitem.objectItem = gameObject;
            ociitem.arrayRender = gameObject.GetComponentsInChildren<Renderer>().Where(v => v.enabled).ToArray();
            ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>();
            if (!componentsInChildren.IsNullOrEmpty())
            {
                ociitem.arrayParticle = componentsInChildren.Where(v => v.isPlaying).ToArray();
            }
            GuideObject guideObject = Singleton<GuideObjectManager>.Instance.Add(gameObject.transform, _info.dicKey);
            guideObject.isActive = false;
            guideObject.scaleSelect = 0.1f;
            guideObject.scaleRot = 0.05f;
            GuideObject guideObject2 = guideObject;
            guideObject2.isActiveFunc = (GuideObject.IsActiveFunc)Delegate.Combine(guideObject2.isActiveFunc, new GuideObject.IsActiveFunc(ociitem.OnSelect));
            guideObject.enableScale = loadInfo.isScale;
            ociitem.guideObject = guideObject;
            if (!loadInfo.childRoot.IsNullOrEmpty())
            {
                GameObject gameObject2 = gameObject.transform.FindLoop(loadInfo.childRoot);
                if (gameObject2)
                    ociitem.childRoot = gameObject2.transform;
            }
            if (ociitem.childRoot == null)
                ociitem.childRoot = gameObject.transform;
            ociitem.animator = gameObject.GetComponent<Animator>();
            if (ociitem.animator)
                ociitem.animator.enabled = loadInfo.isAnime;
            if (loadInfo.isColor)
            {
                List<KeyValuePair<string, OCIItem.ColorTargetInfo>> listWork = GetTarget(gameObject, loadInfo.colorTarget);
                if (loadInfo.isColor2)
                {
                    foreach (KeyValuePair<string, OCIItem.ColorTargetInfo> keyValuePair in listWork.Where(v => loadInfo.color2Target.Any(s => s == v.Key)))
                        keyValuePair.Value.isColor2 = true;
                    List<KeyValuePair<string, OCIItem.ColorTargetInfo>> target = GetTarget(gameObject, loadInfo.color2Target.Where(s => listWork.All(v => v.Key != s)).ToArray());
                    if (!target.IsNullOrEmpty())
                    {
                        int count = target.Count;
                        for (int i = 0; i < count; i++)
                        {
                            target[i].Value.isColor2 = true;
                            target[i].Value.isColor2Only = true;
                        }
                        listWork.AddRange(target);
                    }
                }
                ociitem.colorTargets = (from v in listWork
                                        select v.Value).ToArray();
            }
            if (_addInfo)
                Studio.AddInfo(_info, ociitem);
            else
                Studio.AddObjectCtrlInfo(ociitem);
            TreeNodeObject parent = !(_parentNode != null) ? (_parent == null ? null : _parent.treeNodeObject) : _parentNode;
            TreeNodeObject treeNodeObject = Studio.AddNode(loadInfo.name, parent);
            treeNodeObject.treeState = _info.treeState;
            TreeNodeObject treeNodeObject2 = treeNodeObject;
            treeNodeObject2.onVisible = (TreeNodeObject.OnVisibleFunc)Delegate.Combine(treeNodeObject2.onVisible, new TreeNodeObject.OnVisibleFunc(ociitem.OnVisible));
            treeNodeObject.enableVisible = true;
            treeNodeObject.visible = _info.visible;
            guideObject.guideSelect.treeNodeObject = treeNodeObject;
            ociitem.treeNodeObject = treeNodeObject;
            if (!loadInfo.bones.IsNullOrEmpty())
            {
                ociitem.itemFKCtrl = gameObject.AddComponent<ItemFKCtrl>();
                ociitem.itemFKCtrl.InitBone(ociitem, loadInfo, _addInfo);
                ociitem.dynamicBones = ociitem.objectItem.GetComponentsInChildren<DynamicBone>(true);
            }
            else if (ociitem.objectItem != null && ociitem.objectItem.transform.childCount != 0)
            {
                ociitem.itemFKCtrl = gameObject.AddComponent<ItemFKCtrl>();
                ociitem.itemFKCtrl.InitBone(ociitem, null, _addInfo);
                ociitem.dynamicBones = ociitem.objectItem.GetComponentsInChildren<DynamicBone>(true);
            }
            else
                ociitem.itemFKCtrl = null;
            if (_initialPosition == 1)
                _info.changeAmount.pos = Singleton<Studio>.Instance.cameraCtrl.targetPos;
            _info.changeAmount.OnChange();
            Studio.AddCtrlInfo(ociitem);
            if (_parent != null)
            {
                _parent.OnLoadAttach(!(_parentNode != null) ? _parent.treeNodeObject : _parentNode, ociitem);
            }
            if (ociitem.animator)
            {
                ociitem.animator.speed = _info.animeSpeed;
                if (_info.animeNormalizedTime != 0f && ociitem.animator.layerCount != 0)
                {
                    ociitem.animator.Update(1f);
                    AnimatorStateInfo currentAnimatorStateInfo = ociitem.animator.GetCurrentAnimatorStateInfo(0);
                    ociitem.animator.Play(currentAnimatorStateInfo.shortNameHash, 0, _info.animeNormalizedTime);
                }
            }
            ociitem.UpdateColor();
            ociitem.ActiveFK(_info.enableFK);
            __result = ociitem;
            return false;
        }

        private static Info.ItemLoadInfo GetLoadInfo(int _no)
        {
            Info.ItemLoadInfo result = null;
            if (!Singleton<Info>.Instance.dicItemLoadInfo.TryGetValue(_no, out result))
            {
                Debug.LogWarning($"存在しない番号[{_no}]");
                return null;
            }
            return result;
        }

        private static List<KeyValuePair<string, OCIItem.ColorTargetInfo>> GetTarget(GameObject _obj, string[] _target)
        {
            if (_target.IsNullOrEmpty())
            {
                return null;
            }
            return (from v in (from g in _obj.DescendantsAndSelf(null)
                    where _target.Any(s => s == g.name)
                    select g).OfComponent<Renderer>()
                select new KeyValuePair<string, OCIItem.ColorTargetInfo>(v.name, new OCIItem.ColorTargetInfo(v))).ToList();
        }

    }

    [HarmonyPatch]
    public class ItemFKCtrl_InitBone_Patches
    {
        private static ConstructorInfo _targetInfoConstructor;
        private static PropertyInfo _countProperty;

        private static bool Prepare()
        {
            if (HSUS.HSUS._self._binary == HSUS.HSUS.Binary.Neo && HSUS.HSUS._self._enableGenericFK && Type.GetType("Studio.ItemFKCtrl,Assembly-CSharp") != null)
            {
                _targetInfoConstructor = Type.GetType("Studio.ItemFKCtrl+TargetInfo,Assembly-CSharp").GetConstructor(new[] {typeof(GameObject), typeof(ChangeAmount), typeof(bool)});
                _countProperty = Type.GetType("Studio.ItemFKCtrl,Assembly-CSharp").GetProperty("count", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                return true;
            }
            return false;
        }

        private static MethodInfo TargetMethod()
        {
            return AccessTools.Method(Type.GetType("Studio.ItemFKCtrl,Assembly-CSharp"), "InitBone", new[] {typeof(OCIItem), typeof(Info.ItemLoadInfo), typeof(bool)});
        }

        public static bool Prefix(object __instance, OCIItem _ociItem, Info.ItemLoadInfo _loadInfo, bool _isNew, object ___listBones)
        {
            if (_loadInfo != null && _loadInfo.bones.Count > 0)
                return true;
            Transform transform = _ociItem.objectItem.transform;
            HashSet<Transform> activeBones = new HashSet<Transform>();
            foreach (MeshRenderer renderer in transform.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (activeBones.Contains(renderer.transform) == false)
                {
                    if (renderer.name.Substring(0, renderer.name.Length - 1).EndsWith("MeshPart") == false)
                    {
                        if (renderer.transform != transform)
                            activeBones.Add(renderer.transform);
                    }
                    else if (activeBones.Contains(renderer.transform.parent) == false)
                    {
                        if (renderer.transform.parent != transform)
                            activeBones.Add(renderer.transform.parent);
                    }
                }
            }
            foreach (SkinnedMeshRenderer renderer in transform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                foreach (Transform bone in renderer.bones)
                {
                    if (bone == null || activeBones.Contains(bone) || bone == transform)
                        continue;
                    activeBones.Add(bone);
                }

            }
            _ociItem.listBones = new List<OCIChar.BoneInfo>();
            IList listBones = (IList)___listBones;
            int i = 0;
            object[] constructorParams = new object[3];
            foreach (Transform t in activeBones)
            {
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
                _ociItem.listBones.Add(
                                       new OCIChar.BoneInfo(
                                                            guideObject,
                                                            oIBoneInfo
#if KOIKATSU
                                                            , i
#endif
                                                           )
                                      );
                guideObject.SetActive(false, true);

                constructorParams[0] = t.gameObject;
                constructorParams[1] = oIBoneInfo.changeAmount;
                constructorParams[2] = _isNew;
                object instance = _targetInfoConstructor.Invoke(constructorParams);
                listBones.Add(instance);
                ++i;
            }
            _countProperty.SetValue(__instance, i, null);
            return false;
        }
    }
}
#endif