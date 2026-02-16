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
using System.Linq;
using UnityEngine;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.Popups.Daily
{
    public class DailyBonus : PopupWithCurrencyLabel
    {
        [SerializeField]
        public DayHandle[] dayHandles;

        // Instance of the custom scriptable object that stores daily bonus settings
        private DailyBonusSettings settings;
        private int rewardStreak;

        public DayHandle[] daysPrefabs;

        public Transform dayHandlesParent;
        private int days;

        // This method is automatically called when the script becomes enabled
        public void OnEnable()
        {
            // Load daily bonus settings
            settings = LoadSettings("Settings/DailyBonusSettings");

            days = settings.rewards.Length;

            // Update the reward streak count and store it
            rewardStreak = UpdateRewardStreak();

            // Update each day handle based on the current reward streak
            UpdateDayHandles(rewardStreak);
        }

        // Loads and returns daily bonus settings stored at the specified path
        public DailyBonusSettings LoadSettings(string path)
        {
            return Resources.Load<DailyBonusSettings>(path);
        }

        // Checks the last reward date and the current date 
        // to determine and update the reward streak
        public int UpdateRewardStreak()
        {
            var today = DateTime.Today;
            var lastRewardDate = DateTime.Parse(PlayerPrefs.GetString("DailyBonusDay", today.Subtract(TimeSpan.FromDays(1)).ToString()));

            if (today > lastRewardDate)
            {
                var rewardStreak = GetRewardStreak() + 1;
                PlayerPrefs.SetString("DailyBonusDay", today.ToString());
                PlayerPrefs.SetInt("RewardStreak", rewardStreak = (int)Mathf.Repeat(rewardStreak, dayHandles.Length));
                return rewardStreak;
            }

            return GetRewardStreak();
        }

        // Updates the status of each day handle in the scene 
        // according to the current reward streak
        public void UpdateDayHandles(int rewardStreak)
        {
            dayHandles = new DayHandle[days];
            for (var i = 0; i < days; i++)
            {
                var status = i < rewardStreak ? EDailyStatus.passed : i == rewardStreak ? EDailyStatus.current : EDailyStatus.locked;
                var dayHandle = Instantiate(daysPrefabs[(int)status], dayHandlesParent);
                dayHandles[i] = dayHandle;
                dayHandle.name = $"Day_{i + 1}";
                dayHandle.SetDay(i + 1, settings.rewards[i]);

                // Set status after setting the day to ensure proper icon display
                dayHandle.SetStatus(status);
            }
        }

        // Gets and returns the reward streak count from player preferences 
        public int GetRewardStreak()
        {
            return PlayerPrefs.GetInt("RewardStreak", -1);
        }

        public override void Close()
        {
            StopInteration();
            closeButton.interactable = false;
            var resource = dayHandles[rewardStreak].RewardData.resource;

            var dayHandle = dayHandles.First(i => i.DailyStatus == EDailyStatus.current);
            resource.AddAnimated(dayHandles[rewardStreak].RewardData.count, dayHandle.transform.position, animationSourceObject: null, callback: () =>
            {
                base.Close();
            });

        }

        #if UNITY_EDITOR
        [ContextMenu("Test - Reset Daily Bonus")]
        private void TestResetDailyBonus()
        {
            CleanupDayHandles();
            PlayerPrefs.DeleteKey("DailyBonusDay");
            PlayerPrefs.DeleteKey("RewardStreak");
            Debug.Log("Daily bonus data reset!");
            if (Application.isPlaying)
            {
                OnEnable();
            }
        }

        [ContextMenu("Test - Set Day 1")]
        private void TestSetDay1()
        {
            SetTestDay(0);
        }

        [ContextMenu("Test - Set Day 2")]
        private void TestSetDay2()
        {
            SetTestDay(1);
        }

        [ContextMenu("Test - Set Day 3")]
        private void TestSetDay3()
        {
            SetTestDay(2);
        }

        [ContextMenu("Test - Set Day 4")]
        private void TestSetDay4()
        {
            SetTestDay(3);
        }

        [ContextMenu("Test - Set Day 5")]
        private void TestSetDay5()
        {
            SetTestDay(4);
        }

        [ContextMenu("Test - Set Day 6")]
        private void TestSetDay6()
        {
            SetTestDay(5);
        }

        [ContextMenu("Test - Set Day 7")]
        private void TestSetDay7()
        {
            SetTestDay(6);
        }

        private void SetTestDay(int day)
        {
            if (settings == null)
            {
                settings = LoadSettings("Settings/DailyBonusSettings");
            }
            
            if (settings != null && day < settings.rewards.Length)
            {
                // Clean up existing day handles before setting new ones
                CleanupDayHandles();
                
                PlayerPrefs.SetInt("RewardStreak", day);
                PlayerPrefs.SetString("DailyBonusDay", DateTime.Today.ToString());
                Debug.Log($"Set daily bonus to day {day + 1}");
                
                if (Application.isPlaying)
                {
                    OnEnable();
                }
            }
            else
            {
                Debug.LogWarning($"Cannot set day {day + 1}. Settings not found or day out of range.");
            }
        }

        private void CleanupDayHandles()
        {
            if (dayHandles != null)
            {
                for (int i = 0; i < dayHandles.Length; i++)
                {
                    if (dayHandles[i] != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(dayHandles[i].gameObject);
                        }
                        else
                        {
                            DestroyImmediate(dayHandles[i].gameObject);
                        }
                    }
                }
                dayHandles = null;
            }
        }
        #endif
    }
}