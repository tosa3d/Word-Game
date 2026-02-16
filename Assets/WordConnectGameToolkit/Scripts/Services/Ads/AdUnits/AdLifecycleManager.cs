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

namespace WordsToolkit.Scripts.Services.Ads.AdUnits
{
    public class AdLifecycleManager : IAdLifecycleManager
    {
        private readonly AdsHandlerBase _adsHandler;

        public AdLifecycleManager(AdsHandlerBase adsHandler)
        {
            _adsHandler = adsHandler;
        }

        public void Load(AdUnit adUnit)
        {
            _adsHandler?.Load(adUnit);
        }

        public void Show(AdUnit adUnit)
        {
            _adsHandler?.Show(adUnit);
        }

        public void Hide(AdUnit adUnit)
        {
            _adsHandler?.Hide(adUnit);
        }

        public bool IsAvailable(AdUnit adUnit)
        {
            return _adsHandler != null && (_adsHandler.IsAvailable(adUnit) || adUnit.Loaded);
        }

        public void Complete(AdUnit adUnit)
        {
            adUnit.OnShown?.Invoke(adUnit.PlacementId);
        }

        public void Initialize(AdUnit adUnit)
        {
            adUnit.OnInitialized?.Invoke(adUnit.PlacementId);
        }
    }
} 