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

using DG.Tweening;
using TMPro;
using UnityEngine;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Popups
{
    public class PreFailed : PopupWithCurrencyLabel
    {
        public TextMeshProUGUI continuePrice;
        public TextMeshProUGUI timerText;

        public CustomButton continueButton;
        public CustomButton rewardButton;
        public CustomButton againButton;
        private int price;
        [SerializeField]
        private AudioClip warningTime;

        [SerializeField]
        private AudioClip failedSound;

        private void OnEnable()
        {
            price = gameSettings.continuePrice;
            continuePrice.text = price.ToString();
            continueButton.onClick.AddListener(Continue);

            closeButton.onClick.AddListener(Cancel);

            timerText.text = "<color=#FFF76B> +" + gameSettings.continueTime + "</color> sec";
            audioService.PlaySound(warningTime);
            rewardButton.gameObject.SetActive(gameSettings.enableAds);
            againButton.onClick.AddListener(Again);
        }

        private void Cancel()
        {
            result = EPopupResult.Cancel;
            audioService.PlaySound(failedSound);
            Close();
        }

        public override void Close()
        {
            base.Close();
        }

        private void Again()
        {
            gameManager.RestartLevel();
            Close();
        }

        private void Continue()
        {
            var coinsResource = resourceManager.GetResource("Coins");
            if (resourceManager.ConsumeWithEffects(coinsResource, price))
            {
                StopInteration();
                DOVirtual.DelayedCall(0.5f, ContinueGame);
            }
        }

        public void ContinueGame()
        {
            result = EPopupResult.Continue;
            EventManager.GameStatus = EGameState.Playing;
            Close();
        }
    }
}