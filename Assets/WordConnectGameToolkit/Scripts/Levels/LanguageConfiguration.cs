using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;
using ModelAsset = Unity.InferenceEngine.ModelAsset;

namespace WordsToolkit.Scripts.Levels
{
    [CreateAssetMenu(fileName = "LanguageConfiguration", menuName ="WordConnectGameToolkit/Editor/Language Configuration")]
    public class LanguageConfiguration : ScriptableObject
    {
        [global::System.Serializable]
        public class LanguageInfo
        {
            [Tooltip("Language code (e.g., 'en', 'fr', 'es')")]
            public string code;
            
            [Tooltip("Display name of the language")]
            public string displayName;
            [Tooltip("Localized name of the language")]
            public string localizedName;
            [Tooltip("Language model asset reference (optional)")]
            public ModelAsset languageModel;
            
            [Tooltip("Is this language enabled by default?")]
            public bool enabledByDefault = true;

            [Tooltip("Localization base")]
            public TextAsset localizationBase;
        }
        
        [Tooltip("List of all available languages")]
        public List<LanguageInfo> languages = new List<LanguageInfo>();
        
        [Tooltip("Default language code to use if not specified")]
        public string defaultLanguage = "en";
        
        // Get language information by code
        public LanguageInfo GetLanguageInfo(string code)
        {
            if (languages == null || languages.Count == 0)
                return null;
                
            return languages.Find(l => l.code == code);
        }
        
        // Get all enabled languages
        public List<LanguageInfo> GetEnabledLanguages()
        {
            if (languages == null || languages.Count == 0)
                return new List<LanguageInfo>();
                
            return languages.FindAll(l => l.enabledByDefault);
        }
    }
}
