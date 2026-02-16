// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Services.Ads.AdUnits;

namespace WordsToolkit.Scripts.Editor.GUI
{
    [CustomEditor(typeof(CustomButton), true)]
    internal class CustomButtonDrawer : UnityEditor.Editor
    {
        private CustomButtonEditor customButtonEditor;

        private void OnEnable()
        {
            customButtonEditor = new CustomButtonEditor();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Add(new PropertyField(serializedObject.FindProperty("noSound")));
            var isRewardedField = new PropertyField(serializedObject.FindProperty("isRewarded"));
            isRewardedField.Bind(serializedObject);
            root.Add(isRewardedField);
            var adReferenceField = new PropertyField(serializedObject.FindProperty("adReference"));
            adReferenceField.Bind(serializedObject);
            var isRewardedProperty = serializedObject.FindProperty("isRewarded");
            var adReferenceProperty = serializedObject.FindProperty("adReference");
            adReferenceField.style.display = isRewardedProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
            isRewardedField.RegisterValueChangeCallback(evt =>
            {
                adReferenceField.style.display = isRewardedProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                if (isRewardedProperty.boolValue && adReferenceProperty.objectReferenceValue == null)
                {
                    adReferenceProperty.objectReferenceValue =  UnityEditor.AssetDatabase.LoadAssetAtPath<AdReference>("Assets/WordConnectGameToolkit/Prefabs/ScriptableAds/AdsTypes/Rewarded.asset");
                    serializedObject.ApplyModifiedProperties();
                }
            });
            root.Add(adReferenceField);
            root.Add(new PropertyField(serializedObject.FindProperty("overrideClickSound")));
            root.Add(new PropertyField(serializedObject.FindProperty("overrideAnimatorController")));

            var foldout = new Foldout { text = "Custom Button Settings", value = false };
            foldout.Add(customButtonEditor.CreateInspectorGUI(serializedObject));
            root.Add(foldout);
            // Draw default inspector for inherited fields
            var iterator = serializedObject.GetIterator();
            iterator.NextVisible(true); // Skip Script field
            var customButtonType = typeof(CustomButton);
            while (iterator.NextVisible(false))
            {
                var field = iterator.serializedObject.targetObject.GetType().GetField(iterator.name);
                if (field != null)
                {
                    // Show fields from parent classes AND child classes, but not from CustomButton itself
                    if (field.DeclaringType != customButtonType)
                    {
                        root.Add(new PropertyField(serializedObject.FindProperty(iterator.name)));
                    }
                }
            }

            // Draw fields from derived class if any
            var targetType = serializedObject.targetObject.GetType();
            if (targetType != customButtonType)
            {
                var derivedFields = targetType.GetFields(BindingFlags.Public | 
                                                       BindingFlags.NonPublic | 
                                                       BindingFlags.Instance |
                                                       BindingFlags.DeclaredOnly);
                foreach (var field in derivedFields)
                {
                    if (field.IsPrivate && !field.IsDefined(typeof(SerializeField), false))
                        continue;
                    
                    var property = serializedObject.FindProperty(field.Name);
                    if (property != null)
                    {
                        root.Add(new PropertyField(property));
                    }
                }
            }


            
            return root;
        }
    }
}