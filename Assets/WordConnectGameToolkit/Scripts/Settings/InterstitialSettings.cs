using System;
using System.Collections.Generic;
using UnityEngine;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Services.Ads.AdUnits;
using WordsToolkit.Scripts.Settings;

namespace WordConnectGameToolkit.Scripts.Settings
{
    [CreateAssetMenu(fileName = "InterstitialSettings", menuName = "WordConnectGameToolkit/Settings/InterstitialSettings", order = 1)]
    public class InterstitialSettings : ScriptableObject
    {
        public InterstitialAdElement[] interstitials;

        public void PopulateFromAdsSettings(AdsSettings adsSettings)
        {
            if (adsSettings == null) return;

            var interstitialElements = new List<InterstitialAdElement>();

            foreach (var adProfile in adsSettings.adProfiles)
            {
                if (!adProfile.enable) continue;

                foreach (var adElement in adProfile.adElements)
                {
                    if (adElement.adReference != null && adElement.adReference.adType == EAdType.Interstitial)
                    {
                        var interstitialElement = new InterstitialAdElement
                        {
                            elementName = adElement.adReference.name,
                            adReference = adElement.adReference,
                            popup = adElement.popup.popup,
                            showOnOpen = adElement.popup.showOnOpen,
                            showOnClose = adElement.popup.showOnClose,
                        };

                        interstitialElements.Add(interstitialElement);
                    }
                }
            }

            interstitials = interstitialElements.ToArray();
        }
    }


    [Serializable]
    public class InterstitialAdElement
    {
        [HideInInspector]
        public string elementName;
        
        public AdReference adReference;

        [Header("Popup that triggers Interstitial ads")]
        public Popup popup;
        public bool showOnOpen;
        public bool showOnClose;
        [Header("Level Conditions")]
        public int minLevel = 1;
        public int maxLevel = 1000;
        public int frequency = 1;
    }
}
