using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace WordsToolkit.Scripts.Levels.Editor
{
    [CustomEditor(typeof(LanguageConfiguration))]
    public class LanguageConfigurationEditor : UnityEditor.Editor
    {
        private SerializedProperty languagesProp;
        private SerializedProperty defaultLanguageProp;
        
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            languagesProp = serializedObject.FindProperty("languages");
            defaultLanguageProp = serializedObject.FindProperty("defaultLanguage");

            // Create default language dropdown
            var defaultLanguageField = new PopupField<string>("Default Language");
            UpdateDefaultLanguageChoices(defaultLanguageField);
            root.Add(defaultLanguageField);
            
            // Create languages list
            var listView = new ListView
            {
                reorderable = true,
                showAddRemoveFooter = true,
                showBorder = true,
                showFoldoutHeader = true,
                headerTitle = "Languages",
                fixedItemHeight = 20
            };
            
            listView.makeItem = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                
                var codeField = new TextField { name = "code", style = { width = 60 } };
                var nameField = new TextField { name = "displayName", style = { width = 120 } };
                var localizedField = new TextField { name = "localizedName", style = { width = 120 } };
                var enabledToggle = new Toggle { name = "enabledByDefault", style = { width = 20 } };
                var modelField = new ObjectField { name = "languageModel", style = { width = 120 } };
                var baseField = new ObjectField { name = "localizationBase", style = { width = 120 } };
                
                row.Add(codeField);
                row.Add(nameField);
                row.Add(localizedField);
                row.Add(enabledToggle);
                row.Add(modelField);
                row.Add(baseField);
                
                return row;
            };
            
            listView.bindItem = (element, index) =>
            {
                var itemProperty = languagesProp.GetArrayElementAtIndex(index);
                
                var codeField = element.Q<TextField>("code");
                var nameField = element.Q<TextField>("displayName");
                var localizedField = element.Q<TextField>("localizedName");
                var enabledToggle = element.Q<Toggle>("enabledByDefault");
                var modelField = element.Q<ObjectField>("languageModel");
                var baseField = element.Q<ObjectField>("localizationBase");
                
                codeField.BindProperty(itemProperty.FindPropertyRelative("code"));
                nameField.BindProperty(itemProperty.FindPropertyRelative("displayName"));
                localizedField.BindProperty(itemProperty.FindPropertyRelative("localizedName"));
                enabledToggle.BindProperty(itemProperty.FindPropertyRelative("enabledByDefault"));
                modelField.BindProperty(itemProperty.FindPropertyRelative("languageModel"));
                baseField.BindProperty(itemProperty.FindPropertyRelative("localizationBase"));
            };
            
            listView.itemsAdded += (indexes) =>
            {
                foreach (int index in indexes)
                {
                    var element = languagesProp.GetArrayElementAtIndex(index);
                    element.FindPropertyRelative("code").stringValue = "new";
                    element.FindPropertyRelative("displayName").stringValue = "New Language";
                    element.FindPropertyRelative("localizedName").stringValue = "New Language";
                    element.FindPropertyRelative("enabledByDefault").boolValue = true;
                    serializedObject.ApplyModifiedProperties();
                    UpdateDefaultLanguageChoices(defaultLanguageField);
                }
            };
            
            listView.itemsRemoved += (indexes) =>
            {
                UpdateDefaultLanguageChoices(defaultLanguageField);
            };
            
            // Bind the list view to the languages property
            listView.bindingPath = languagesProp.propertyPath;
            listView.Bind(serializedObject);
            
            root.Add(listView);
            return root;
        }
        
        private void UpdateDefaultLanguageChoices(PopupField<string> popup)
        {
            var choices = new List<string>();
            for (int i = 0; i < languagesProp.arraySize; i++)
            {
                var element = languagesProp.GetArrayElementAtIndex(i);
                choices.Add(element.FindPropertyRelative("code").stringValue);
            }
            
            popup.choices = choices;
            popup.value = defaultLanguageProp.stringValue;
            popup.RegisterValueChangedCallback(evt =>
            {
                defaultLanguageProp.stringValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });
        }
    }
}
