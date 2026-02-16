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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;
using WordConnectGameToolkit.Scripts.Settings;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Services.Ads;
using WordsToolkit.Scripts.Services.Ads.AdUnits;
using WordsToolkit.Scripts.Services.IAP;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.System;
#if UMP_AVAILABLE
using GoogleMobileAds.Ump.Api;
#endif

namespace WordsToolkit.Scripts.Services
{
    public class AdsManager : MonoBehaviour, IAdsManager
    {
        private readonly List<AdSetting> adList = new();
        private readonly List<AdUnit> adUnits = new();
        private readonly Dictionary<AdUnit, IAdLifecycleManager> lifecycleManagers = new();
        private EPlatforms platforms;
        private bool consentInfoUpdateInProgress = false;
        private bool adsInitialized = false;
        
        [Inject]
        private GameSettings gameSettings;

        [Inject]
        private IIAPManager iapManager;

        [SerializeField]
        private ProductID noAdsProduct;
        private InterstitialSettings interstitialSettings;
        private AdSetting[] adElements;

        private void Awake()
        {
            if (!gameSettings.enableAds)
            {
                return;
            }
            interstitialSettings = Resources.Load<InterstitialSettings>("Settings/AdsInterstitialSettings");
            adElements = Resources.Load<AdsSettings>("Settings/AdsSettings").adProfiles;
            StartConsentFlow();
        }

        private void StartConsentFlow()
        {
            if (consentInfoUpdateInProgress) return;
            
            consentInfoUpdateInProgress = true;

#if UMP_AVAILABLE && (UNITY_ANDROID || UNITY_IOS)
            var request = new ConsentRequestParameters();
            
            if (Debug.isDebugBuild || Application.isEditor)
            {
                var debugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugGeography.EEA
                };
                
                var testDeviceIds = new List<string>();
                // testDeviceIds.Add("YOUR-TEST-DEVICE-ID-HERE"); // Uncomment and add your device ID if needed
                
                if (testDeviceIds.Count > 0)
                {
                    debugSettings.TestDeviceHashedIds = testDeviceIds;
                }
                
                request.ConsentDebugSettings = debugSettings;
            }

