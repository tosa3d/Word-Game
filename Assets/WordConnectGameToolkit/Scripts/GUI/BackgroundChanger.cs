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
using UnityEngine.UI;
using VContainer;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Infrastructure.Service;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.GUI
{
    public class BackgroundChanger : MonoBehaviour
    {
        [SerializeField]
        private Image image;

        private ILevelLoaderService levelLoaderService;
        private StateManager stateManager;
        [SerializeField]
        private Sprite mainBackground;

        [Inject]
        public void Construct(ILevelLoaderService levelLoaderService, StateManager stateManager)
        {
            this.levelLoaderService = levelLoaderService;
            this.levelLoaderService.OnLevelLoaded += OnLevelLoaded;
            SceneLoader.OnGameStart += SetBack;
            this.stateManager = stateManager;
        }

        private void OnEnable()
        {
            stateManager.OnStateChanged.AddListener(OnStateChanged);
        }

        private void OnStateChanged(EScreenStates arg0)
        {
            if (arg0 == EScreenStates.Game)
            {
                SetBack();
            }else if (arg0 == EScreenStates.MainMenu)
            {
                image.sprite = mainBackground;
            }
        }

        private void SetBack()
        {
            image.sprite = GameDataManager.GetLevel().background;
        }

        private void OnDestroy()
        {
            if (levelLoaderService != null)
            {
                levelLoaderService.OnLevelLoaded -= OnLevelLoaded;
                SceneLoader.OnGameStart -= SetBack;
                stateManager.OnStateChanged.RemoveListener(OnStateChanged);
            }
        }

        private void OnLevelLoaded(Level level)
        {
            image.sprite = level.background;
        }

        public void SetBackground(Sprite bg)
        {
            image.sprite = bg;
        }
    }
}