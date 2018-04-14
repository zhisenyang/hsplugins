using System.Collections.Generic;

namespace BegoneThot
{
    public class ItemEntry
    {
        public int? Id;
        public int Category;
        public string ItemName;
        public string Unknown1;
        public string FilePath;
        public string AnimatorName;
        public string Unknown2;
        public bool AnimationFlag;
        public bool Color1Flag;
        public string Color1Renderers;
        public bool Color2Flag;
        public string Color2Renderers;
        public bool ScalingFlag;

        public static bool TryParse(string s, out ItemEntry entry)
        {
            List<string> res = new List<string>();
            int i = 0;
            entry = null;
            while (i < s.Length)
            {
                if (s[i] != '<')
                    return false;
                int start = i;
                while (i < s.Length && s[i] != '>')
                    ++i;
                if (i == s.Length)
                    return false;
                res.Add(s.Substring(start + 1, i - start - 1));
                i++;
            }
            if (res.Count != 13)
                return false;
            entry = new ItemEntry();
            if (res[0].Length == 0)
                entry.Id = null;
            else
            {
                if (int.TryParse(res[0], out int id))
                    entry.Id = id;
                else
                    return false;
            }
            if (int.TryParse(res[1], out int category))
                entry.Category = category;
            else
                return false;
            entry.ItemName = res[2];
            entry.Unknown1 = res[3];
            entry.FilePath = res[4];
            entry.AnimatorName = res[5];
            entry.Unknown2 = res[6];
            if (bool.TryParse(res[7], out bool animationFlag))
                entry.AnimationFlag = animationFlag;
            else
                return false;
            if (bool.TryParse(res[8], out bool color1Flag))
                entry.Color1Flag = color1Flag;
            else
                return false;
            entry.Color1Renderers = res[9];
            if (bool.TryParse(res[10], out bool color2Flag))
                entry.Color2Flag = color2Flag;
            else
                return false;
            entry.Color2Renderers = res[11];
            if (bool.TryParse(res[12], out bool scalingFlag))
                entry.ScalingFlag = scalingFlag;
            else
                return false;
            return true;
        }

        public override string ToString()
        {
            return $"<{this.Id}><{this.Category}><{this.ItemName}><{this.Unknown1}><{this.FilePath}><{this.AnimatorName}><{this.Unknown2}><{this.AnimationFlag}><{this.Color1Flag}><{this.Color1Renderers}><{this.Color2Flag}><{this.Color2Renderers}><{this.ScalingFlag}>";
        }

        public static string GenerateEmpty(int? id)
        {
            return $"<{id}><98><Empty{id:00000000}><studioneo00><studioneo/00.unity3d><p_item_hs_sphere_00><><False><False><><False><><False>";
        }
    }
}
