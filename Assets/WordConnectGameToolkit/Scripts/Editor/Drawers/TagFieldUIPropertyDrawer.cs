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

using System.Collections.Generic;

namespace WordsToolkit.Scripts.Editor.Drawers
{
    using UnityEditor;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;

    [CustomPropertyDrawer(typeof(TagFieldUIAttribute))]
    public class TagFieldUIPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            if (property.propertyType == SerializedPropertyType.String)
            {
                var dropdown = new DropdownField(property.displayName);
                dropdown.choices = new List<string>(UnityEditorInternal.InternalEditorUtility.tags);
                dropdown.value = property.stringValue;

                dropdown.RegisterValueChangedCallback(evt =>
                {
                    property.stringValue = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                });

                container.Add(dropdown);
            }
            else
            {
                var propertyField = new PropertyField(property);
                container.Add(propertyField);
            }

            return container;
        }
    }
}