using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SB3Utility;

namespace SB3UDebug
{
    public class SB3UDebug
    {
        [Plugin]
        public static void LogArray([DefaultVar] object[] array)
        {
            foreach (object o in array)
            {
                Console.WriteLine(o);
            }
        }
    }
}
