using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
#if HONEYSELECT
using Harmony;
using IllusionPlugin;
#elif AISHOUJO
using AIChara;
using BepInEx;
using HarmonyLib;
#endif
using ToolBox;
using ToolBox.Extensions;
using UnityEngine;

namespace BonesFramework
{
#if AISHOUJO
    [BepInPlugin(_guid, _name, _version)]
#endif
    public class BonesFramework : GenericPlugin
#if HONEYSELECT
        , IEnhancedPlugin
#endif
    {
        #region Private Types
        private class AdditionalObjectData
        {
            public GameObject parent;
            public List<GameObject> objects = new List<GameObject>();
        }
        #endregion

        #region Private Variables
        private static GameObject _currentLoadingCloth;
        private static readonly HashSet<string> _currentAdditionalRootBones = new HashSet<string>();
        private static Transform _currentTransformParent;
        private static readonly Dictionary<GameObject, AdditionalObjectData> _currentAdditionalObjects = new Dictionary<GameObject, AdditionalObjectData>();
        private const string _name = "BonesFramework";
#if HONEYSELECT
        private const string _version = "1.1.0";
#elif AISHOUJO
        private const string _version = "1.0.0";
#endif
        private const string _guid = "com.joan6694.illusionplugins.bonesframework";
        #endregion

#if HONEYSELECT
        public override string Name { get { return _name; } }
        public override string Version { get { return _version; } }
        public override string[] Filter { get { return new[] {"HoneySelect_64", "HoneySelect_32", "StudioNEO_64", "StudioNEO_32", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }
#endif

        #region Unity Methods
        protected override void Awake()
        {
            base.Awake();

            UnityEngine.Debug.Log("BonesFramework: Trying to patch methods...");
            try
            {
                var harmony = HarmonyExtensions.PatchAll(_guid);
#if HONEYSELECT
                harmony.Patch(typeof(CharBody).GetCoroutineMethod("LoadCharaFbxDataAsync"), 
#elif AISHOUJO
                harmony.Patch(typeof(ChaControl).GetCoroutineMethod("LoadCharaFbxDataAsync"),
#endif
                              null,
                              null,
                              new HarmonyMethod(typeof(BonesFramework), nameof(CharBody_LoadCharaFbxDataAsync_Transpiler), new[] { typeof(IEnumerable<CodeInstruction>) }));
                harmony.Patch(AccessTools.Method(typeof(AssignedAnotherWeights), "AssignedWeights", new[] {typeof(GameObject), typeof(string), typeof(Transform)}),
                              new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeights_Prefix)),
                              new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeights_Postfix)));
                harmony.Patch(AccessTools.Method(typeof(AssignedAnotherWeights), "AssignedWeightsLoop", new[] {typeof(Transform), typeof(Transform)}),
                              new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeightsLoop_Prefix)));

#if AISHOUJO
                harmony.Patch(AccessTools.Method(typeof(AssignedAnotherWeights), "AssignedWeightsAndSetBounds", new[] {typeof(GameObject), typeof(string), typeof(Bounds), typeof(Transform)}),
                              new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeights_Prefix)),
                              new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeights_Postfix)));
                harmony.Patch(AccessTools.Method(typeof(AssignedAnotherWeights), "AssignedWeightsAndSetBoundsLoop", new[] {typeof(Transform), typeof(Bounds), typeof(Transform)}),
                              new HarmonyMethod(typeof(BonesFramework), nameof(AssignedAnotherWeights_AssignedWeightsAndSetBoundsLoop_Prefix)));
