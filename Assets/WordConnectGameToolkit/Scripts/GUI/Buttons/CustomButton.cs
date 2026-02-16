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

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Popups.Reward;
using WordsToolkit.Scripts.Services.Ads.AdUnits;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.System.Haptic;

namespace WordsToolkit.Scripts.GUI.Buttons
{
    [RequireComponent(typeof(Animator))]
    public class CustomButton : Button
    {
        public AudioClip overrideClickSound;
        public RuntimeAnimatorController overrideAnimatorController;
        private bool isClicked;
        private readonly float cooldownTime = .5f; // Cooldown time in seconds
        public new ButtonClickedEvent onClick;
        private new Animator animator;
        public bool noSound;
        public bool isRewarded;
        private RewardedButtonHandler handler;
        private PointerEventData currentEventData;

        private static bool blockInput;
        public AdReference adReference;
        protected virtual bool ShouldShowRewarded() => isRewarded;

        public static CustomButton latestClickedButton;
        private IAudioService audioService;
        private IObjectResolver objectResolver;
        [Inject]
        public void Construct(IAudioService audioService, IObjectResolver objectResolver)
        {
            this.audioService = audioService;
            this.objectResolver = objectResolver;
        }

        protected override void OnEnable()
        {
            isClicked = false;
            if (ShouldShowRewarded() && !GetComponent<RewardedButtonHandler>() && Application.isPlaying)
            {
                handler = gameObject.AddComponent<RewardedButtonHandler>();
                objectResolver.Inject(handler);
                handler.adReference = adReference;
                handler.onRewardedAdComplete = new UnityEngine.Events.UnityEvent();
                handler.onRewardedAdComplete.AddListener(ExecuteEvent);
            }
            if (Application.isEditor)
            {
                return;
            }

            base.OnEnable();
            animator = GetComponent<Animator>();
            if (overrideAnimatorController != null)
            {
                animator.runtimeAnimatorController = overrideAnimatorController;
            }

        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (blockInput || isClicked || !interactable)
            {
                return;
            }

            currentEventData = eventData;

            if (ShouldShowRewarded())
            {
                if (handler != null)
                {
                    handler.ShowRewardedAd();
                    return;
                }
            }

            if (transition != Transition.Animation)
            {
                Pressed();
            }

            isClicked = true;
            if(!noSound)
                audioService.PlayClick(overrideClickSound);
            HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticForce.Light);
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(Cooldown());
            }

            base.OnPointerClick(eventData);
        }

        public void Pressed()
        {
            if (blockInput || !interactable)
            {
                return;
            }
            latestClickedButton = this;
            if (ShouldShowRewarded())
            {
                if (handler != null)
                {
                    handler.ShowRewardedAd();
                    return;
                }
            }

            ExecuteEvent();
        }

        protected virtual void ExecuteEvent()
        {
            onClick?.Invoke();
            EventManager.GetEvent<CustomButton>(EGameEvent.ButtonClicked).Invoke(this);
            base.onClick?.Invoke();
        }

        private IEnumerator Cooldown()
        {
            yield return new WaitForSeconds(cooldownTime);
            isClicked = false;
        }


        private bool IsAnimationPlaying()
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.loop || stateInfo.normalizedTime < 1;
        }

        public static void BlockInput(bool block)
        {
            blockInput = block;
        }
    }
}