            ConsentInformation.Update(request, OnConsentInfoUpdated);
#else
            InitializeAds();
#endif
        }

#if UMP_AVAILABLE
        private void OnConsentInfoUpdated(FormError consentError)
        {
            consentInfoUpdateInProgress = false;

            if (consentError != null)
            {
                Debug.LogError($"Consent info update failed: {consentError}");
                InitializeAds();
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(OnConsentFormDismissed);
        }

        private void OnConsentFormDismissed(FormError formError)
        {
            if (formError != null)
            {
                Debug.LogError($"Consent form error: {formError}");
            }

            if (ConsentInformation.CanRequestAds())
            {
                InitializeAds();
                RefreshBannerAds();
            }
            else
            {
                Debug.Log("User denied consent or consent not available");
                InitializeAds();
            }
        }
#endif

        private void InitializeAds()
        {
            if (adsInitialized) return;
            adsInitialized = true;


            platforms = GetPlatform();
            foreach (var t in adElements)
            {
                if (t.platforms == platforms && t.enable)
                {
                    if (Application.isEditor && !t.testInEditor)
                    {
                        continue;
                    }

                    adList.Add(t);
                    foreach (var adElement in t.adElements)
                    {
                        var adUnit = new AdUnit(adElement.placementId, adElement.adReference, t.adsHandler);
                        var lifecycleManager = new AdLifecycleManager(t.adsHandler);
                        lifecycleManagers[adUnit] = lifecycleManager;
                        adUnits.Add(adUnit);

                        if (adUnit.AdReference.adType == EAdType.Banner && !IsNoAdsPurchased())
                        {
                            lifecycleManager.Show(adUnit);
                        }
                    }

                    t.adsHandler?.Init(t.appId, false, new AdsListener(adUnits, lifecycleManagers));
                }
            }
        }

        private void Start()
        {
           if(IsNoAdsPurchased())
               RemoveAds();
        }

        private void OnEnable()
        {
            Popup.OnOpenPopup += OnOpenPopup;
            Popup.OnClosePopup += OnClosePopup;
        }

        private void OnDisable()
        {
            Popup.OnOpenPopup -= OnOpenPopup;
            Popup.OnClosePopup -= OnClosePopup;
        }

        private EPlatforms GetPlatform()
        {
            #if UNITY_ANDROID
            return EPlatforms.Android;
            #elif UNITY_IOS
            return EPlatforms.IOS;
            #elif UNITY_WEBGL
            return EPlatforms.WebGL;
            #else
            return EPlatforms.Windows;
            #endif
        }

        private void OnOpenPopup(Popup popup)
        {
            OnPopupTrigger(popup, true);
        }

        private void OnClosePopup(Popup popup)
        {
            OnPopupTrigger(popup, false);
        }

        private void OnPopupTrigger(Popup popup, bool open)
        {
            if (IsNoAdsPurchased() || !CanShowAds())
            {
                return;
            }

             // Get current level number
            int currentLevel = GameDataManager.GetLevelNum();

            // Check interstitial ads using InterstitialSettings
            if (interstitialSettings != null && interstitialSettings.interstitials != null)
            {
                foreach (var interstitialElement in interstitialSettings.interstitials)
                {
                    // Check if this interstitial should trigger based on popup
                    if (((open && interstitialElement.showOnOpen) || (!open && interstitialElement.showOnClose))
                        && popup.GetType() == interstitialElement.popup.GetType())
                    {
                        var adUnit = adUnits.Find(i => i.AdReference == interstitialElement.adReference);
                        if (adUnit == null || !adUnit.IsAvailable())
                        {
                            adUnit?.Load();
                            continue;
                        }

                        // Check level conditions
                        if (!IsLevelConditionMet(currentLevel, interstitialElement))
                        {
                            continue;
                        }

                        // Find placement ID for frequency tracking
                        string placementId = GetPlacementIdForAdReference(interstitialElement.adReference);
                        if (placementId == null) continue;

                        if (!IsFrequencyConditionMet(placementId, interstitialElement.frequency))
                        {
                            continue;
                        }

                        adUnit.Show();
                        adUnit.Load();
                        IncrementAdFrequency(placementId);
                        return;
                    }
                }
            }

            // Handle non-interstitial ads (banners, rewarded) using the original logic
            foreach (var ad in adList)
            {
                foreach (var adElement in ad.adElements)
                {
                    if (adElement.adReference.adType == EAdType.Interstitial)
                        continue; // Skip interstitials as they're handled above

                    var adUnit = adUnits.Find(i => i.AdReference == adElement.adReference);
                    if (!adUnit.IsAvailable())
                    {
                        adUnit.Load();
                        continue;
                    }

                    if (((open && adElement.popup.showOnOpen) || (!open && adElement.popup.showOnClose)) && popup.GetType() == adElement.popup.popup.GetType())
                    {
                        adUnit.Show();
                        adUnit.Load();
                        return;
                    }
                }
            }
        }

        public bool IsNoAdsPurchased()
        {
            return !gameSettings.enableAds || iapManager.IsProductPurchased(noAdsProduct.ID);
        }

        public void ShowAdByType(AdReference adRef, Action<string> shown)
        {
            if (!gameSettings.enableAds || !CanShowAds()) 
            {
                shown?.Invoke(null);
                return;
            }

            int currentLevel = GameDataManager.GetLevelNum();

            foreach (var adUnit in adUnits)
            {
                if (adUnit.AdReference == adRef && adUnit.IsAvailable())
                {
                    // Check level conditions for interstitial ads using InterstitialSettings
                    if (adRef.adType == EAdType.Interstitial && interstitialSettings != null)
                    {
                        var interstitialElement = interstitialSettings.interstitials?.FirstOrDefault(i => i.adReference == adRef);
                        if (interstitialElement != null)
                        {
                            if (!IsLevelConditionMet(currentLevel, interstitialElement))
                            {
                                shown?.Invoke(null);
                                return;
                            }

                            string placementId = GetPlacementIdForAdReference(adRef);
                            if (placementId != null)
                            {
                                if (!IsFrequencyConditionMet(placementId, interstitialElement.frequency))
                                {
                                    shown?.Invoke(null);
                                    return;
                                }

                                // Increment frequency counter
                                IncrementAdFrequency(placementId);
                            }
                        }
                    }

                    adUnit.OnShown = shown;
                    adUnit.Show();
                    adUnit.Load();
                    return;
                }
            }
        }

        public bool IsRewardedAvailable(AdReference adRef)
        {
            foreach (var adUnit in adUnits)
            {
                if (adUnit.AdReference == adRef)
                {
                    return lifecycleManagers[adUnit].IsAvailable(adUnit);
                }
            }

            return false;
        }

        public void RemoveAds()
        {
            foreach (var adUnit in adUnits)
            {
                if (adUnit.AdReference.adType == EAdType.Banner)
                {
                    lifecycleManagers[adUnit].Hide(adUnit);
                }
            }
        }

        private bool IsLevelConditionMet(int currentLevel, InterstitialAdElement popupSetting)
        {
            return currentLevel >= popupSetting.minLevel && currentLevel <= popupSetting.maxLevel;
        }

        private bool IsFrequencyConditionMet(string placementId, int frequency)
        {
            if (frequency <= 1) return true; // Always show if frequency is 1 or less

            int adShowCount = PlayerPrefs.GetInt($"AdCount_{placementId}", 0);
            return adShowCount % frequency == 0;
        }

        private void IncrementAdFrequency(string placementId)
        {
            int currentCount = PlayerPrefs.GetInt($"AdCount_{placementId}", 0);
            PlayerPrefs.SetInt($"AdCount_{placementId}", currentCount + 1);
            PlayerPrefs.Save();
        }

        private string GetPlacementIdForAdReference(AdReference adRef)
        {
            foreach (var ad in adList)
            {
                foreach (var adElement in ad.adElements)
                {
                    if (adElement.adReference == adRef)
                    {
                        return adElement.placementId;
                    }
                }
            }
            return null;
        }

        private bool CanShowAds()
        {
#if UMP_AVAILABLE
            return ConsentInformation.CanRequestAds();
#else
            return true;
#endif
        }

        public void RefreshBannerAds()
        {
            if (!CanShowAds() || IsNoAdsPurchased()) return;

            foreach (var adUnit in adUnits)
            {
                if (adUnit.AdReference.adType == EAdType.Banner)
                {
                    lifecycleManagers[adUnit].Show(adUnit);
                }
            }
        }

        public void ReconsiderUMPConsent()
        {
#if UMP_AVAILABLE && (UNITY_ANDROID || UNITY_IOS)
            ConsentInformation.Reset();
#endif
            StartConsentFlow();
        }
    }
}