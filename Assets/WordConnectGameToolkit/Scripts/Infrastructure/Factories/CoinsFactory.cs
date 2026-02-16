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
using VContainer;
using VContainer.Unity;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Utils;

namespace WordsToolkit.Scripts.Infrastructure.Factories
{
    public class CoinsFactory : ICoinsFactory
    {
        private readonly IObjectResolver _container;
        private IAudioService audioService;

        public CoinsFactory(IObjectResolver container, IAudioService audioService)
        {
            _container = container;
            this.audioService = audioService;
        }

        public void CreateCoins(GameObject prefabFX)
        {
            audioService.PlayCoins();
            var fx = _container.Instantiate(prefabFX, CustomButton.latestClickedButton.transform.position, Quaternion.identity);
            fx.transform.position = fx.transform.position.SnapZ();
            fx.transform.localScale = Vector3.one;
        }
    }

    public interface ICoinsFactory
    {
        void CreateCoins(GameObject prefabFX);
    }
}