#endif
                UnityEngine.Debug.Log("BonesFramework: Patch successful!");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("BonesFramework: Couldn't patch properly:\n" + e);
            }
        }
        #endregion

        #region Patches
        private static void AssignedAnotherWeights_AssignedWeights_Prefix(GameObject obj)
        {
            _currentTransformParent = obj.transform.parent.Find("p_cf_anim");
            if (_currentTransformParent == null)
            {
                _currentTransformParent = obj.transform.parent.Find("p_cm_anim");
                if (_currentTransformParent == null)
                    _currentTransformParent = obj.transform.parent;
            }
        }

        private static void AssignedAnotherWeights_AssignedWeights_Postfix()
        {
            AdditionalObjectData data;
            if (_currentLoadingCloth != null && _currentAdditionalObjects.TryGetValue(_currentLoadingCloth, out data))
            {
                data.parent = _currentLoadingCloth.transform.parent.gameObject;
                IEnumerable<DynamicBoneCollider> colliders = data.objects.SelectMany(go => go.GetComponentsInChildren<DynamicBoneCollider>(true));
                foreach (DynamicBone bone in data.parent.GetComponentsInChildren<DynamicBone>(true))
                {
                    if (bone.m_Colliders != null)
                    {
                        foreach (DynamicBoneCollider collider in colliders)
                            bone.m_Colliders.Add(collider);
                    }
                }
            }

            _currentLoadingCloth = null;
        }

        private static bool AssignedAnotherWeights_AssignedWeightsLoop_Prefix(AssignedAnotherWeights __instance, Transform t, Transform rootBone)
        {
            SkinnedMeshRenderer component = t.GetComponent<SkinnedMeshRenderer>();
            if (component)
            {
                int num = component.bones.Length;
                Transform[] array = new Transform[num];
                GameObject gameObject = null;
                for (int i = 0; i < num; i++)
                {
                    Transform bone = component.bones[i];
                    if (__instance.dictBone.TryGetValue(bone.name, out gameObject))
                        array[i] = gameObject.transform;
                    else if (_currentAdditionalRootBones.Count != 0 &&
                             _currentAdditionalRootBones.Any(bone.IsChildOf))
                        array[i] = bone;
                }

                component.bones = array;
                if (rootBone)
                    component.rootBone = rootBone;
                else if (component.rootBone && __instance.dictBone.TryGetValue(component.rootBone.name, out gameObject))
                    component.rootBone = gameObject.transform;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                Transform obj = t.GetChild(i);
                if (_currentAdditionalRootBones.Count != 0 && _currentAdditionalRootBones.Contains(obj.name))
                {
                    Transform parent = _currentTransformParent.FindDescendant(obj.parent.name);
                    Vector3 localPos = obj.localPosition;
                    Quaternion localRot = obj.localRotation;
                    Vector3 localScale = obj.localScale;
                    obj.SetParent(parent);
                    obj.localPosition = localPos;
                    obj.localRotation = localRot;
                    obj.localScale = localScale;
                    _currentAdditionalObjects[_currentLoadingCloth].objects.Add(obj.gameObject);
                    --i;
                }
                else
                    AssignedAnotherWeights_AssignedWeightsLoop_Prefix(__instance, obj, rootBone);
            }

            return false;
        }

#if AISHOUJO
        private static bool AssignedAnotherWeights_AssignedWeightsAndSetBoundsLoop_Prefix(AssignedAnotherWeights __instance, Transform t, Bounds bounds, Transform rootBone)
        {
            SkinnedMeshRenderer component = t.GetComponent<SkinnedMeshRenderer>();
            if (component)
            {
                int num = component.bones.Length;
                Transform[] array = new Transform[num];
                GameObject gameObject = null;
                for (int i = 0; i < num; i++)
                {
                    Transform bone = component.bones[i];
                    if (__instance.dictBone.TryGetValue(bone.name, out gameObject))
                        array[i] = gameObject.transform;
                    else if (_currentAdditionalRootBones.Count != 0 &&
                             _currentAdditionalRootBones.Any(bone.IsChildOf))
                        array[i] = bone;
                }

                component.bones = array;
                component.localBounds = bounds;
                Cloth component2 = component.gameObject.GetComponent<Cloth>();
                if (rootBone && component2 != null)
                    component.rootBone = rootBone;
                else if (component.rootBone && __instance.dictBone.TryGetValue(component.rootBone.name, out gameObject))
                    component.rootBone = gameObject.transform;
            }

            for (int i = 0; i < t.childCount; i++)
            {
                Transform obj = t.GetChild(i);
                if (_currentAdditionalRootBones.Count != 0 && _currentAdditionalRootBones.Contains(obj.name))
                {
                    Transform parent = _currentTransformParent.FindDescendant(obj.parent.name);
                    Vector3 localPos = obj.localPosition;
                    Quaternion localRot = obj.localRotation;
                    Vector3 localScale = obj.localScale;
                    obj.SetParent(parent);
                    obj.localPosition = localPos;
                    obj.localRotation = localRot;
                    obj.localScale = localScale;
                    _currentAdditionalObjects[_currentLoadingCloth].objects.Add(obj.gameObject);
                    --i;
                }
                else
                    AssignedAnotherWeights_AssignedWeightsAndSetBoundsLoop_Prefix(__instance, obj, bounds, rootBone);
            }

            return false;
        }
