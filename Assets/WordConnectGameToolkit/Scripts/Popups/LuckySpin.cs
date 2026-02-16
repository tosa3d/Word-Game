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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.GUI.Labels;
using WordsToolkit.Scripts.Popups.Reward;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.System;
using Random = UnityEngine.Random;

namespace WordsToolkit.Scripts.Popups
{
    public class LuckySpin : PopupWithCurrencyLabel
    {
        public float velocity;
        public float stoptime;

        [SerializeField]
        private GameObject spin;

        [SerializeField]
        private List<Image> lights = new();

        public CustomButton freeSpinButton;
        public CustomButton rewardedAdButton;
        public RewardSettingSpin[] spinRewards;
        public List<RewardVisual> rewards = new();
        private SpinSettings spinSettings;
        private Rigidbody2D rb;
        private bool isSpinning;
        private int previousRotationMarker;
        private const string LastFreeSpinTimeKey = "LastFreeSpinTime";

        [SerializeField]
        private float minVelocityMultiplier = 0.5f;

        [SerializeField]
        private float maxVelocityMultiplier = 2.5f;

        [SerializeField]
        private float additionalRandomFactor = 0.2f;
        [Inject]
        private SpinSettings luckySpinSettings;

        [SerializeField]
        private AudioClip luckySpin;

        [SerializeField]
        private AudioClip applause;

        private void OnEnable()
        {
            rb = spin.GetComponent<Rigidbody2D>();
            freeSpinButton.onClick.AddListener(FreeSpin);

            UpdateButtonVisibility();

            spinSettings = luckySpinSettings;
            DefineRewards(spinSettings.rewards);
            StartCoroutine(SwitchLightsAlpha());
        }

        private void UpdateButtonVisibility()
        {
            var canFreeSpin = CanUseFreeSpinToday();
            freeSpinButton.gameObject.SetActive(canFreeSpin);
            rewardedAdButton.gameObject.SetActive(!canFreeSpin);
        }

        private void SetButtonsVisibility(bool visible)
        {
            freeSpinButton.gameObject.SetActive(visible);
            rewardedAdButton.gameObject.SetActive(visible);
        }

        private bool CanUseFreeSpinToday()
        {
            if (!PlayerPrefs.HasKey(LastFreeSpinTimeKey))
            {
                return true;
            }

            var lastFreeSpinTimeStr = PlayerPrefs.GetString(LastFreeSpinTimeKey);
            var lastFreeSpinTime = DateTime.Parse(lastFreeSpinTimeStr);
            return DateTime.Now.Date > lastFreeSpinTime.Date;
        }

        private void FreeSpin()
        {
            PlayerPrefs.SetString(LastFreeSpinTimeKey, DateTime.Now.ToString("o"));
            Spin();
        }

        private IEnumerator SwitchLightsAlpha()
        {
            const float maxSpeed = 100;

            while (true)
            {
                var speedRatio = Mathf.Abs(rb.angularVelocity) / maxSpeed; // Ratio of the current speed to the maximum speed
                speedRatio = Mathf.Min(speedRatio, .9f);
                var delay = 1f - speedRatio; // Higher speed -> smaller delay
                yield return new WaitForSeconds(delay);

                foreach (var light in lights)
                {
                    light.color = new Color(light.color.r, light.color.g, light.color.b, light.color.a == 0 ? 1 : 0);
                }
            }
        }

        public void DefineRewards(RewardSettingSpin[] spinRewards)
        {
            this.spinRewards = spinRewards;
            foreach (var reward in spinRewards)
            {
                var obj = Instantiate(reward.rewardVisualPrefab, spin.transform);
                //rotate to 360/number of rewards
                obj.transform.localPosition += new Vector3(0, 20, 0);
                obj.transform.RotateAround(spin.transform.position, Vector3.forward, 360f / spinRewards.Length * obj.transform.GetSiblingIndex());
                obj.SetCount(reward.count);
                rewards.Add(obj);
            }
        }

        public void Spin()
        {
            StartCoroutine(StartSpin());
        }

        private IEnumerator StartSpin()
        {
            // buttons interaction
            closeButton.interactable = false;
            freeSpinButton.interactable = false;
            rewardedAdButton.interactable = false;
            //hide buttons
            SetButtonsVisibility(false);

            var randomVelocity = CalculateRandomVelocity();

            float timeElapsed = 0;
            isSpinning = true;
            previousRotationMarker = Mathf.FloorToInt(spin.transform.eulerAngles.z / 25);

            while (timeElapsed < stoptime)
            {
                var appliedTorque = Mathf.Lerp(0, randomVelocity, timeElapsed / stoptime);
                rb.AddTorque(appliedTorque);
                timeElapsed += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }

            rb.angularDrag *= 100;
            yield return new WaitWhile(() => rb.angularVelocity != 0);
            isSpinning = false;
            CheckReward(GetWinReward());
        }

        private float CalculateRandomVelocity()
        {
            var baseMultiplier = Random.Range(minVelocityMultiplier, maxVelocityMultiplier);
            var additionalRandomness = Random.Range(-additionalRandomFactor, additionalRandomFactor);
            return velocity * (baseMultiplier + additionalRandomness);
        }

        private void Update()
        {
            if (isSpinning)
            {
                CheckPlaySound();
            }
        }

        private void CheckPlaySound()
        {
            var currentZRotation = spin.transform.eulerAngles.z;
            var currentTenDegreeMarker = Mathf.FloorToInt(currentZRotation / 25);

            if (currentTenDegreeMarker != previousRotationMarker)
            {
                audioService.PlaySound(luckySpin);
                previousRotationMarker = currentTenDegreeMarker;
            }
        }

        private int GetWinReward()
        {
            audioService.PlaySound( applause);
            var highestYIndex = 0; // Start with first item's index
            var highestY = rewards[0].transform.position.y; // and its 'y' position

            for (var i = 1; i < rewards.Count; i++)
            {
                // If current item's 'y' position is higher
                if (rewards[i].transform.position.y > highestY)
                {
                    highestY = rewards[i].transform.position.y;
                    highestYIndex = i;
                }
            }

            return highestYIndex;
        }

        private void CheckReward(int rewardIndex)
        {
            var rewardSettingSpin = spinRewards[rewardIndex];
            var rewardVisual = rewards[rewardIndex];
            var _resource = rewardSettingSpin.resource;
            var iconPos = rewardVisual.transform;
            var _count = rewardSettingSpin.count;
            _resource.AddAnimated(_count, iconPos.position, animationSourceObject: null, callback: () =>
            {
                Close();
            });
        }
    }
}