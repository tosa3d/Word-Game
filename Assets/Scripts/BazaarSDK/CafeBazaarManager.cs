using Bazaar.Data;
using Bazaar.Poolakey;
using Bazaar.Poolakey.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using WordsToolkit.Scripts.GUI.Buttons;

public class CafeBazaarManager : MonoBehaviour
{
    public static CafeBazaarManager Instance { get; private set; }
    [TextArea]
    public string Key;
    private Payment _payment;
    public CustomButton[] customButtons; 
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        SecurityCheck securityCheck = SecurityCheck.Enable(Key);
        PaymentConfiguration paymentConfiguration = new PaymentConfiguration(securityCheck);
        _payment = new Payment(paymentConfiguration);
        _ = ConnectToPolackyAsync();
        customButtons[0].onClick.AddListener(BuyProductBazaar);
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
    public void BuyProductBazaar()
    {
      _=  BuyCoinsAsync();
    }
    public async Task BuyCoinsAsync() {
        _ = _payment.Purchase("Kolookhpacklookh001", Bazaar.Poolakey.Data.SKUDetails.Type.subscription, OnSubscribeStart, OnSubscribeComplete, "PAYLOAD");
    }

    private void OnSubscribeComplete(Result<PurchaseInfo> result)
    {
        _ = _payment.Consume(result.data.productId, OnConsumeComleteCoin);

      
    }

    private void OnConsumeComleteCoin(Result<bool> result)
    {
       if(result.status == Status.Success)
        {

            Debug.Log("Purchase consumed successfully.");
            // Here you can add logic to grant the purchased item to the user
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
