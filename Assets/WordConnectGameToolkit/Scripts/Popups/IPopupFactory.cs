using UnityEngine;
using System;

namespace WordsToolkit.Scripts.Popups
{
    public interface IPopupFactory
    {
        Popup CreatePopup(string pathWithType, Transform parent);
        T CreatePopup<T>(Transform parent) where T : Popup;
        Popup CreatePopup(Popup popupPrefab, Transform parent);
    }
} 