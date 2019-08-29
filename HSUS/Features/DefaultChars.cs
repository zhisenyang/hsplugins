using System.IO;
using UnityEngine;
#if  KOIKATSU    
using ChaCustom;
using ToolBox;
#endif

namespace HSUS
{
    public static class DefaultChars
    {
        public static void Do(int level)
        {
#if HONEYSELECT
            if (level == 21 && string.IsNullOrEmpty(HSUS._self._defaultFemaleChar) == false)
                LoadCustomDefault(Path.Combine(Path.Combine(Path.Combine(UserData.Path, "chara"), "female"), HSUS._self._defaultFemaleChar).Replace("\\", "/"));
#elif KOIKATSU
            if (level == 2)
            {
                HSUS._self.ExecuteDelayed(() =>
                {
                    switch (CustomBase.Instance.modeSex)
                    {
                        case 0:
                            if (string.IsNullOrEmpty(HSUS._self._defaultMaleChar) == false)
                                LoadCustomDefault(UserData.Path + "chara/male/" + HSUS._self._defaultMaleChar);
                            break;
                        case 1:
                            if (string.IsNullOrEmpty(HSUS._self._defaultFemaleChar) == false)
                                LoadCustomDefault(UserData.Path + "chara/female/" + HSUS._self._defaultFemaleChar);
                            break;
                    }
                });
            }
#endif
        }

#if HONEYSELECT
        private static void LoadCustomDefault(string path)
        {
            CustomControl customControl = Resources.FindObjectsOfTypeAll<CustomControl>()[0];
            int personality = customControl.chainfo.customInfo.personality;
            string name = customControl.chainfo.customInfo.name;
            bool isConcierge = customControl.chainfo.customInfo.isConcierge;
            bool flag = false;
            bool flag2 = false;
            if (customControl.modeCustom == 0)
            {
                customControl.chainfo.chaFile.Load(path);
                customControl.chainfo.chaFile.ChangeCoordinateType(customControl.chainfo.statusInfo.coordinateType);
                if (customControl.chainfo.chaFile.customInfo.isConcierge)
                {
                    flag = true;
                    flag2 = true;
                }
            }
            else
            {
                customControl.chainfo.chaFile.LoadBlockData(customControl.chainfo.customInfo, path);
                customControl.chainfo.chaFile.LoadBlockData(customControl.chainfo.chaFile.coordinateInfo, path);
                customControl.chainfo.chaFile.ChangeCoordinateType(customControl.chainfo.statusInfo.coordinateType);
                flag = true;
                flag2 = true;
            }
            customControl.chainfo.customInfo.isConcierge = isConcierge;
            if (customControl.chainfo.Sex == 0)
            {
                CharMale charMale = customControl.chainfo as CharMale;
                charMale.Reload();
                charMale.maleStatusInfo.visibleSon = false;
            }
            else
            {
                CharFemale charFemale = customControl.chainfo as CharFemale;
                charFemale.Reload();
                charFemale.UpdateBustSoftnessAndGravity();
            }
            if (flag)
            {
                customControl.chainfo.customInfo.personality = personality;
            }
            if (flag2)
            {
                customControl.chainfo.customInfo.name = name;
            }
            customControl.SetSameSetting();
            customControl.noChangeSubMenu = true;
            customControl.ChangeSwimTypeFromLoad();
            customControl.noChangeSubMenu = false;
            customControl.UpdateCharaName();
            customControl.UpdateAcsName();
        }

#elif KOIKATSU
        private static void LoadCustomDefault(string path)
        {
            ChaControl chaCtrl = Singleton<CustomBase>.Instance.chaCtrl;
            CustomBase.Instance.chaCtrl.chaFile.LoadFileLimited(path, chaCtrl.sex, true, true, true, true, true);
            chaCtrl.ChangeCoordinateType(true);
            chaCtrl.Reload(false, false, false, false);
            CustomBase.Instance.updateCustomUI = true;
            //CustomHistory.Instance.Add5(chaCtrl, chaCtrl.Reload, false, false, false, false);
        }
#endif
    }
}
