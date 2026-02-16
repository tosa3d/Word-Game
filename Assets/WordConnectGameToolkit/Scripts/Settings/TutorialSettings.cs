using System;
using UnityEngine;
using UnityEngine.Serialization;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Tutorials;

namespace WordsToolkit.Scripts.Settings
{
    public class TutorialSettings : ScriptableObject
    {
        public TutorialSettingsData[] tutorialSettings;
    }

    [Serializable]
    public class TutorialSettingsData
    {
        public TutorialKind kind;
        public TutorialShowCondition showCondition;
        public TutorialPopupBase popup;
        [TagFieldUI]
        public string[] tagsToShow;

        public string GetID()
        {
            return $"_{string.Join("_", tagsToShow)}";
        }
    }

    [Serializable]
    public class TutorialShowCondition
    {
        public ETutorialShowCondition showCondition;
        public int level;
    }

    public enum TutorialKind
    {
        TipBoosterButton,
        HammerBoosterButton,
        ExtraWordsButton,
        GiftButton,
        ShuffleButton,
        RedGem,
        GameTutorial,
        TimeTutorial
    }

    public enum ETutorialShowCondition
    {
        Level,
        Event,
        FirstAppearance,
    }
}