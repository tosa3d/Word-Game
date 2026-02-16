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

namespace WordsToolkit.Scripts.Services.Ads.Networks
{
    [CreateAssetMenu(fileName = "IronsourceBannerHandler", menuName ="WordConnectGameToolkit/Ads/IronsourceBannerHandler")]
    public class IronsourceBannerHandler : AdsHandlerBase
    {
        private IAdsListener _listener;
        #if IRONSOURCE
        private LevelPlayBannerAd _bannerAd;
        private LevelPlayBannerAd.Config.Builder configBuilder;
        #endif

        private void Init(string _id)
        {
            #if IRONSOURCE
            LevelPlay.ValidateIntegration();
            LevelPlay.Init(_id);
            #endif
        }

        private void SetListener(IAdsListener listener)
        {
            _listener = listener;
        }

        #if IRONSOURCE
        private void BannerAdLoadedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("LevelPlay Banner ad loaded");
            _listener?.OnAdsLoaded(adInfo.AdUnitId);
        }

        private void BannerAdLoadFailedEvent(LevelPlayAdError error)
        {
            Debug.Log($"LevelPlay Banner ad load failed. Error: {error}");
            _listener?.OnAdsLoadFailed();
        }

        private void BannerAdClickedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("LevelPlay Banner ad clicked");
        }

        private void BannerAdDisplayedEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("LevelPlay Banner ad displayed");
        }

        private void BannerAdDisplayFailedEvent(LevelPlayAdInfo levelPlayAdInfo, LevelPlayAdError levelPlayAdError)
        {
            Debug.Log($"LevelPlay Banner ad display failed. Error: {levelPlayAdError}");
        }

        #if LEVELPLAY8
        private void BannerAdDisplayFailedEvent(LevelPlayAdDisplayInfoError error)
        {
            Debug.Log($"LevelPlay Banner ad display failed. Error: {error}");
        }
        #endif

        private void BannerAdLeftApplicationEvent(LevelPlayAdInfo adInfo)
        {
            Debug.Log("LevelPlay Banner ad caused app to leave");
        }

        private LevelPlayBannerAd GetBannerAd(AdUnit adUnit)
        {
            configBuilder ??= new LevelPlayBannerAd.Config.Builder();
            #if LEVELPLAY9
            configBuilder.SetSize(LevelPlayAdSize.BANNER);
            #elif LEVELPLAY8
            configBuilder.SetSize(com.unity3d.mediation.LevelPlayAdSize.BANNER);
            #endif
            #if LEVELPLAY9
            configBuilder.SetPosition(LevelPlayBannerPosition.BottomCenter);
            #elif LEVELPLAY8
            configBuilder.SetPosition(com.unity3d.mediation.LevelPlayBannerPosition.BottomCenter);
            #endif
            configBuilder.SetDisplayOnLoad(true);
            #if UNITY_ANDROID
            configBuilder.SetRespectSafeArea(true); // Only relevant for Android
            #endif
            configBuilder.SetPlacementName("bannerPlacement");
            configBuilder.SetBidFloor(1.0); // Minimum bid price in USD
            var bannerConfig = configBuilder.Build();
            var bannerAd = new LevelPlayBannerAd(adUnit.PlacementId, bannerConfig);

            bannerAd.OnAdLoaded += BannerAdLoadedEvent;
            bannerAd.OnAdLoadFailed += BannerAdLoadFailedEvent;
            bannerAd.OnAdClicked += BannerAdClickedEvent;
            bannerAd.OnAdDisplayed += BannerAdDisplayedEvent;
            bannerAd.OnAdDisplayFailed += BannerAdDisplayFailedEvent;
            bannerAd.OnAdLeftApplication += BannerAdLeftApplicationEvent;

            return bannerAd;
        }
        #endif

        public override void Init(string _id, bool adSettingTestMode, IAdsListener listener)
        {
            Debug.Log("LevelPlay Banner Init");
            Init(_id);
            SetListener(listener);
        }

        public override void Show(AdUnit adUnit)
        {
            #if IRONSOURCE
            if (_bannerAd == null)
            {
                _bannerAd = GetBannerAd(adUnit);
            }
            if (adUnit.AdReference.adType == EAdType.Banner && _bannerAd != null)
            {
                _bannerAd.ShowAd();
                _listener?.Show(adUnit);
            }
            #endif
        }


        public override void Load(AdUnit adUnit)
        {
            #if IRONSOURCE
            _bannerAd.LoadAd();
            #endif
        }

        public override bool IsAvailable(AdUnit adUnit)
        {
            // IronSource doesn't provide a direct method to check if a banner is available
            // You might want to implement your own logic to track banner availability
            return true;
        }

        public override void Hide(AdUnit adUnit)
        {
            #if IRONSOURCE
            if (adUnit.AdReference.adType == EAdType.Banner && _bannerAd != null)
            {
                _bannerAd.HideAd();
            }
            #endif
        }
    }
}