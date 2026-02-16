using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using WordsToolkit.Scripts.Popups;

namespace WordConnectGameToolkit.Scripts.Settings.Editor
{
    [CustomPropertyDrawer(typeof(InterstitialAdElement))]
    public class InterstitialAdElementDrawer : PropertyDrawer
    {
        private Popup[] popupPrefabs;
        private List<string> popupNames;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            LoadPopupPrefabs();

            var container = new VisualElement();
            
            var adReferenceProperty = property.FindPropertyRelative("adReference");
            var elementNameProperty = property.FindPropertyRelative("elementName");
            var popupProperty = property.FindPropertyRelative("popup");
            var showOnOpenProperty = property.FindPropertyRelative("showOnOpen");
            var showOnCloseProperty = property.FindPropertyRelative("showOnClose");
            var minLevelProperty = property.FindPropertyRelative("minLevel");
            var maxLevelProperty = property.FindPropertyRelative("maxLevel");
            var frequencyProperty = property.FindPropertyRelative("frequency");

            // Update element name based on ad reference
            UpdateElementName(adReferenceProperty, elementNameProperty);

            // Ad Reference field
            var adReferenceField = new PropertyField(adReferenceProperty);
            adReferenceField.RegisterValueChangeCallback(evt =>
            {
                UpdateElementName(adReferenceProperty, elementNameProperty);
                property.serializedObject.ApplyModifiedProperties();
            });
            container.Add(adReferenceField);

            // Popup dropdown
            var popupDropdown = new DropdownField("Popup", popupNames, GetPopupIndex(popupProperty.objectReferenceValue as Popup));
            popupDropdown.RegisterValueChangedCallback(evt =>
            {
                int selectedIndex = popupNames.IndexOf(evt.newValue);
                if (selectedIndex == 0)
                {
                    popupProperty.objectReferenceValue = null;
                }
                else if (selectedIndex > 0)
                {
                    popupProperty.objectReferenceValue = popupPrefabs[selectedIndex - 1];
                }
                popupProperty.serializedObject.ApplyModifiedProperties();
            });
            container.Add(popupDropdown);

            // Show options
            var showOnOpenField = new PropertyField(showOnOpenProperty);
            container.Add(showOnOpenField);

            var showOnCloseField = new PropertyField(showOnCloseProperty);
            container.Add(showOnCloseField);

            // Level conditions header
            var levelHeader = new Label("Level Conditions");
            levelHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            levelHeader.style.marginTop = 5;
            container.Add(levelHeader);

            // Level conditions fields
            var minLevelField = new PropertyField(minLevelProperty);
            container.Add(minLevelField);

            var maxLevelField = new PropertyField(maxLevelProperty);
            container.Add(maxLevelField);

            var frequencyField = new PropertyField(frequencyProperty);
            container.Add(frequencyField);

            return container;
        }

        private void LoadPopupPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            var popups = guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
                .Where(go => go != null && go.GetComponent<Popup>() != null)
                .Select(go => go.GetComponent<Popup>())
                .OrderBy(popup => popup.name)
                .ToArray();

            popupPrefabs = popups;
            popupNames = new List<string> { "None (Popup)" };
            popupNames.AddRange(popups.Select(popup => popup.name));
        }

        private int GetPopupIndex(Popup popup)
        {
            if (popup == null) return 0;
            
            for (int i = 0; i < popupPrefabs.Length; i++)
            {
                if (popupPrefabs[i] == popup)
                    return i + 1;
            }
            return 0;
        }

        private void UpdateElementName(SerializedProperty adReferenceProperty, SerializedProperty elementNameProperty)
        {
            if (adReferenceProperty.objectReferenceValue != null)
            {
                string adRefName = adReferenceProperty.objectReferenceValue.name;
                elementNameProperty.stringValue = adRefName;
            }
            else
            {
                elementNameProperty.stringValue = "Unnamed";
            }
        }
    }
}