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
using Random = UnityEngine.Random;

namespace WordsToolkit.Scripts.GUI.Labels
{
    public class CurrencyLabelAnimation : LabelAnim, ILabelAnimation
    {
        public void Animate(GameObject sourceObject, Vector3 startPosition, string rewardDataCount, AudioClip sound, Action callback)
        {
            var count = 0;
            var animateCount = 4;
            var targetPosition = targetTransform.transform.position;
            // if (coinsTextPrefab != null)
            // {
            //     PopupText(startPosition, rewardDataCount);
            // }
            if (targetTransform)
            {
                for (var i = 0; i < animateCount; i++)
                {
                    var item = GetAnimatedObjectSource(sourceObject);
                    var random = .5f;
                    item.transform.position = startPosition + new Vector3(Random.Range(-random, random), Random.Range(-random, random));
                    var randomStartDelay = i==0? 0 : Random.Range(0f, 0.5f);
                    StartAnim(item.transform, targetPosition, randomStartDelay, () =>
                    {
                        animatedObjectSource.ReleaseObject(item);
                        if (doPunchScale == null || !doPunchScale.IsPlaying())
                        {
                            var punchScale = transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
                            punchScale.OnComplete(() => { doPunchScale = null; });
                            doPunchScale = punchScale;
                        }

                        if (fxPrefab != null)
                        {
                            var fx = PoolObject.GetObject(fxPrefab, targetPosition);
                            fx.transform.localScale = Vector3.one;
                            fx.transform.position = targetPosition;
                            DOVirtual.DelayedCall(1f, () => { PoolObject.Return(fx); });
                        }

                        if (count == 0)
                        {
                            _audioService.PlaySound(sound);
                        }

                        count++;
                        if (count == animateCount)
                        {
                            transform.localScale = Vector3.one;
                            callback?.Invoke();
                            DOVirtual.DelayedCall(.5f, () => { DOTween.Kill(gameObject); });
                        }

                        PoolObject.Return(item);
                    });
                }
            }
        }

        private void StartAnim(Transform targetTransform, Vector3 targetPos, float randomStartDelay, Action callback = null)
        {
            var sequence = DOTween.Sequence();

            targetTransform.localScale = Vector3.zero;
            var _scaleTween = targetTransform.DOScale(Vector3.one * 1f, .5f)
                .SetEase(Ease.OutBack)
                .SetDelay(randomStartDelay);

            sequence.Append(_scaleTween);
            var _rotationTween = targetTransform.DORotate(new Vector3(0, 0, Random.Range(0, 360)), .3f)
                .SetEase(Ease.Linear)
                .SetLoops(1, LoopType.Incremental);

            sequence.Join(_rotationTween);

            var _movementTween = targetTransform.DOMove(targetPos, .3f)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    callback?.Invoke();
                    targetTransform.gameObject.SetActive(false);
                });

            sequence.Join(_movementTween);
            sequence.Play();
        }
    }
}