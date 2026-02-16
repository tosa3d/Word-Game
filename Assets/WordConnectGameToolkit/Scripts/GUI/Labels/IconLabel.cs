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
using DG.Tweening;
using UnityEngine;
using WordsToolkit.Scripts.Gameplay.Pool;
using WordsToolkit.Scripts.Popups.RewardsGift;

namespace WordsToolkit.Scripts.GUI.Labels
{
    public class IconLabel : LabelAnim, ILabelAnimation
    {
        public void Animate(GameObject sourceObject, Vector3 startPosition, string rewardDataCount, AudioClip sound, Action callback)
        {
            sourceObject.GetComponent<GiftBase>().HideText();
            var animatedObject = GetAnimatedObjectSource(sourceObject);
            Vector3 targetPos = targetTransform.position;
            animatedObject.transform.position = startPosition;
            var sequence = DOTween.Sequence();
            var _decreaseScaleTween = animatedObject.transform.DOScale(Vector3.one * 0.1f, .3f)
                .SetEase(Ease.Linear);
            sequence.Join(_decreaseScaleTween);
            var _movementTween = animatedObject.transform.DOMove(targetPos, .3f)
                .SetEase(Ease.Linear);

            sequence.Join(_movementTween);

            // bounce after movement
            var _bounceTween = animatedObject.transform.DOScale(Vector3.one * .3f, .4f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    _audioService.PlaySoundExclusive(sound);
                    // play fx
                    if (fxPrefab != null)
                    {
                        var fx = PoolObject.GetObject(fxPrefab, targetPos);
                        fx.transform.position = targetPos;
                        DOVirtual.DelayedCall(1f, () => { PoolObject.Return(fx); });
                    }
                    Release(animatedObject);
                    callback?.Invoke();
                    OnAnimationComplete?.Invoke();
                });
            sequence.Append(_bounceTween);

            sequence.Play();
        }
    }
}