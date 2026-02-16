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

using UnityEngine;
using WordsToolkit.Scripts.Localization;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.GUI.Tutorials
{
    public class TutorialPopupBase : Popup
    {
        [SerializeField]
        private LocalizedTextMeshProUGUI text;

        protected TutorialSettingsData tutorialData;

        protected GameObject[] GetObjectsOfTagsToShow()
        {
            if (tagsToShow == null || tagsToShow.Length == 0)
                return null;

            var tags = new GameObject[tagsToShow.Length];
            for (int i = 0; i < tagsToShow.Length; i++)
            {
                var t = GameObject.FindGameObjectWithTag(tagsToShow[i]);
                if (t == null)
                    Debug.LogError($"TutorialPopupBase: GetTagsToShow: {tagsToShow[i]} found: {t != null}");
                if (t != null)
                {
                    tags[i] = t;
                }
            }

            return tags;
        }

        public void SetTitle(string getText)
        {
            if (text != null)
            {
                text.text = getText;
            }
        }

        public void SetData(TutorialSettingsData tutorialData)
        {
            this.tutorialData = tutorialData;
            ShowTags(tutorialData.tagsToShow);
        }

        public TutorialSettingsData GetData()
        {
            return tutorialData;
        }
    }
}