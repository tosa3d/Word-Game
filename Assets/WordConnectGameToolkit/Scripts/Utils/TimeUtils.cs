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

namespace WordsToolkit.Scripts.Utils
{
    public static class TimeUtils
    {
        /// <summary>
        /// Formats time in seconds to a string representation (MM:SS)
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds</param>
        /// <returns>Formatted time string</returns>
        public static string GetTimeString(float timeInSeconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timeInSeconds);
            return string.Format("{0:00}:{1:00}", 
                (int)timeSpan.TotalMinutes, 
                timeSpan.Seconds);
        }
        
        /// <summary>
        /// Formats time in seconds to a string representation with hours (HH:MM:SS)
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds</param>
        /// <returns>Formatted time string with hours</returns>
        public static string GetTimeStringWithHours(float timeInSeconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timeInSeconds);
            return string.Format("{0:00}:{1:00}:{2:00}", 
                (int)timeSpan.TotalHours, 
                timeSpan.Minutes, 
                timeSpan.Seconds);
        }

        public static string GetTimeString(float time, float activeTimeLimit, bool descendant = true)
        {
            var adjustedTime = descendant ? activeTimeLimit - time % activeTimeLimit : time % activeTimeLimit;
            return GetTimeString(adjustedTime);
        }

        public static float GetTimeInSeconds(string timeString)
        {
            var time = timeString.Split(':');
            if (time.Length == 3)
            {
                return GetTimeInSeconds(int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2]));
            }
            else if (time.Length == 2)
            {
                return GetTimeInSeconds(0, int.Parse(time[0]), int.Parse(time[1]));
            }
            return 0;
        }

        public static float GetTimeInSeconds(int hours, int minutes, int seconds)
        {
            return hours * 3600 + minutes * 60 + seconds;
        }
    }
}