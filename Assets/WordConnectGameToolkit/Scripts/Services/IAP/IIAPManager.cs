using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WordsToolkit.Scripts.Services.IAP
{
    public interface IIAPManager
    {
        Task InitializePurchasing(IEnumerable<(string productId, ProductTypeWrapper.ProductType productType)> products);
        void BuyProduct(string productId);
        decimal GetProductLocalizedPrice(string productId);
        string GetProductLocalizedPriceString(string productId);
        bool IsProductPurchased(string productId);
        void RestorePurchases(Action<bool, List<string>> action);
        void SubscribeToPurchaseEvent(Action<string> purchaseHandler);
        void UnsubscribeFromPurchaseEvent(Action<string> purchaseHandler);
    }
}