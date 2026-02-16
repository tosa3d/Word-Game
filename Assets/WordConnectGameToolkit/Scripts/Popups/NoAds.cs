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
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.Services.IAP;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Popups
{
    public class NoAds : Popup
    {
        public CustomButton removeAdsButton;
        public ProductID productID;
        [SerializeField]
        public TextMeshProUGUI priceText; // Add price display UI element

        private void OnEnable()
        {
            removeAdsButton.onClick.AddListener(RemoveAds);
            EventManager.GetEvent<string>(EGameEvent.PurchaseSucceeded).Subscribe(PurchaseSucceeded);
            
            // Set localized price when popup opens
            UpdatePriceDisplay();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.GetEvent<string>(EGameEvent.PurchaseSucceeded).Unsubscribe(PurchaseSucceeded);
        }

        private void PurchaseSucceeded(string obj)
        {
            if (obj == productID.ID)
            {
                adsManager.RemoveAds();
                Close();
            }
        }

        private void RemoveAds()
        {
            iapManager.BuyProduct(productID.ID);
        }

        private void UpdatePriceDisplay()
        {
            if (priceText != null && productID != null)
            {
                var localizedPrice = iapManager.GetProductLocalizedPriceString(productID.ID);
                if (!string.IsNullOrEmpty(localizedPrice))
                {
                    priceText.text = localizedPrice;
                }
            }
        }
    }
}