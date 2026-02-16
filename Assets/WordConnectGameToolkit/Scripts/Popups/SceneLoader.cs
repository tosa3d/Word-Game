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
using UnityEngine;
using UnityEngine.SceneManagement;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.System;
using VContainer;

namespace WordsToolkit.Scripts.Popups
{
    public class SceneLoader : MonoBehaviour
    {
        public static Action<Scene> OnSceneLoadedCallback;
        public static event Action OnGameStart;
        private Loading loading;
        private Scene previouseScene;
        
        [Inject]
        private StateManager stateManager;

        private void Start()
        {
            CheckEvent(SceneManager.GetActiveScene());
        }

        public void StartGameScene()
        {
            // Let GameDataManager.GetLevel() handle loading the appropriate level
            // This ensures consistent behavior with our level selection logic
            Level level = GameDataManager.GetLevel();
            if (level != null)
            {
                GameDataManager.SetLevel(level);
            }
            OnGameStart?.Invoke();
            stateManager.CurrentState = EScreenStates.Game;
        }

        public void GoMain()
        {
            stateManager.CurrentState = EScreenStates.MainMenu;
        }

        private void CheckEvent(Scene scene)
        {
            if (previouseScene != scene)
            {
                OnSceneLoadedCallback?.Invoke(scene);
                previouseScene = scene;
            }
        }

    }
}