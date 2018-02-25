using System.Collections.Generic;
using Harmony;
using Studio;

namespace HSPE
{
    [HarmonyPatch(typeof(Studio.Studio), "Duplicate")]
    public class Studio_Duplicate_Patches
    {
        private static SortedList<int, OCIChar> _toDuplicate;

        public static void Prefix(Studio.Studio __instance)
        {
            _toDuplicate = new SortedList<int, OCIChar>();
            TreeNodeObject[] selectNodes = __instance.treeNodeCtrl.selectNodes;
            for (int i = 0; i < selectNodes.Length; i++)
            {
                ObjectCtrlInfo objectCtrlInfo = null;
                if (__instance.dicInfo.TryGetValue(selectNodes[i], out objectCtrlInfo))
                {
                    objectCtrlInfo.OnSavePreprocessing();
                    OCIChar ociChar = objectCtrlInfo as OCIChar;
                    if (ociChar != null)
                    {
                        _toDuplicate.Add(objectCtrlInfo.objectInfo.dicKey, ociChar);
                    }
                }
            }
        }

        public static void Postfix(Studio.Studio __instance)
        {
            IEnumerator<KeyValuePair<int, OCIChar>> enumerator = _toDuplicate.GetEnumerator();
            foreach (KeyValuePair<int, ObjectInfo> keyValuePair in new SortedDictionary<int, ObjectInfo>(__instance.sceneInfo.dicImport))
            {
                OCIChar dest = __instance.dicObjectCtrl[keyValuePair.Value.dicKey] as OCIChar;
                if (dest != null && enumerator.MoveNext())
                    MainWindow.self.OnDuplicate(enumerator.Current.Value, dest);
            }
            enumerator.Dispose();
        }
    }
}
