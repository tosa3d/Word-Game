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

using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.GUI.Buttons.Boosts
{
    public class HammerBoostButton : BaseBoostButton
    {
        protected override void InitializePrice()
        {
            count = gameSettings.hammerBoostPrice;
            UpdatePriceDisplay();
        }

        protected override void ActivateBoost(bool hideButtons = true)
        {
            base.ActivateBoost(hideButtons);
            if (levelManager != null && !levelManager.hammerMode)
            {
                levelManager.hammerMode = true;
                EventManager.GetEvent<Tile>(EGameEvent.TileSelected).Subscribe(OnTileSelected);
            }
        }

        private void OnTileSelected(Tile obj)
        {
            DeactivateBoost();
        }

        protected override void DeactivateBoost()
        {
            levelManager.hammerMode = false;
            EventManager.GetEvent<Tile>(EGameEvent.TileSelected).Unsubscribe(OnTileSelected);
            base.DeactivateBoost();
        }
    }
}