#endif

        private static IEnumerable<CodeInstruction> CharBody_LoadCharaFbxDataAsync_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.ToString().Contains("AddLoadAssetBundle")) //There's probably something better but idk m8
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(BonesFramework).GetMethod(nameof(CharBody_LoadCharaFbxDataAsync_Injected), BindingFlags.NonPublic | BindingFlags.Static));
                    set = true;
                }
            }
        }

        private static void CharBody_LoadCharaFbxDataAsync_Injected(object self)
        {
#if HONEYSELECT
            _currentLoadingCloth = (GameObject)self.GetPrivate("<newObj>__6");
#elif AISHOUJO
            _currentLoadingCloth = (GameObject)self.GetPrivate("$locvar2").GetPrivate("newObj");
#endif
            _currentAdditionalRootBones.Clear();
            if (_currentLoadingCloth == null)
                return;

#if HONEYSELECT
            ListTypeFbx ltf = (ListTypeFbx)self.GetPrivate("<ltf>__3");
            string assetBundlePath = ltf.ABPath;
            string assetName = (string)self.GetPrivate("<assetName>__5");
            string manifest = ltf.Manifest;
#elif AISHOUJO
            string assetBundlePath = (string)self.GetPrivate("<assetBundleName>__0");
            string assetName = (string)self.GetPrivate("<assetName>__0");
            string manifest = (string)self.GetPrivate("<manifestName>__0");
#endif
            TextAsset ta = CommonLib.LoadAsset<TextAsset>(assetBundlePath, "additional_bones", true, manifest);
            if (ta == null)
                return;

            UnityEngine.Debug.Log("BonesFramework: Loaded additional_bones TextAsset from " + assetBundlePath);
            _currentAdditionalObjects.Add(_currentLoadingCloth, new AdditionalObjectData());
            OnDestroyTrigger destroyTrigger = _currentLoadingCloth.AddComponent<OnDestroyTrigger>();
            destroyTrigger.onDestroy = (go) =>
            {
                AdditionalObjectData data = _currentAdditionalObjects[go];
                DynamicBone[] dbs = data.parent.GetComponentsInChildren<DynamicBone>();
                foreach (GameObject o in data.objects)
                {
                    DynamicBoneCollider[] colliders = o.GetComponentsInChildren<DynamicBoneCollider>(true);
                    foreach (DynamicBoneCollider collider in colliders)
                    {
                        foreach (DynamicBone dynamicBone in dbs)
                        {
                            int index = dynamicBone.m_Colliders.FindIndex(c => c == collider);
                            if (index != -1)
                                dynamicBone.m_Colliders.RemoveAt(index);
                        }
                    }
                    GameObject.Destroy(o);
                }
                _currentAdditionalObjects.Remove(go);
            };

            string[] lines = ta.text.Split(new[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string[] cells = line.Split('\t');
                if (!cells[0].Equals(assetName))
                    continue;
                UnityEngine.Debug.Log("BonesFramework: Found matching line for asset " + assetName + "\n" + line);
                for (int i = 1; i < cells.Length; i++)
                    _currentAdditionalRootBones.Add(cells[i]);

                destroyTrigger.onStart = (self2) =>
                {
                    self2.ExecuteDelayed(() =>
                    {
                        foreach (DynamicBone dynamicBone in self2.transform.parent.GetComponentsInChildren<DynamicBone>(true))
                        {
                            dynamicBone.InitTransforms();
                            dynamicBone.SetupParticles();
                        }
                    });
                };
                break;
            }
        }
        #endregion
    }
}