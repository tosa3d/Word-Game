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

using System.Linq;
using TMPro;
using UnityEngine;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Services.IAP;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Popups
{
    public class CoinsShop : PopupWithCurrencyLabel
    {
        public ItemPurchase[] packs;
        private CoinsShopSettings shopSettings;
        public ProductID noAdsProduct;
        [SerializeField]
        public TextMeshProUGUI noAdsPriceText;
        [SerializeField]
        private AudioClip coinsSound;

        private void OnEnable()
        {
            shopSettings = Resources.Load<CoinsShopSettings>("Settings/CoinsShopSettings");
            foreach (var itemPurchase in packs)
            {
                if (shopSettings.coinsProducts.Count > 0)
                {
                    var productID = itemPurchase.productID;
                    if (shopSettings.coinsProducts.TryToGetPair(kvp => kvp.Key == productID, out var settingsShopItem))
                    {
                        itemPurchase.Init(settingsShopItem, iapManager);
                    }
                }
            }

            EventManager.GetEvent<string>(EGameEvent.PurchaseSucceeded).Subscribe(PurchaseSucceded);
            
            // Update NoAds price display
            UpdateNoAdsPriceDisplay();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventManager.GetEvent<string>(EGameEvent.PurchaseSucceeded).Unsubscribe(PurchaseSucceded);
        }

        private void PurchaseSucceded(string id)
        {
            var shopItem = packs.First(i => i.productID.ID == id);
            var count = shopItem.settingsShopItem.Value;
            shopItem.resource.AddAnimated(count, shopItem.BuyItemButton.transform.position, animationSourceObject: null, callback: () =>
            {
                GetComponentInParent<Popup>().CloseDelay();
            });

            // If the item is non-consumable, mark it as purchased
            if (shopItem.productID.productType == ProductTypeWrapper.ProductType.NonConsumable)
            {
                PlayerPrefs.SetInt("Purchased_" + id, 1);
                PlayerPrefs.Save();

                // Disable the button for this item
                var pack = shopItem;
                if (pack.BuyItemButton != null)
                {
                    pack.BuyItemButton.interactable = false;
                }
            }
            if (id == noAdsProduct.ID)
            {
                adsManager.RemoveAds();
                Close();
            }
        }

        public void BuyCoins(string id)
        {
            // StopInteration();
#if UNITY_WEBGL
            gameManager.PurchaseSucceeded(id);
#else
            iapManager.BuyProduct(id);
#endif
        }

        private void UpdateNoAdsPriceDisplay()
        {
            if (noAdsPriceText != null && noAdsProduct != null)
            {
                var localizedPrice = iapManager.GetProductLocalizedPriceString(noAdsProduct.ID);
                if (!string.IsNullOrEmpty(localizedPrice))
                {
                    noAdsPriceText.text = localizedPrice;
                }
            }
        }
    }
}