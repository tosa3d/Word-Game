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
using System.Collections;

namespace WordsToolkit.Scripts.GUI.Tutorials
{
    public class TutorialPopupUI : TutorialPopupBase
    {
        [SerializeField]
        protected GameObject arrow;

        protected Coroutine arrowAnimation;
        protected GameObject targetObject;
        protected float arrowOffset = 1.0f;

        public override void AfterShowAnimation()
        {
            base.AfterShowAnimation();

            targetObject = GetObjectsOfTagsToShow()[0];
            if (targetObject != null && arrow != null)
            {
                Vector3 targetPos = targetObject.transform.position;
                if (targetPos.x < 0)
                {
                    arrow.transform.localScale = new Vector3(-1, 1, 1);
                    arrowOffset = 1.0f;
                }
                else
                {
                    arrow.transform.localScale = new Vector3(1, 1, 1);
                    arrowOffset = -1.0f;
                }
                
                UpdateArrowPosition();
                arrow.SetActive(true);
                arrowAnimation = StartCoroutine(AnimateArrow());
            }
        }

        protected void UpdateArrowPosition()
        {
            if (targetObject != null && arrow != null)
            {
                Vector3 targetPos = targetObject.transform.position;
                arrow.transform.position = new Vector3(targetPos.x + arrowOffset, targetPos.y, targetPos.z);
            }
        }

        protected IEnumerator AnimateArrow()
        {
            float time = 0f;
            float wobbleAmount = 0.1f;
            
            while (true)
            {
                time += Time.deltaTime * 2f; // Complete one cycle in 0.5 seconds
                UpdateArrowPosition();
                
                // Add small wobble animation
                float wobble = Mathf.PingPong(time, 1f) * wobbleAmount;
                Vector3 currentPos = arrow.transform.position;
                arrow.transform.position = new Vector3(currentPos.x + (arrowOffset > 0 ? wobble : -wobble), currentPos.y, currentPos.z);
                
                yield return null;
            }
        }

        public override void AfterHideAnimation()
        {
            base.AfterHideAnimation();
            if (arrow != null)
            {
                arrow.SetActive(false);
                if (arrowAnimation != null)
                {
                    StopCoroutine(arrowAnimation);
                    arrowAnimation = null;
                }
            }
            targetObject = null;
        }
    }
}
