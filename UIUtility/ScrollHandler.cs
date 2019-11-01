﻿using System;
using UnityEngine.EventSystems;

namespace UILib
{
    public class ScrollHandler : UIBehaviour, IScrollHandler
    {
        public event Action<PointerEventData> onScroll;

        public void OnScroll(PointerEventData eventData)
        {
            if (this.onScroll != null)
                this.onScroll(eventData);
        }
    }
}
