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

#if IRONSOURCE
using Unity.Services.LevelPlay;
#endif
using UnityEngine;
using WordsToolkit.Scripts.Services.Ads.AdUnits;
#if UMP_AVAILABLE
using GoogleMobileAds.Ump.Api;
#endif

namespace WordsToolkit.Scripts.Services.Ads.Networks
{
    [CreateAssetMenu(fileName = "IronsourceAdsHandler", menuName ="WordConnectGameToolkit/Ads/IronsourceAdsHandler")]
    public class IronsourceAdsHandler : AdsHandlerBase
    {
        private IAdsListener _listener;
        #if IRONSOURCE
        private LevelPlayInterstitialAd _interstitialAd;
        private LevelPlayRewardedAd _rewardedAd;
        #endif

        private void Init(string _id)
        {
            #if IRONSOURCE
            LevelPlay.ValidateIntegration();

            // Set consent for LevelPlay
            SetConsentStatus();

            // Register initialization events
            LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
            LevelPlay.OnInitFailed += SdkInitializationFailedEvent;

            LevelPlay.Init(_id);
            #endif
        }

        private void SetConsentStatus()
        {
            #if IRONSOURCE && UMP_AVAILABLE
            bool hasConsent = ConsentInformation.CanRequestAds();
            Debug.Log($"LevelPlay consent status: {hasConsent}");

            // Set consent for GDPR
            LevelPlay.SetConsent(hasConsent);

            // For CCPA compliance (optional)
            LevelPlay.SetMetaData("do_not_sell", hasConsent ? "false" : "true");
            #elif IRONSOURCE
            // Default to no consent if UMP not available
            LevelPlay.SetConsent(false);
            Debug.Log("UMP not available - setting LevelPlay consent to false");
            #endif
        }

        private void SetListener(IAdsListener listener)
        {
            _listener = listener;
            Debug.Log(_listener);
        }

        #if IRONSOURCE
        private void SdkInitializationCompletedEvent(LevelPlayConfiguration config)
        {
            Debug.Log("LevelPlay SdkInitializationCompletedEvent");
            _listener?.OnAdsInitialized();
        }

        private void SdkInitializationFailedEvent(LevelPlayInitError error)
        {
            Debug.Log($"LevelPlay SdkInitializationFailedEvent: {error}");
            _listener?.OnInitFailed();
        }

        // Interstitial event handlers
        private void InterstitialAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("LevelPlay OnInterstitialAdReady");
            _listener?.OnAdsLoaded(adInfo.AdUnitId);
        }

        private void InterstitialAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.Log($"LevelPlay InterstitialAdLoadFailedEvent: {error}");
            _listener?.OnAdsLoadFailed();
        }

        private void InterstitialAdDisplayFailedEvent(LevelPlayAdInfo levelPlayAdInfo, LevelPlayAdError levelPlayAdError)
        {
            Debug.Log($"LevelPlay InterstitialAdDisplayFailedEvent: {levelPlayAdError}");
            _listener?.OnAdsShowFailed();
            LevelPlay.SetPauseGame(false);
        }

        #if LEVELPLAY8
        private void InterstitialAdDisplayFailedEvent(LevelPlayAdDisplayInfoError obj)
        {
            Debug.Log($"LevelPlay InterstitialAdDisplayFailedEvent: {obj}");
            _listener?.OnAdsShowFailed();
            LevelPlay.SetPauseGame(false);
        }
        #endif


        // Rewarded video event handlers
        private void RewardedAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("LevelPlay OnRewardedVideoAdReady");
            _listener?.OnAdsLoaded(adInfo.AdUnitId);
        }

        private void RewardedAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.Log($"LevelPlay RewardedVideoAdLoadFailedEvent: {error}");
            _listener?.OnAdsLoadFailed();
        }

        private void RewardedAdDisplayFailedEvent(LevelPlayAdInfo levelPlayAdInfo, LevelPlayAdError levelPlayAdError)
        {
            Debug.Log($"LevelPlay RewardedVideoAdShowFailedEvent: {levelPlayAdError}");
            _listener?.OnAdsShowFailed();
            LevelPlay.SetPauseGame(false);
        }

        #if LEVELPLAY8
        private void RewardedAdDisplayFailedEvent(LevelPlayAdDisplayInfoError obj)
        {
            Debug.Log($"LevelPlay RewardedAdDisplayFailedEvent: {obj}");
            _listener?.OnAdsShowFailed();
            LevelPlay.SetPauseGame(false);
        }
        #endif

        private void RewardedAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            Debug.Log("LevelPlay Rewarded");
            _listener?.OnAdsShowComplete();
            LevelPlay.SetPauseGame(false);
        }
        private void InterstitialDisplayedEvent(LevelPlayAdInfo obj)
        {
            Debug.Log("LevelPlay InterstitialDisplayedEvent");
            _listener?.OnAdsShowStart();
            LevelPlay.SetPauseGame(false);
        }
        #endif


        public override void Init(string _id, bool adSettingTestMode, IAdsListener listener)
        {
            Debug.Log("LevelPlay Init");
            Init(_id);
            Debug.Log("LevelPlay SetListener");
            SetListener(listener);
        }

        public override void Show(AdUnit adUnit)
        {
            #if IRONSOURCE
            if (adUnit.AdReference.adType == EAdType.Interstitial && _interstitialAd != null)
            {
                if (_interstitialAd.IsAdReady())
                {
                    _interstitialAd.ShowAd();
                    LevelPlay.SetPauseGame(true);
                }
            }
            else if (adUnit.AdReference.adType == EAdType.Rewarded && _rewardedAd != null)
            {
                if (_rewardedAd.IsAdReady())
                {
                    _rewardedAd.ShowAd();
                    LevelPlay.SetPauseGame(true);
                }
            }

            _listener?.Show(adUnit);
            #endif
        }

        public override void Load(AdUnit adUnit)
        {
            #if IRONSOURCE
            if (adUnit.AdReference.adType == EAdType.Interstitial)
            {
                _interstitialAd = new LevelPlayInterstitialAd(adUnit.PlacementId);
                
                _interstitialAd.OnAdLoaded += InterstitialAdLoadedEvent;
                _interstitialAd.OnAdDisplayed += InterstitialDisplayedEvent;
                _interstitialAd.OnAdLoadFailed += InterstitialAdLoadFailedEvent;
                _interstitialAd.OnAdDisplayFailed += InterstitialAdDisplayFailedEvent;
                
                _interstitialAd.LoadAd();
            }
            else if (adUnit.AdReference.adType == EAdType.Rewarded)
            {
                _rewardedAd = new LevelPlayRewardedAd(adUnit.PlacementId);
                
                _rewardedAd.OnAdLoaded += RewardedAdLoadedEvent;
                _rewardedAd.OnAdLoadFailed += RewardedAdLoadFailedEvent;
                _rewardedAd.OnAdDisplayFailed += RewardedAdDisplayFailedEvent;
                _rewardedAd.OnAdRewarded += RewardedAdRewardedEvent;
                
                _rewardedAd.LoadAd();
            }
            #endif
        }




        public override bool IsAvailable(AdUnit adUnit)
        {
            #if IRONSOURCE
            if (adUnit.AdReference.adType == EAdType.Interstitial)
            {
                return _interstitialAd != null && _interstitialAd.IsAdReady();
            }

            if (adUnit.AdReference.adType == EAdType.Rewarded)
            {
                return _rewardedAd != null && _rewardedAd.IsAdReady();
            }
            #endif
            return false;
        }

        public override void Hide(AdUnit adUnit)
        {
        }
    }
}