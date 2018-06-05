// Decompiled with JetBrains decompiler
// Type: ShortcutsHS.ShortcutsHS
// Assembly: ShortcutsHS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 62AE9985-56CF-46BD-982F-B15D5A0C4B01
// Assembly location: C:\Program Files (x86)\HoneySelect\illusion\HoneySelect\Plugins\ShortcutsHS.dll

using System;
using System.Collections.Generic;
using System.Reflection;
using GUITree;
using Harmony;
using HSPE.AMModules;
using IllusionPlugin;
using ILSetUtility.TimeUtility;
using Manager;
using RootMotion.FinalIK;
using UILib;
using UnityEngine;
using Studio;
using SuperScrollView;
using UniRx.Triggers;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityStandardAssets.CinematicEffects;
using UnityStandardAssets.ImageEffects;
using Vectrosity;
using DepthOfField = UnityStandardAssets.ImageEffects.DepthOfField;
using Info = Studio.Info;

namespace HSPE
{
    public class HSPE : IEnhancedPlugin
    {
        public static string VersionNum { get { return "2.5.0"; } }

        public string Name { get { return "HSPE"; } }

        public string Version { get { return HSPE.VersionNum; } }

        public string[] Filter { get { return new[] {"StudioNEO_32", "StudioNEO_64"}; } }

        public void OnApplicationQuit()
        {

        }

        public void OnApplicationStart()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("com.joan6694.hsplugins.hspe");
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                try
                {
                    List<HarmonyMethod> harmonyMethods = type.GetHarmonyMethods();
                    if (harmonyMethods != null && harmonyMethods.Count > 0)
                    {
                        HarmonyMethod attributes = HarmonyMethod.Merge(harmonyMethods);
                        new PatchProcessor(harmony, type, attributes).Patch();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log("HSPE: Exception occured when patching: " + e.ToString());
                }
            }
            UIUtility.Init();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnLevelWasInitialized(int level)
        {
            if (level == 3)
            {
                GameObject go = new GameObject("HSPEPlugin");
                go.AddComponent<MainWindow>();
            }
        }

        public void OnLevelWasLoaded(int level)
        {
        }

        public void OnUpdate()
        {
        }
    }

