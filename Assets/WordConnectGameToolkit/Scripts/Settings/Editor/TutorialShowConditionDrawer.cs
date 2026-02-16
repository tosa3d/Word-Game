using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace WordsToolkit.Scripts.Settings.Editor
{
    [CustomPropertyDrawer(typeof(TutorialShowCondition))]
    public class TutorialShowConditionDrawer : PropertyDrawer
    {
        private static StyleSheet styleSheet;

        private void LoadStyleSheet()
        {
            if (styleSheet == null)
            {
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/WordConnectGameToolkit/Scripts/Settings/Editor/TutorialShowConditionDrawer.uss");
                
                if (styleSheet == null)
                {
                    Debug.LogWarning("Could not load TutorialShowConditionDrawer.uss");
                }
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            LoadStyleSheet();
            var root = new VisualElement();
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            var fieldsContainer = new VisualElement();
            fieldsContainer.AddToClassList("tutorial-fields-container");

            // Add label
            var propertyLabel = new Label("Tutorial Condition");
            propertyLabel.AddToClassList("property-label");
            fieldsContainer.Add(propertyLabel);

            // Create fields container
            var fieldsGroup = new VisualElement();
            fieldsGroup.AddToClassList("fields-container");

            var conditionProperty = property.FindPropertyRelative("showCondition");
            var conditionEnum = new EnumField();
            conditionEnum.BindProperty(conditionProperty);
            conditionEnum.style.flexGrow = 1;

            var levelProperty = property.FindPropertyRelative("level");
            var levelInt = new IntegerField();
            levelInt.BindProperty(levelProperty);

            // Add fields to the group
            fieldsGroup.Add(conditionEnum);
            fieldsGroup.Add(levelInt);

            // Add fields group to the main container
            fieldsContainer.Add(fieldsGroup);

            // Add container to root
            root.Add(fieldsContainer);

            // Register value change callbacks
            conditionEnum.RegisterValueChangedCallback(evt => 
            {
                // Refresh the level field visibility based on condition
                var condition = (ETutorialShowCondition)evt.newValue;
                levelInt.style.display = condition == ETutorialShowCondition.Level ? 
                    DisplayStyle.Flex : DisplayStyle.None;
                
                property.serializedObject.ApplyModifiedProperties();
            });

            // Initial setup of level field visibility
            var initialCondition = (ETutorialShowCondition)conditionProperty.enumValueIndex;
            levelInt.style.display = initialCondition == ETutorialShowCondition.Level ? 
                DisplayStyle.Flex : DisplayStyle.None;

            return root;
        }
    }
}