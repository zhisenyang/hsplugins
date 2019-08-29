#if HONEYSELECT
using System.Reflection;
using Harmony;
using Studio;

namespace HSUS
{
    public static class FKColors
    {
        [HarmonyPatch]
        internal static class BoneInfo_Ctor_Patches
        {
            private static ConstructorInfo TargetMethod()
            {
                return typeof(OCIChar.BoneInfo).GetConstructor(new[] { typeof(GuideObject), typeof(OIBoneInfo) });
            }

            private static bool Prepare()
            {
                return HSUS._self._binary == HSUS.Binary.Neo;
            }

            private static void Postfix(GuideObject _guideObject, OIBoneInfo _boneInfo)
            {
                switch (_boneInfo.group)
                {
                    case OIBoneInfo.BoneGroup.Body:
                        _guideObject.guideSelect.color = HSUS._self._fkBodyColor;
                        break;
                    case (OIBoneInfo.BoneGroup)3:
                    case OIBoneInfo.BoneGroup.RightLeg:
                        _guideObject.guideSelect.color = HSUS._self._fkBodyColor;
                        break;
                    case (OIBoneInfo.BoneGroup)5:
                    case OIBoneInfo.BoneGroup.LeftLeg:
                        _guideObject.guideSelect.color = HSUS._self._fkBodyColor;
                        break;
                    case (OIBoneInfo.BoneGroup)9:
                    case OIBoneInfo.BoneGroup.RightArm:
                        _guideObject.guideSelect.color = HSUS._self._fkBodyColor;
                        break;
                    case (OIBoneInfo.BoneGroup)17:
                    case OIBoneInfo.BoneGroup.LeftArm:
                        _guideObject.guideSelect.color = HSUS._self._fkBodyColor;
                        break;
                    case OIBoneInfo.BoneGroup.RightHand:
                        _guideObject.guideSelect.color = HSUS._self._fkRightHandColor;
                        break;
                    case OIBoneInfo.BoneGroup.LeftHand:
                        _guideObject.guideSelect.color = HSUS._self._fkLeftHandColor;
                        break;
                    case OIBoneInfo.BoneGroup.Hair:
                        _guideObject.guideSelect.color = HSUS._self._fkHairColor;
                        break;
                    case OIBoneInfo.BoneGroup.Neck:
                        _guideObject.guideSelect.color = HSUS._self._fkNeckColor;
                        break;
                    case OIBoneInfo.BoneGroup.Breast:
                        _guideObject.guideSelect.color = HSUS._self._fkChestColor;
                        break;
                    case OIBoneInfo.BoneGroup.Skirt:
                        _guideObject.guideSelect.color = HSUS._self._fkSkirtColor;
                        break;
                    case (OIBoneInfo.BoneGroup)0:
                        _guideObject.guideSelect.color = HSUS._self._fkItemsColor;
                        break;
                }
            }
        }
    }
}
#endif