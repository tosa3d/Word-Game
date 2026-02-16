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

using UnityEngine;
using WordsToolkit.Scripts.Services.Ads.AdUnits;
#if UMP_AVAILABLE
using GoogleMobileAds.Ump.Api;
#endif
namespace WordsToolkit.Scripts.Services.Ads.Networks
{
    #if UNITY_ADS
    using UnityEngine.Advertisements;
    [CreateAssetMenu(fileName = "UnityAdsHandler", menuName ="WordConnectGameToolkit/Ads/UnityAdsHandler")]
    public class UnityAdsHandler : AdsHandlerBase, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        private bool rewardedLoaded;
        private bool initialized;
        private IAdsListener listener;

        #region IAdsShowable Implementations

        public override void Init(string _id, bool adSettingTestMode, IAdsListener listener)
        {
            SetConsentStatus();
            Advertisement.Initialize(_id, adSettingTestMode, this);
            this.listener = listener;
        }

        private void SetConsentStatus()
        {
            #if UNITY_ADS && UMP_AVAILABLE
            bool hasConsent = ConsentInformation.CanRequestAds();
            Debug.Log($"Unity Ads consent status: {hasConsent}");

            var consentMetaData = new MetaData("gdpr");
            consentMetaData.Set("consent", hasConsent);
            Advertisement.SetMetaData(consentMetaData);

            var privacyMetaData = new MetaData("privacy");
            privacyMetaData.Set("mode", "mixed");
            Advertisement.SetMetaData(privacyMetaData);
            #elif UNITY_ADS
            // Default to no consent if UMP not available
            var consentMetaData = new MetaData("gdpr");
            consentMetaData.Set("consent", false);
            Advertisement.SetMetaData(consentMetaData);
            Debug.Log("UMP not available - setting Unity Ads consent to false");
            #endif
        }

        public override void Show(AdUnit adUnit)
        {
            Advertisement.Show(adUnit.PlacementId, this);
            listener.Show(adUnit);
        }

        public override void Load(AdUnit adUnit)
        {
            Advertisement.Load(adUnit.PlacementId, this);
        }

        public override bool IsAvailable(AdUnit adUnit)
        {
            return false;
        }

        public override void Hide(AdUnit adUnit)
        {
        }

        #endregion

        #region Interface Implementations

        public void OnInitializationComplete()
        {
            DebugLog("Init Success");
            listener.OnAdsInitialized();
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            DebugLog($"Init Failed: [{error}]: {message}");
            listener.OnInitFailed();
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            DebugLog($"Load Success: {placementId}");
            listener.OnAdsLoaded(placementId);
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            DebugLog($"Load Failed: [{error}:{placementId}] {message}");
            listener.OnAdsLoadFailed();
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            DebugLog($"OnUnityAdsShowFailure: [{error}]: {message}");
            listener.OnAdsShowFailed();
        }

        public void OnUnityAdsShowStart(string placementId)
        {
            DebugLog($"OnUnityAdsShowStart: {placementId}");
            listener.OnAdsShowStart();
        }

        public void OnUnityAdsShowClick(string placementId)
        {
            DebugLog($"OnUnityAdsShowClick: {placementId}");
            listener.OnAdsShowClick();
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                DebugLog($"OnUnityAdsShowComplete: {placementId}");
                listener.OnAdsShowComplete();
            }
        }

        //wrapper around debug.log to allow broadcasting log strings to the UI
        private void DebugLog(string msg)
        {
            Debug.Log(msg);
        }

        #endregion
    }

    #endif
}