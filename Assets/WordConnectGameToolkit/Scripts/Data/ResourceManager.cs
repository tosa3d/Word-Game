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
using WordsToolkit.Scripts.System;
using VContainer;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Infrastructure.Factories;

namespace WordsToolkit.Scripts.Data
{
    public class ResourceManager : MonoBehaviour
    {
        [Inject] private ICoinsFactory coinsFactory;
        [Inject] private MenuManager menuManager;

        private ResourceObject[] resources;

        public ResourceObject[] Resources
        {
            get
            {
                if (resources == null || resources.Length == 0)
                {
                    Init();
                }

                return resources;
            }
            set => resources = value;
        }

        public void Awake()
        {
            Init();
        }

        private void Init()
        {
            Resources = UnityEngine.Resources.LoadAll<ResourceObject>("Variables");
            foreach (var resource in Resources)
            {
                resource.LoadPrefs();
            }
        }

        public bool Consume(ResourceObject resource, int amount)
        {
            return resource.Consume(amount);
        }

        private void ShowShop(Popup shopPopup)
        {
            menuManager.ShowPopup(shopPopup);
        }

        public void PlaySpendEffect(GameObject fxPrefab)
        {
            if (fxPrefab != null)
            {
                coinsFactory.CreateCoins(fxPrefab);
            }
        }

        public bool ConsumeWithEffects(ResourceObject resource, int amount)
        {
            if (resource.Consume(amount))
            {
                PlaySpendEffect(resource.GetSpendEffectPrefab());
                return true;
            }

            ShowShop(resource.shopPopup);
            return false;
        }

        public ResourceObject GetResource(string resourceName)
        {
            foreach (var resource in Resources)
            {
                if (resource.name == resourceName)
                {
                    return resource;
                }
            }

            return null;
        }
    }
}