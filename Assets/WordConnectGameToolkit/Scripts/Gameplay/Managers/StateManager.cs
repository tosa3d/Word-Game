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
using UnityEngine.Events;

namespace WordsToolkit.Scripts.Gameplay.Managers
{
    public class StateManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] mainMenus;

        [SerializeField]
        private GameObject[] maps;

        [SerializeField]
        private GameObject[] games;

        private EScreenStates _currentState;
        
        public UnityEvent<EScreenStates> OnStateChanged = new UnityEvent<EScreenStates>();

        public EScreenStates CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                SetActiveState(mainMenus, _currentState == EScreenStates.MainMenu);
                SetActiveState(games, _currentState == EScreenStates.Game);
                OnStateChanged?.Invoke(_currentState);
            }
        }

        public void HideMain()
        {
            SetActiveState(mainMenus, false);
        }

        private void SetActiveState(GameObject[] gameObjects, bool isActive)
        {
            foreach (var gameObject in gameObjects)
            {
                if (gameObject != null && gameObject.activeSelf != isActive)
                {
                    gameObject.SetActive(isActive);
                }
            }
        }
    }

    public enum EScreenStates
    {
        MainMenu,
        Game
    }
}