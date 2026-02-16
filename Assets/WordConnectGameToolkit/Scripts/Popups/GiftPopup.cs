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
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using VContainer;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Popups.RewardsGift;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Popups
{
    public class GiftPopup : Popup
    {
        [SerializeField] private CustomButton claimButton;
        [SerializeField] private Animator boxAnimator;
        [Inject] private GiftsSettings giftsSettings;
        private List<Transform> giftsPositionsSelected;
        [SerializeField] private Transform startTransform;
        [SerializeField] private Transform targetPosition;
        private bool opened;
        private List<GiftDataObject> giftsToClaim;
        [SerializeField]
        private AudioClip openBoxSound;

        [SerializeField]
        private Transform transformParent;

        private void OnEnable()
        {
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }

        private void OnDestroy()
        {
            claimButton.onClick.RemoveListener(OnClaimButtonClicked);
        }

        private void OnClaimButtonClicked()
        {
            if (!opened)
            {
                audioService.PlayDelayed(openBoxSound, 1.0f);
                boxAnimator.Play($"OpenBox");
            }
            else
                StartCoroutine(ClaimGifts());
        }

        private IEnumerator ClaimGifts()
        {
            for (var index = 0; index < giftsToClaim.Count; index++)
            {
                var giftData = giftsToClaim[index];
                giftData.giftInstance.Animate();
                var giftInstanceGameObject = giftData.giftData.resourceObject.name != "Coins" ? giftData.giftInstance.gameObject : null;
                giftData.giftData.resourceObject.AddAnimated(giftData.giftData.giftCount, giftData.giftInstance.transform.position, giftInstanceGameObject, () => { CloseDelay(); });
                if (index > 0 && index < giftsToClaim.Count - 1)
                {
                    yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
                }
            }
        }

        public void OnBoxOpened()
        {
            giftsToClaim = new List<GiftDataObject>();

            foreach(var giftData in giftsSettings.gifts)
            {
                var firstOrDefault = gameSettings.boostLevels.FirstOrDefault(i=> i.tag == giftData.tagUIElement);
                if(firstOrDefault != null && firstOrDefault!.level > GameDataManager.GetLevelNum())
                {
                    break;
                }
                if(Random.value < giftData.giftChance/100f)
                {
                    giftsToClaim.Add(new GiftDataObject
                    {
                        giftData = giftData,
                        giftInstance = null // Will be instantiated later
                    });
                }
            }

            // If no gifts were added, force add the first gift
            if(giftsToClaim.Count == 0 && giftsSettings.gifts.Length > 0)
            {
                giftsToClaim.Add( new GiftDataObject
                {
                    giftData = giftsSettings.gifts[Random.Range(0, giftsSettings.gifts.Length)],
                    giftInstance = null // Will be instantiated later
                });
            }

            for (var i = 0; i < giftsToClaim.Count; i++)
            {
                var giftData = giftsToClaim[i].giftData;
                var giftPrefab = giftData.giftPrefab;
                var giftInstance = Instantiate(giftPrefab, transformParent);
                giftsToClaim[i].giftInstance = giftInstance;
                giftInstance.transform.position = startTransform.position;
                AnimateGift(giftInstance, i, giftsToClaim.Count, giftData);
                giftInstance.SetCount(giftData.giftCount);
            }
        }

        private void AnimateGift(GiftBase giftInstance, int i, int giftsCount, GiftData giftData)
        {
            giftInstance.transform.DOMove(GetGiftPosition(i, giftsCount), 0.5f).OnComplete(()=>OnCompleteAnimation(giftData));
        }

        private void OnCompleteAnimation(GiftData giftData)
        {
            opened = true;
            var list = ShowTags(new[] { giftData.tagUIElement });
            foreach (var tagObject in list)
            {
                var showable = tagObject.GetComponent<IFadeable>();
                showable?.InstantHide();
                showable?.Show();
                var customButton = tagObject.GetComponent<CustomButton>();
                if (customButton)
                {
                    customButton.interactable = false;
                }
            }
        }

        private Vector3 GetGiftPosition(int index, int totalCount)
        {
            // check even or odd total count
            if(totalCount % 2 == 0)
            {
                float radius = Vector3.Distance(targetPosition.position, startTransform.position)/1.2f;
                float sign = (index == 1) ? 1f : -1f;
                float angleInRadians = sign * 30f * Mathf.Deg2Rad;
                float x = startTransform.position.x - (radius * Mathf.Sin(angleInRadians));
                float y = startTransform.position.y + (radius * Mathf.Cos(angleInRadians));
                return new Vector3(x, y, targetPosition.position.z);
            }

            // odd count
            if (index == 0)
            {
                return targetPosition.position;
            }
            else if (index == 1 || index == 2)
            {
                float radius = Vector3.Distance(targetPosition.position, startTransform.position)/1.2f;
                float sign = (index == 1) ? 1f : -1f;
                float angleInRadians = sign * 50f * Mathf.Deg2Rad;
                float x = startTransform.position.x - (radius * Mathf.Sin(angleInRadians));
                float y = startTransform.position.y + (radius * Mathf.Cos(angleInRadians));
                return new Vector3(x, y, targetPosition.position.z);
            }
            return targetPosition.position; // fallback
        }
    }

    class GiftDataObject
    {
        public GiftBase giftInstance;
        public GiftData giftData;
    }
}

