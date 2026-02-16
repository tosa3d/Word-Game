using System.Collections.Generic;
using UnityEngine;
using System;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.Levels
{
    [Serializable]
    public class LocalizedTextGroup
    {
        public string language;
        public string title;
        public string text;
    }

    [CreateAssetMenu(fileName = "NewLevelGroup", menuName ="WordConnectGameToolkit/Editor/Level Group")]
    public class LevelGroup : ScriptableObject
    {
        [Tooltip("Name of this group")]
        public string groupName;

        [Tooltip("Parent group (if any)")]
        [HideInInspector]
        public LevelGroup parentGroup;

        [Tooltip("Levels in this group")]
        public List<Level> levels = new List<Level>();

        [Tooltip("Background sprite for this group")]
        public Sprite background;

        [Tooltip("Language-specific text for this group")]
        public List<LocalizedTextGroup> localizedTexts = new List<LocalizedTextGroup>();

        [Tooltip("Colors tile for this group")]
        public ColorsTile colorsTile;

        [Tooltip("Target number of extra words for levels in this group")]
        public int targetExtraWords = 8;

        // Apply the group's colorsTile to all levels in this group
        public void ApplyColorsTileToLevels()
        {
            if (colorsTile == null || levels == null || levels.Count == 0)
                return;

            foreach (var level in levels)
            {
                if (level != null)
                {
                    level.colorsTile = this.colorsTile;
                }
            }
        }

        public string GetTitle(string languageCode)
        {
            var localizedText = GetGroupTextObject(languageCode);
            return localizedText != null ? localizedText.title : string.Empty;
        }

        public string GetText(string languageCode)
        {
            var localizedText = GetGroupTextObject(languageCode);
            return localizedText != null ? localizedText.text : string.Empty;
        }

        private LocalizedTextGroup GetGroupTextObject(string languageCode)
        {
            foreach (var localizedText in localizedTexts)
            {
                if (localizedText.language.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                {
                    return localizedText;
                }
            }
            return null;
        }
        public void AddLanguage(string configLanguageCode)
        {
            localizedTexts.Add(new LocalizedTextGroup
            {
                language = configLanguageCode,
            });
        }
    }
}
