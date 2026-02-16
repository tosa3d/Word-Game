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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.Unity_Reorderable_List_master.List;
using WordsToolkit.Scripts.Unity_Reorderable_List_master.List.Attributes;

namespace WordsToolkit.Scripts.Localization
{
    public class LocalizeText : MonoBehaviour
    {
        private TextMeshProUGUI textObject;
        public string instanceID;

        [SerializeField]
        [Reorderable(elementNameProperty = "language")]
        [HideInInspector]
        public MyList objects;

        private string _originalText;
        private string _currentText;

        private void Awake()
        {
            textObject = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            //take text from target editor
            _originalText = textObject.text;
            _currentText = LocalizationManager.instance.GetText(instanceID, _originalText);
            textObject.text = _currentText;
        }

//        private void Update()
//        {
//            if (_currentText != "")
//            {
//                textObject.text = _currentText;
//            }
//        }
    }

    [Serializable]
    public class MyList : ReorderableArray<LanguageObject>
    {
        private SystemLanguage GetSystemLanguage(DebugSettings _debugSettings)
        {
            SystemLanguage lang;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
            {
                // Use TestLanguageCode from DebugSettings and convert to SystemLanguage
                try
                {
                    var cultureInfo = new CultureInfo(_debugSettings.TestLanguageCode);
                    lang = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), cultureInfo.EnglishName);
                }
                catch (Exception)
                {
                    Debug.LogWarning($"Failed to parse TestLanguageCode '{_debugSettings.TestLanguageCode}'. Using English as fallback.");
                    lang = SystemLanguage.English;
                }
            }
            else
            {
                lang = Application.systemLanguage;
            }

            return this.Any(i => i.language == lang) ? lang : SystemLanguage.English;
        }

        public IEnumerable<LanguageObject> GetText(DebugSettings _debugSettings)
        {
            var systemLanguage = GetSystemLanguage(_debugSettings);
            return GetText(systemLanguage);
        }

        public IEnumerable<LanguageObject> GetText(SystemLanguage systemLanguage)
        {
            return this.Where(i => i.language == systemLanguage);
        }
    }
}