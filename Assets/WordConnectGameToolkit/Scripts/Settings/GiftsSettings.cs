using System;
using UnityEngine;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Popups.RewardsGift;

namespace WordsToolkit.Scripts.Settings
{
    public class GiftsSettings : ScriptableObject
    {
        public GiftData[] gifts;
    }

    [Serializable]
    public class GiftData
    {
        public GiftBase giftPrefab;
        public int giftCount;
        public float giftChance;
        public ResourceObject resourceObject;
        [TagFieldUI]
        public string tagUIElement;

    }
}