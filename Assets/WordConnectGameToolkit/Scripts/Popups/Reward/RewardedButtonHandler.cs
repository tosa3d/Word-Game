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
using UnityEngine.Events;
using VContainer;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.Services.Ads.AdUnits;

namespace WordsToolkit.Scripts.Popups.Reward
{
    public class RewardedButtonHandler : MonoBehaviour
    {
        [SerializeField]
        public AdReference adReference;

        [SerializeField]
        private CustomButton rewardedButton;

        [SerializeField]
        public UnityEvent onRewardedAdComplete;

        [SerializeField]
        private UnityEvent onRewardedShow;

        [Inject]
        private IAdsManager adsManager;

        private void Awake()
        {
            rewardedButton?.onClick.AddListener(ShowRewardedAd);
        }

        public void ShowRewardedAd()
        {
            if (adsManager.IsRewardedAvailable(adReference))
            {
                onRewardedShow?.Invoke();
                adsManager.ShowAdByType(adReference, _ => onRewardedAdComplete?.Invoke());
            }
            else
            {
                Debug.Log("Rewarded ad is not available");
            }
        }
    }
}