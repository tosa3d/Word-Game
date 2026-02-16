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
using WordsToolkit.Scripts.Data;
using Object = UnityEngine.Object;

namespace WordsToolkit.Scripts.GUI.Labels
{
    public static class ResourceAnimationController
    {
        public static void AnimateForResource(ResourceObject resourceObject, GameObject animatedObject, Vector3 startPosition, string rewardDataCount, AudioClip sound,
            Action callback)
        {
            var label = FindLabelForResource(resourceObject);
            if (label != null)
            {
                label.Animate(animatedObject, startPosition, rewardDataCount, sound, callback);
            }
            else
            {
                callback?.Invoke();
            }
        }

        private static ILabelAnimation FindLabelForResource(ResourceObject resourceObject)
        {
            var allLabels = Object.FindObjectsOfType<LabelAnim>();
            foreach (var label in allLabels)
            {
                if (label.associatedResource == resourceObject)
                {
                    return label as ILabelAnimation;
                }
            }

            return null;
        }
    }
}