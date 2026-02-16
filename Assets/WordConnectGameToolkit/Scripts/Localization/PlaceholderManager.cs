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

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Localization
{
    // This class is used to replace placeholders in localized strings with actual values.
    public static class PlaceholderManager
    {
        public static string GetPlaceholderValue(string placeholderKey, Dictionary<string, string> placeholdersDic)
        {
            // First check if the key exists in the provided dictionary
            if (placeholdersDic != null && placeholdersDic.TryGetValue(placeholderKey, out string value))
            {
                return value;
            }

            // If not found in dictionary, check default cases
            switch (placeholderKey)
            {
                case "level":
                    return GetCurrentLevel().ToString();
                default:
                    if (PlayerPrefs.HasKey(placeholderKey))
                    {
                        return PlayerPrefs.GetString(placeholderKey);
                    }
                    return "{" + placeholderKey + "}";
            }
        }

        public static string ReplacePlaceholders(string input, Dictionary<string, string> placeholdersDic)
        {
            return Regex.Replace(input, @"\{(\w+)\}", match =>
            {
                var placeholderKey = match.Groups[1].Value;
                return GetPlaceholderValue(placeholderKey, placeholdersDic);
            });
        }

        private static int GetCurrentLevel()
        {
            return GameDataManager.GetLevel().number;
        }
    }
}