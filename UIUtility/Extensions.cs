﻿using System;
using System.Collections;
using UnityEngine;

namespace UILib
{
    public static class Extensions
    {
        public static void ExecuteDelayed(this MonoBehaviour self, Action action, int waitCount = 1)
        {
            self.StartCoroutine(ExecuteDelayed_Routine(action, waitCount));
        }

        private static IEnumerator ExecuteDelayed_Routine(Action action, int waitCount)
        {
            for (int i = 0; i < waitCount; ++i)
                yield return null;
            action();
        }
    }
}