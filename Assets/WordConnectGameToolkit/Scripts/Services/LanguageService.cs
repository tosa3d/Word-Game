using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.Localization;
using WordsToolkit.Scripts.System;

namespace WordsToolkit.Scripts.Services
{
    public interface ILanguageService
    {
        string GetCurrentLanguageCode();
        LanguageConfiguration.LanguageInfo GetCurrentLanguageInfo();
        List<LanguageConfiguration.LanguageInfo> GetEnabledLanguages();
        void SetLanguage(string languageCode);
        bool HasMultipleEnabledLanguages();
        void InitializeDefaultLanguage();
    }

    public class LanguageService : ILanguageService, IInitializable
    {
        private const string LANGUAGE_PREF_KEY = "SelectedLanguage";
        
        private readonly IObjectResolver container;
        private LanguageConfiguration languageConfiguration;
        private string currentLanguageCode;

        public LanguageService(IObjectResolver container)
        {
            this.container = container;
        }

        public void Initialize()
        {
            languageConfiguration = container.Resolve<LanguageConfiguration>();
            InitializeDefaultLanguage();
        }

        public void InitializeDefaultLanguage()
        {
            currentLanguageCode = GetSavedOrDefaultLanguage();
            ApplyLanguage(currentLanguageCode);
        }

        public string GetCurrentLanguageCode()
        {
            if (string.IsNullOrEmpty(currentLanguageCode))
            {
                currentLanguageCode = GetSavedOrDefaultLanguage();
            }
            return currentLanguageCode;
        }

        public LanguageConfiguration.LanguageInfo GetCurrentLanguageInfo()
        {
            if (languageConfiguration == null) return null;
            var languageCode = GetCurrentLanguageCode();
            return languageConfiguration.GetLanguageInfo(languageCode);
        }

        public List<LanguageConfiguration.LanguageInfo> GetEnabledLanguages()
        {
            if (languageConfiguration == null) return new List<LanguageConfiguration.LanguageInfo>();
            return languageConfiguration.GetEnabledLanguages();
        }

        public void SetLanguage(string languageCode)
        {
            if (languageConfiguration == null) return;
            var languageInfo = languageConfiguration.GetLanguageInfo(languageCode);
            if (languageInfo != null && languageInfo.enabledByDefault)
            {
                currentLanguageCode = languageCode;
                PlayerPrefs.SetString(LANGUAGE_PREF_KEY, languageCode);
                PlayerPrefs.Save();
                
                ApplyLanguage(languageCode);
                EventManager.GetEvent<string>(EGameEvent.LanguageChanged).Invoke(languageCode);
            }
        }

        public bool HasMultipleEnabledLanguages()
        {
            return GetEnabledLanguages().Count > 1;
        }

        private string GetSavedOrDefaultLanguage()
        {
            if (languageConfiguration == null) 
            {
                Debug.LogWarning("LanguageService: LanguageConfiguration is null, using 'en'");
                return "en";
            }
            
            var enabledLanguages = languageConfiguration.GetEnabledLanguages();
            Debug.Log($"LanguageService: Found {enabledLanguages.Count} enabled languages");
            
            // If only one language is enabled, use it regardless of saved preference
            if (enabledLanguages.Count == 1)
            {
                Debug.Log($"LanguageService: Using single enabled language: {enabledLanguages[0].code}");
                return enabledLanguages[0].code;
            }
            
            // If multiple languages are enabled, use saved preference or default
            var defaultLang = languageConfiguration.defaultLanguage ?? "en";
            var selectedLang = PlayerPrefs.GetString(LANGUAGE_PREF_KEY, defaultLang);
            Debug.Log($"LanguageService: Using selected/default language: {selectedLang} (default: {defaultLang})");
            return selectedLang;
        }

        private void ApplyLanguage(string languageCode)
        {
            if (languageConfiguration == null) return;
            var languageInfo = languageConfiguration.GetLanguageInfo(languageCode);
            if (languageInfo?.localizationBase != null)
            {
                LocalizationManager.instance.LoadLanguageFromBase(languageInfo.localizationBase);
            }
        }
    }
}
