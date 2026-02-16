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
using DG.Tweening;
using UnityEngine;
using VContainer;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.GUI.Tutorials
{
    public class TutorialPopupGame : TutorialWordSubstitution
    {
        public override void AfterShowAnimation()
        {
            base.AfterShowAnimation();
            var words = levelManager.GetCurrentLevel().GetWords(gameManager.language);
            ReplaceWordForTutorial(words[0]);
            SelectWordAnimation(words[0]);
            var tiles = fieldManager.GetTilesWord(words[0]);
            if (tiles != null && tiles.Count > 0)
            {
                foreach (var tile in tiles)
                {
                    if (tile != null)
                    {
                        MakeObjectVisible(tile.gameObject);
                    }
                }
            }
        }
    }
}