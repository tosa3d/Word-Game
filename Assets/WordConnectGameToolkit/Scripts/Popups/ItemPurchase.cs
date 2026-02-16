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
using TMPro;
using UnityEngine;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Services.IAP;

namespace WordsToolkit.Scripts.Popups
{
    public class ItemPurchase : MonoBehaviour
    {
        public CustomButton BuyItemButton;
        public TextMeshProUGUI price;
        public TextMeshProUGUI count;
        public TextMeshProUGUI discountPercent;

        [HideInInspector]
        public KeyValuePair<ProductID, int> settingsShopItem;

        public ProductID productID;

        [SerializeField]
        public ResourceObject resource;

        private IIAPManager iapManager;

        public void Init(KeyValuePair<ProductID, int> settingsShopItem, IIAPManager iapManager)
        {
            this.settingsShopItem = settingsShopItem;
            productID = settingsShopItem.Key;
            
            if (count != null)
            {
                count.text = settingsShopItem.Value.ToString();
            }

            if (productID != null && price != null)
            {
                var priceValue = iapManager.GetProductLocalizedPrice(productID.ID);
                if (priceValue > 0.01m)
                {
                    price.text = iapManager.GetProductLocalizedPriceString(productID.ID);
                }
            }

            BuyItemButton?.onClick.AddListener(BuyCoins);

            // Check if non-consumable item is already purchased
            if (productID != null && productID.productType == ProductTypeWrapper.ProductType.NonConsumable && 
                PlayerPrefs.GetInt("Purchased_" + productID.ID, 0) == 1)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Removed old initialization code since it's now in Init
        }

        private void BuyCoins()
        {
            if (productID != null)
            {
                GetComponentInParent<CoinsShop>().BuyCoins(productID.ID);
            }
        }
    }
}