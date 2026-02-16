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

using TMPro;
using UnityEngine;
using VContainer;

namespace WordsToolkit.Scripts.Localization
{
    public class LocalizedTextMeshProUGUI : TextMeshProUGUI
    {
        [SerializeField]
        public string instanceID;

        private string originalText;
        private ILocalizationService _localizationService;

        [Inject]
        public void Construct(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            originalText = text;
            UpdateText();
        }

        public void UpdateText()
        {
            if (instanceID != "")
            {
                text = _localizationService?.GetText(instanceID, originalText) ?? originalText;
            }
        }
    }
}