    /*
    [HarmonyPatch(typeof(MeshCollider), "LateUpdate")]
    public class MeshCollider_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GuideMove), "LateUpdate")]
    public class GuideMove_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MeshRenderer), "LateUpdate")]
    public class MeshRenderer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MeshFilter), "LateUpdate")]
    public class MeshFilter_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Transform), "LateUpdate")]
    public class Transform_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(BoxCollider), "LateUpdate")]
    public class BoxCollider_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Text), "LateUpdate")]
    public class Text_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CanvasRenderer), "LateUpdate")]
    public class CanvasRenderer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(RectTransform), "LateUpdate")]
    public class RectTransform_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(LoopListViewItem2), "LateUpdate")]
    public class LoopListViewItem2_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Button), "LateUpdate")]
    public class Button_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Image), "LateUpdate")]
    public class Image_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ClickEventListener), "LateUpdate")]
    public class ClickEventListener_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(LoopListView2), "LateUpdate")]
    public class LoopListView2_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(HSceneFlagCtrl), "LateUpdate")]
    public class HSceneFlagCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CharFemaleBody), "LateUpdate")]
    public class CharFemaleBody_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CharFemale), "LateUpdate")]
    public class CharFemale_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VectorObject2D), "LateUpdate")]
    public class VectorObject2D_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(BlendShapesEditor), "LateUpdate")]
    public class BlendShapesEditor_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(DynamicBonesEditor), "LateUpdate")]
    public class DynamicBonesEditor_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(BoobsEditor), "LateUpdate")]
    public class BoobsEditor_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Canvas), "LateUpdate")]
    public class Canvas_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(BonesEditor), "LateUpdate")]
    public class BonesEditor_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(PoseController), "LateUpdate")]
    public class PoseController_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ListTypeFbxComponent), "LateUpdate")]
    public class ListTypeFbxComponent_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SkinnedMeshRenderer), "LateUpdate")]
    public class SkinnedMeshRenderer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SetRenderQueue), "LateUpdate")]
    public class SetRenderQueue_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(DynamicBone), "LateUpdate")]
    public class DynamicBone_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(FKCtrl), "LateUpdate")]
    public class FKCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TextSizeFitter), "LateUpdate")]
    public class TextSizeFitter_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ContentSizeFitter), "LateUpdate")]
    public class ContentSizeFitter_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(PreferredSizeFitter), "LateUpdate")]
    public class PreferredSizeFitter_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TreeNodeObject), "LateUpdate")]
    public class TreeNodeObject_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TreeNode), "LateUpdate")]
    public class TreeNode_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GuideScale), "LateUpdate")]
    public class GuideScale_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GuideRotation), "LateUpdate")]
    public class GuideRotation_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GuideSelect), "LateUpdate")]
    public class GuideSelect_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SphereCollider), "LateUpdate")]
    public class SphereCollider_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GuideObject), "LateUpdate")]
    public class GuideObject_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(IKCtrl), "LateUpdate")]
    public class IKCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(HandAnimeCtrl), "LateUpdate")]
    public class HandAnimeCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Animator), "LateUpdate")]
    public class Animator_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CharAnimeCtrl), "LateUpdate")]
    public class CharAnimeCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(OptionItemCtrl), "LateUpdate")]
    public class OptionItemCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Expression), "LateUpdate")]
    public class Expression_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(FaceBlendShape), "LateUpdate")]
    public class FaceBlendShape_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ChangePtnAnime), "LateUpdate")]
    public class ChangePtnAnime_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(EyeLookController), "LateUpdate")]
    public class EyeLookController_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(EyeLookCalc), "LateUpdate")]
    public class EyeLookCalc_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(DynamicBoneCollider), "LateUpdate")]
    public class DynamicBoneCollider_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(NeckLookCalcVer2), "LateUpdate")]
    public class NeckLookCalcVer2_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(NeckLookControllerVer2), "LateUpdate")]
    public class NeckLookControllerVer2_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AnimeIKCtrl), "LateUpdate")]
    public class AnimeIKCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(FullBodyBipedIK), "LateUpdate")]
    public class FullBodyBipedIK_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(DynamicBone_Ver02), "LateUpdate")]
    public class DynamicBone_Ver02_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(LayoutElement), "LateUpdate")]
    public class LayoutElement_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GraphicRaycaster), "LateUpdate")]
    public class GraphicRaycaster_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CanvasScaler), "LateUpdate")]
    public class CanvasScaler_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Mask), "LateUpdate")]
    public class Mask_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ScrollRect), "LateUpdate")]
    public class ScrollRect_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Scrollbar), "LateUpdate")]
    public class Scrollbar_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MovableWindow), "LateUpdate")]
    public class MovableWindow_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(RawImage), "LateUpdate")]
    public class RawImage_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(HSceneManager), "LateUpdate")]
    public class HSceneManager_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(BackgroundCtrl), "LateUpdate")]
    public class BackgroundCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(DrawLightLine), "LateUpdate")]
    public class DrawLightLine_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(PhysicsRaycaster), "LateUpdate")]
    public class PhysicsRaycaster_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Camera), "LateUpdate")]
    public class Camera_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Light), "LateUpdate")]
    public class Light_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Rigidbody), "LateUpdate")]
    public class Rigidbody_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CapsuleCollider), "LateUpdate")]
    public class CapsuleCollider_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CameraControl), "LateUpdate")]
    public class CameraControl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GameScreenShotAssist), "LateUpdate")]
    public class GameScreenShotAssist_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CrossFade), "LateUpdate")]
    public class CrossFade_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Antialiasing), "LateUpdate")]
    public class Antialiasing_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(BloomAndFlares), "LateUpdate")]
    public class BloomAndFlares_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ColorCorrectionCurves), "LateUpdate")]
    public class ColorCorrectionCurves_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SunShafts), "LateUpdate")]
    public class SunShafts_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VignetteAndChromaticAberration), "LateUpdate")]
    public class VignetteAndChromaticAberration_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(DepthOfField), "LateUpdate")]
    public class DepthOfField_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GlobalFog), "LateUpdate")]
    public class GlobalFog_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ScreenSpaceReflection), "LateUpdate")]
    public class ScreenSpaceReflection_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SSAOPro), "LateUpdate")]
    public class SSAOPro_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ParticleSystemRenderer), "LateUpdate")]
    public class ParticleSystemRenderer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ParticleSystem), "LateUpdate")]
    public class ParticleSystem_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Toggle), "LateUpdate")]
    public class Toggle_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(PointerDownHandler), "LateUpdate")]
    public class PointerDownHandler_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AspectRatioFitter), "LateUpdate")]
    public class AspectRatioFitter_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Outline), "LateUpdate")]
    public class Outline_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Slider), "LateUpdate")]
    public class Slider_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MainWindow), "LateUpdate")]
    public class MainWindow_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(InputField), "LateUpdate")]
    public class InputField_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(RectMask2D), "LateUpdate")]
    public class RectMask2D_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VerticalLayoutGroup), "LateUpdate")]
    public class VerticalLayoutGroup_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(HorizontalLayoutGroup), "LateUpdate")]
    public class HorizontalLayoutGroup_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(StandaloneInputModule), "LateUpdate")]
    public class StandaloneInputModule_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(EventSystem), "LateUpdate")]
    public class EventSystem_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ObservableDestroyTrigger), "LateUpdate")]
    public class ObservableDestroyTrigger_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ObservableUpdateTrigger), "LateUpdate")]
    public class ObservableUpdateTrigger_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Dropdown), "LateUpdate")]
    public class Dropdown_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_CameraMask), "LateUpdate")]
    public class SteamVR_CameraMask_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GridLayoutGroup), "LateUpdate")]
    public class GridLayoutGroup_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AudioListener), "LateUpdate")]
    public class AudioListener_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Game), "LateUpdate")]
    public class Game_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Map), "LateUpdate")]
    public class Map_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SceneFade), "LateUpdate")]
    public class SceneFade_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SpriteRenderer), "LateUpdate")]
    public class SpriteRenderer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TimeUtility), "LateUpdate")]
    public class TimeUtility_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GameScreenShot), "LateUpdate")]
    public class GameScreenShot_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Scene), "LateUpdate")]
    public class Scene_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Voice), "LateUpdate")]
    public class Voice_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AssetBundleManager), "LateUpdate")]
    public class AssetBundleManager_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GUILayer), "LateUpdate")]
    public class GUILayer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(LineRenderer), "LateUpdate")]
    public class LineRenderer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(FlareLayer), "LateUpdate")]
    public class FlareLayer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_Render), "LateUpdate")]
    public class SteamVR_Render_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Info), "LateUpdate")]
    public class Info_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_RenderModel), "LateUpdate")]
    public class SteamVR_RenderModel_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_Ears), "LateUpdate")]
    public class SteamVR_Ears_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_TrackedObject), "LateUpdate")]
    public class SteamVR_TrackedObject_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TouchpadRotation), "LateUpdate")]
    public class TouchpadRotation_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(RightController), "LateUpdate")]
    public class RightController_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GripRotation), "LateUpdate")]
    public class GripRotation_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GripMove), "LateUpdate")]
    public class GripMove_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Teleportation), "LateUpdate")]
    public class Teleportation_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(DestinationPointer), "LateUpdate")]
    public class DestinationPointer_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_Camera), "LateUpdate")]
    public class SteamVR_Camera_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_CameraFlip), "LateUpdate")]
    public class SteamVR_CameraFlip_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_PlayArea), "LateUpdate")]
    public class SteamVR_PlayArea_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_ControllerManager), "LateUpdate")]
    public class SteamVR_ControllerManager_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SteamVR_GameView), "LateUpdate")]
    public class SteamVR_GameView_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(StudioVR), "LateUpdate")]
    public class StudioVR_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ChangeFocus), "LateUpdate")]
    public class ChangeFocus_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MapDragButton), "LateUpdate")]
    public class MapDragButton_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(DragObject), "LateUpdate")]
    public class DragObject_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(OptionCtrl), "LateUpdate")]
    public class OptionCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(StudioNode), "LateUpdate")]
    public class StudioNode_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VoiceControl), "LateUpdate")]
    public class VoiceControl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VoiceList), "LateUpdate")]
    public class VoiceList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GameSceneNode), "LateUpdate")]
    public class GameSceneNode_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(InputFieldToCamera), "LateUpdate")]
    public class InputFieldToCamera_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ChangeFocusSender), "LateUpdate")]
    public class ChangeFocusSender_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UI_ColorPresetsClick), "LateUpdate")]
    public class UI_ColorPresetsClick_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ButtonPlaySE), "LateUpdate")]
    public class ButtonPlaySE_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GuideInputSender), "LateUpdate")]
    public class GuideInputSender_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AutoLayoutCtrl), "LateUpdate")]
    public class AutoLayoutCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UI_ColorMenu), "LateUpdate")]
    public class UI_ColorMenu_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UI_HideMenuAnimation), "LateUpdate")]
    public class UI_HideMenuAnimation_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UI_NoControlCamera), "LateUpdate")]
    public class UI_NoControlCamera_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CanvasSortOrder), "LateUpdate")]
    public class CanvasSortOrder_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UI_Parameter), "LateUpdate")]
    public class UI_Parameter_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CameraLightCtrl), "LateUpdate")]
    public class CameraLightCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AnimeList), "LateUpdate")]
    public class AnimeList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UI_InfoPicker), "LateUpdate")]
    public class UI_InfoPicker_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MPItemCtrl), "LateUpdate")]
    public class MPItemCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ToggleGroup), "LateUpdate")]
    public class ToggleGroup_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SoundButtonCtrl), "LateUpdate")]
    public class SoundButtonCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TreeNodeCtrl), "LateUpdate")]
    public class TreeNodeCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AnimeCategoryList), "LateUpdate")]
    public class AnimeCategoryList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(OutsideSoundControl), "LateUpdate")]
    public class OutsideSoundControl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(BGMControl), "LateUpdate")]
    public class BGMControl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AnimeControl), "LateUpdate")]
    public class AnimeControl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SystemButtonCtrl), "LateUpdate")]
    public class SystemButtonCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(EventTrigger), "LateUpdate")]
    public class EventTrigger_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ColorPaletteCtrl), "LateUpdate")]
    public class ColorPaletteCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TextSlideEffectCtrl), "LateUpdate")]
    public class TextSlideEffectCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TextSlideEffect), "LateUpdate")]
    public class TextSlideEffect_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ItemList), "LateUpdate")]
    public class ItemList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(CharaList), "LateUpdate")]
    public class CharaList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MPPathMoveCtrl), "LateUpdate")]
    public class MPPathMoveCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UI_OnOffImage), "LateUpdate")]
    public class UI_OnOffImage_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MPCharCtrl), "LateUpdate")]
    public class MPCharCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(LightList), "LateUpdate")]
    public class LightList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VoicePlayNode), "LateUpdate")]
    public class VoicePlayNode_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GuideInput), "LateUpdate")]
    public class GuideInput_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ConfigCtrl), "LateUpdate")]
    public class ConfigCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VoiceCategoryList), "LateUpdate")]
    public class VoiceCategoryList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(TreeRoot), "LateUpdate")]
    public class TreeRoot_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ScrollSizeCorrect), "LateUpdate")]
    public class ScrollSizeCorrect_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UI_DragWindow), "LateUpdate")]
    public class UI_DragWindow_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(WorkspaceCtrl), "LateUpdate")]
    public class WorkspaceCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VoiceNode), "LateUpdate")]
    public class VoiceNode_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(PauseRegistrationList), "LateUpdate")]
    public class PauseRegistrationList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(StudioScene), "LateUpdate")]
    public class StudioScene_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(RootButtonCtrl), "LateUpdate")]
    public class RootButtonCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(Studio.Studio), "LateUpdate")]
    public class Studio_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GuideObjectManager), "LateUpdate")]
    public class GuideObjectManager_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(UndoRedoManager), "LateUpdate")]
    public class UndoRedoManager_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ShortcutKeyCtrl), "LateUpdate")]
    public class ShortcutKeyCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(SortCanvas), "LateUpdate")]
    public class SortCanvas_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(GameCursor), "LateUpdate")]
    public class GameCursor_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MapCtrl), "LateUpdate")]
    public class MapCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MapList), "LateUpdate")]
    public class MapList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ItemGroupList), "LateUpdate")]
    public class ItemGroupList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VoiceGroupList), "LateUpdate")]
    public class VoiceGroupList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AddButtonCtrl), "LateUpdate")]
    public class AddButtonCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(VoiceRegistrationList), "LateUpdate")]
    public class VoiceRegistrationList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(BackgroundList), "LateUpdate")]
    public class BackgroundList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MPFolderCtrl), "LateUpdate")]
    public class MPFolderCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(MPLightCtrl), "LateUpdate")]
    public class MPLightCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(AnimeGroupList), "LateUpdate")]
    public class AnimeGroupList_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ENVControl), "LateUpdate")]
    public class ENVControl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    [HarmonyPatch(typeof(ManipulatePanelCtrl), "LateUpdate")]
    public class ManipulatePanelCtrl_patches
    {
        public static void Postfix(object __instance)
        {
            UnityEngine.Debug.LogError(Time.frameCount + " " + __instance.GetType().FullName);
        }
    }
    */
}