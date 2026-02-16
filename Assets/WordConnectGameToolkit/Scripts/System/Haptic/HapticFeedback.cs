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
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Android;

namespace WordsToolkit.Scripts.System.Haptic
{
    public class HapticFeedback : MonoBehaviour
    {
        public enum HapticForce
        {
            Light,
            Medium,
            Heavy
        }

        private const string VibrationPrefKey = "VibrationLevel";

        #if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void _TriggerHapticFeedback(int force);
        #endif

        private static bool IsSystemSupported()
        {
            #if UNITY_IOS
            return SystemInfo.supportsVibration;
            #elif UNITY_ANDROID
            return SystemInfo.supportsVibration && Permission.HasUserAuthorizedPermission("android.permission.VIBRATE");
            #else
            return false;
            #endif
        }

        private static bool TryHapticFeedback(HapticForce force)
        {
            if (!IsSystemSupported())
                return false;

            try
            {
                #if UNITY_IOS
                _TriggerHapticFeedback((int)force);
                #elif UNITY_ANDROID
                long[] pattern = force switch
                {
                    HapticForce.Light => new long[] { 0, 50 },
                    HapticForce.Medium => new long[] { 0, 100 },
                    HapticForce.Heavy => new long[] { 0, 200 },
                    _ => new long[] { 0, 50 }
                };
                Vibration.Vibrate(pattern, -1);
                #endif
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void TriggerHapticFeedback(HapticForce force)
        {
            if (!IsVibrationEnabled())
                return;

        }

        private static bool IsVibrationEnabled()
        {
            return PlayerPrefs.GetFloat(VibrationPrefKey, 1) > 0;
        }
    }
}