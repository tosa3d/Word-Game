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

using System.Collections.Generic;
using UnityEngine;
using WordsToolkit.Scripts.Services.Ads.AdUnits;

namespace WordsToolkit.Scripts.Services.Ads
{
    public class AdsListener : IAdsListener
    {
        private readonly List<AdUnit> adUnits;
        private readonly Dictionary<AdUnit, IAdLifecycleManager> lifecycleManagers;
        private bool available;
        private AdUnit _adUnit;

        public AdsListener(List<AdUnit> adUnits, Dictionary<AdUnit, IAdLifecycleManager> lifecycleManagers)
        {
            this.adUnits = adUnits;
            this.lifecycleManagers = lifecycleManagers;
        }

        public void Show(AdUnit adUnit)
        {
            Debug.Log("Show ad " + adUnit.PlacementId);
            _adUnit = adUnit;
        }

        public void OnAdsInitialized()
        {
            Debug.Log("Ads initialized");
            foreach (var adUnit in adUnits)
            {
                lifecycleManagers[adUnit].Initialize(adUnit);
            }
        }

        public void OnAdsLoaded(string placementId)
        {
            foreach (var adUnit in adUnits)
            {
                if (adUnit.PlacementId == placementId)
                {
                    adUnit.Loaded = true;
                }
            }
        }

        public void OnInitFailed()
        {
            Debug.Log("Ads init failed, check assigned ad handler");
        }

        public void OnAdsLoadFailed()
        {
        }

        public void OnAdsShowFailed()
        {
            if (_adUnit != null)
            {
                _adUnit.Loaded = false;
            }

            _adUnit = null;
        }

        public void OnAdsShowStart()
        {
        }

        public void OnAdsShowClick()
        {
        }

        public void OnAdsShowComplete()
        {
            if (_adUnit != null)
            {
                lifecycleManagers[_adUnit].Complete(_adUnit);
                _adUnit = null;
            }
        }
    }
}