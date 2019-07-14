using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using IllusionPlugin;
using ToolBox;
using UnityEngine;

namespace BonesFramework
{
    public class BonesFramework : IEnhancedPlugin
    {
        #region Private Variables
        internal static GameObject _currentLoadingCloth;
        internal static readonly HashSet<string> _currentAdditionalRootBones = new HashSet<string>();
        internal static Transform _currentTransformParent;
        internal static Routines _routines;
        internal static readonly Dictionary<GameObject, List<GameObject>> _currentAdditionalObjects = new Dictionary<GameObject, List<GameObject>>();
        #endregion

        #region Public Accessors
        public string Name { get { return "BonesFramework"; } }
        public string Version { get { return "1.1.0"; } }
        public string[] Filter { get { return new[] {"HoneySelect_64", "HoneySelect_32", "StudioNEO_64", "StudioNEO_32", "Honey Select Unlimited_64", "Honey Select Unlimited_32" }; } }
        #endregion

        #region Unity Methods
        public void OnApplicationStart()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.illusionplugins.bonesframework");
            if (CharBody_LoadCharaFbxDataAsync_Patches.ManualPatch(harmony))
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            else
                UnityEngine.Debug.LogError("BonesFramework: Couldn't patch properly, features won't work.");
        }

        public void OnApplicationQuit()
        {
        }

        public void OnLevelWasLoaded(int level)
        {
            _routines = new GameObject("BonesFramework", typeof(Routines)).GetComponent<Routines>();
        }

        public void OnLevelWasInitialized(int level)
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }
        #endregion
    }

    [HarmonyPatch(typeof(AssignedAnotherWeights), "AssignedWeights", new[] {typeof(GameObject), typeof(string), typeof(Transform)})]
    static class AssignedAnotherWeights_AssignedWeights_Patches
    {
        private static void Prefix(GameObject obj)
        {
            BonesFramework._currentTransformParent = obj.transform.parent.Find("p_cf_anim");
            if (BonesFramework._currentTransformParent == null)
            {
                BonesFramework._currentTransformParent = obj.transform.parent.Find("p_cm_anim");
                if (BonesFramework._currentTransformParent == null)
                    BonesFramework._currentTransformParent = obj.transform.parent;
            }
        }

        private static void Postfix()
        {
            List<GameObject> additionalObjects;
            if (BonesFramework._currentLoadingCloth != null && BonesFramework._currentAdditionalObjects.TryGetValue(BonesFramework._currentLoadingCloth, out additionalObjects))
            {
                IEnumerable<DynamicBoneCollider> colliders = additionalObjects.SelectMany(go => go.GetComponentsInChildren<DynamicBoneCollider>(true));
                foreach (DynamicBone bone in BonesFramework._currentLoadingCloth.transform.parent.GetComponentsInChildren<DynamicBone>(true))
                {
                    if (bone.m_Colliders != null)
                    {
                        foreach (DynamicBoneCollider collider in colliders)
                            bone.m_Colliders.Add(collider);
                    }
                }
            }
            BonesFramework._currentLoadingCloth = null;
        }
    }
    [HarmonyPatch(typeof(AssignedAnotherWeights), "AssignedWeightsLoop", new[] {typeof(Transform), typeof(Transform)})]
    static class AssignedAnotherWeights_AssignedWeightsLoop_Patches
    {
        private static bool Prefix(AssignedAnotherWeights __instance, Transform t, Transform rootBone)
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
                    else if (BonesFramework._currentAdditionalRootBones.Count != 0 &&
                             BonesFramework._currentAdditionalRootBones.Any(bone.IsChildOf))
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
                if (BonesFramework._currentAdditionalRootBones.Count != 0 && BonesFramework._currentAdditionalRootBones.Contains(obj.name))
                {
                    Transform parent = BonesFramework._currentTransformParent.FindDescendant(obj.parent.name);
                    Vector3 localPos = obj.localPosition;
                    Quaternion localRot = obj.localRotation;
                    Vector3 localScale = obj.localScale;
                    obj.SetParent(parent);
                    obj.localPosition = localPos;
                    obj.localRotation = localRot;
                    obj.localScale = localScale;
                    BonesFramework._currentAdditionalObjects[BonesFramework._currentLoadingCloth].Add(obj.gameObject);
                    --i;
                }
                else
                    Prefix(__instance, obj, rootBone);
            }
            return false;
        }
    }

    static class CharBody_LoadCharaFbxDataAsync_Patches
    {
        internal static bool ManualPatch(HarmonyInstance harmony)
        {
            Type t = null;
            UnityEngine.Debug.Log("BonesFramework: Trying to patch methods...");
            foreach (Type type in typeof(CharBody).GetNestedTypes(BindingFlags.NonPublic))
            {
                if (type.FullName.StartsWith("CharBody+<LoadCharaFbxDataAsync>"))
                {
                    UnityEngine.Debug.Log(type);
                    t = type;
                    break;
                }
            }
            if (t != null)
            {
                harmony.Patch(t.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance), null, null, new HarmonyMethod(typeof(CharBody_LoadCharaFbxDataAsync_Patches), nameof(Transpiler), new[] {typeof(IEnumerable<CodeInstruction>)}));
                UnityEngine.Debug.Log("BonesFramework: Patch successful " + t);
                return true;
            }
            UnityEngine.Debug.LogError("BonesFramework: Patch failed!");
            return false;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool set = false;
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction inst = instructionsList[i];
                yield return inst;
                if (set == false && inst.ToString().Equals("callvirt Void AddLoadAssetBundle(System.String)"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(CharBody_LoadCharaFbxDataAsync_Patches).GetMethod(nameof(Injected), BindingFlags.NonPublic | BindingFlags.Static));
                    set = true;
                }
            }
        }

        private static void Injected(object self)
        {
            BonesFramework._currentLoadingCloth = (GameObject)self.GetPrivate("<newObj>__6");
            BonesFramework._currentAdditionalRootBones.Clear();
            if (BonesFramework._currentLoadingCloth == null)
                return;

            ListTypeFbx ltf = (ListTypeFbx)self.GetPrivate("<ltf>__3");
            string assetName = (string)self.GetPrivate("<assetName>__5");
            TextAsset ta = CommonLib.LoadAsset<TextAsset>(ltf.ABPath, "additional_bones", true, ltf.Manifest);
            if (ta == null)
                return;

            UnityEngine.Debug.Log("BonesFramework: Loaded additional_bones TextAsset from " + ltf.ABPath);
            BonesFramework._currentAdditionalObjects.Add(BonesFramework._currentLoadingCloth, new List<GameObject>());
            OnDestroyTrigger onDestroyTrigger = BonesFramework._currentLoadingCloth.AddComponent<OnDestroyTrigger>();
            onDestroyTrigger.onDestroy = (go) =>
            {
                DynamicBone[] dbs = go.transform.parent.GetComponentsInChildren<DynamicBone>();
                foreach (GameObject o in BonesFramework._currentAdditionalObjects[go])
                {
                    DynamicBoneCollider[] colliders = go.GetComponentsInChildren<DynamicBoneCollider>(true);
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
                BonesFramework._currentAdditionalObjects.Remove(go);
            };

            string[] lines = ta.text.Split(new [] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string[] cells = line.Split('\t');
                if (!cells[0].Equals(assetName))
                    continue;
                UnityEngine.Debug.Log("BonesFramework: Found matching line for asset " + assetName + "\n" + line);
                for (int i = 1; i < cells.Length; i++)
                    BonesFramework._currentAdditionalRootBones.Add(cells[i]);

                onDestroyTrigger.onStart = (self2) =>
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
    }
}