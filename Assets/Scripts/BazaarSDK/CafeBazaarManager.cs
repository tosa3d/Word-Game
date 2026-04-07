using Bazaar.Data;
using Bazaar.Poolakey;
using Bazaar.Poolakey.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Popups;

public class CafeBazaarManager : MonoBehaviour
{
    public static CafeBazaarManager Instance { get; private set; }
    [TextArea]
    public string Key;
    public BazaarProductsData productsData; // Assign BazaarProducts ScriptableObject in Inspector
    private Payment _payment;
    public CustomButton[] customButtons;
    private string buyIdProduct;
    public string purchaseToken;
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        SecurityCheck securityCheck = SecurityCheck.Enable(Key);
        PaymentConfiguration paymentConfiguration = new PaymentConfiguration(securityCheck);
        _payment = new Payment(paymentConfiguration);
        _ = ConnectToPolackyAsync();
        foreach (var item in customButtons)
        {
            item.onClick.AddListener(delegate
            {
                BuyProductBazaar(item.GetComponent<ItemPurchase>().productID.androidId);
            });
        }
     
    }

    private async Task ConnectToPolackyAsync()
    {
        var result = await _payment.Connect(OnPaymentConnect);
        Debug.Log($"{result.message}, {result.stackTrace}");
    }

    private void OnPaymentConnect(Result<bool> result)
    {
        if (result.status == Status.Success)
        {
            Debug.Log("Connected to Poolakey successfully.");

        }
        else
        {
            Debug.LogError($"Failed to connect to Poolakey: {result.message}, {result.stackTrace}");
        }
    }
    public void BuyProductBazaar(string productid)
    {
      _=  BuyCoinsAsync(productid);
    }
    public async Task BuyCoinsAsync(string productid) {
        _ = _payment.Purchase(productid, Bazaar.Poolakey.Data.SKUDetails.Type.subscription, OnSubscribeStart, OnSubscribeComplete, "PAYLOAD");
    }

    private void OnSubscribeComplete(Result<PurchaseInfo> result)
    {
        buyIdProduct = result.data.productId;
        purchaseToken = result.data.purchaseToken;
        _ = _payment.Consume(result.data.purchaseToken, OnConsumeComleteCoin);

      
    }

    private void OnConsumeComleteCoin(Result<bool> result)
    {
        if(result.status == Status.Success)
        {
            // Get product data from assigned ScriptableObject
            if (productsData == null)
            {
                productsData = Resources.Load<BazaarProductsData>("BazaarProducts");
            }

            if (productsData != null)
            {
                var product = productsData.GetProduct(buyIdProduct);

                if (product != null)
                {
                    // Add coins
                    if (product.coinsReward > 0)
                    {
                        ResourceObject coinsResource = product.RewardValue;
                        if (coinsResource != null)
                        {
                            coinsResource.AddAnimated(product.coinsReward, Vector3.zero);
                            Debug.Log($"Added {product.coinsReward} coins from product {buyIdProduct}");
                        }
                    }

                    // Add gems
                    if (product.gemsReward > 0)
                    {
                        ResourceObject gemsResource = product.RewardValue;
                        if (gemsResource != null)
                        {
                            gemsResource.AddAnimated(product.gemsReward, Vector3.zero);
                            Debug.Log($"Added {product.gemsReward} gems from product {buyIdProduct}");
                        }
                    }

                    Debug.Log($"Purchase consumed successfully. Granted rewards for product: {buyIdProduct}");
                }
                else
                {
                    Debug.LogWarning($"Product {buyIdProduct} not found in BazaarProductsData");
                }
            }
            else
            {
                Debug.LogError("BazaarProductsData not found in Resources folder");
            }
        }
        else
        {
            Debug.LogError($"Failed to consume purchase: {result.message}, {result.stackTrace}");
        }
    }

    

    private void OnSubscribeStart(Result<PurchaseInfo> result)
    {
       
    }

    void OnApplicationQuit()
    {
        _payment.Disconnect();
    }
}
