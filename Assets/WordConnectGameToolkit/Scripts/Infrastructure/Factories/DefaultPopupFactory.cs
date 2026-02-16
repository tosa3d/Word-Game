using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace WordsToolkit.Scripts.Popups
{
    public class DefaultPopupFactory : IPopupFactory
    {
        private readonly IObjectResolver _container;

        public DefaultPopupFactory(IObjectResolver container)
        {
            _container = container;
        }

        public Popup CreatePopup(string pathWithType, Transform parent)
        {
            var popupPrefab = Resources.Load<Popup>(pathWithType);
            if (popupPrefab == null)
            {
                Debug.LogError("Popup prefab not found in Resources folder: " + pathWithType);
                return null;
            }
            return CreatePopup(popupPrefab, parent);
        }

        public T CreatePopup<T>(Transform parent) where T : Popup
        {
            return (T)CreatePopup("Popups/" + typeof(T).Name, parent);
        }

        public Popup CreatePopup(Popup popupPrefab, Transform parent)
        {
            var popup = _container.Instantiate(popupPrefab, parent);
            var rectTransform = popup.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = Vector2.zero;
            }
            return popup;
        }
    }
} 