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
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.System;
using DG.Tweening;
using VContainer;
using WordsToolkit.Scripts.Levels;

namespace WordsToolkit.Scripts.Gameplay.Managers
{
    public partial class LevelManager
    {
        [Inject]
        private MenuManager menuManager;

        private void HandleGameStateChange(EGameState newState)
        {
            switch (newState)
            {
                case EGameState.PrepareGame:
                    EventManager.GameStatus = EGameState.Playing;
                    break;
                case EGameState.Playing:
                    if (HasTimer)
                    {
                        StartTimer();
                    }
                    break;

                case EGameState.PreWin:
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        ShowPreWinPopup();
                    });
                    break;
                case EGameState.Win:
                    if (_levelData.GroupIsFinished())
                        ShowMenuFinished();
                    else
                        ShowMenuPlay();
                    break;
                case EGameState.PreFailed:
                    menuManager.ShowPopup<PreFailed>(null, PrefailedResult);
                    break;
                default:
                    break;
            }
        }

        private void ShowMenuFinished()
        {
            menuManager.ShowPopup<Finish>(null, _=>ShowMenuPlay());
        }

        private void PrefailedResult(EPopupResult obj)
        {
            if (obj == EPopupResult.Continue)
            {
                TimerLimit += gameSettings.continueTime;
                EventManager.GameStatus = EGameState.Playing;
            }
            else if(obj == EPopupResult.Cancel)
            {
                sceneLoader.GoMain();
            }
        }

        private void ShowPreWinPopup()
        {
           var p = menuManager.ShowPopup<PreWin>(null, _ =>
           {
               PanelWinAnimation();
               DOVirtual.DelayedCall(1f, () =>
               {
                    EventManager.GameStatus = EGameState.Win;
               });
           });
           p.transform.position = bubbleAnchor.position;
        }

        private void ShowMenuPlay()
        {
            if (Resources.LoadAll<Level>("Levels").Length <= _levelData.number)
            {
                menuManager.ShowPopup<ComingSoon>();
            }
            else
            {
                menuManager.ShowPopup<MenuPlay>();
            }
        }
    }
}