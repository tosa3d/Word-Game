using UnityEngine;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Settings;

[System.Serializable]
public class BazaarProduct
{
    public string productId;
    public int coinsReward;
    public int gemsReward;
  
    public ResourceObject RewardValue;
}

[CreateAssetMenu(fileName = "BazaarProducts", menuName = "Bazaar/Products")]
public class BazaarProductsData : ScriptableObject
{
    public BazaarProduct[] products;

    public BazaarProduct GetProduct(string productId)
    {
        foreach (var product in products)
        {
            if (product.productId == productId)
                return product;
        }
        return null;
    }
}
