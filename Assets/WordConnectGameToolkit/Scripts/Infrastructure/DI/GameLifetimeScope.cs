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

using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.ExtraWordBar;
using WordsToolkit.Scripts.GUI.Labels;
using WordsToolkit.Scripts.Infrastructure.Factories;
using WordsToolkit.Scripts.Localization;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.Services.IAP;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.Services.BannedWords;
using WordsToolkit.Scripts.Gameplay;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Infrastructure.Service;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Gameplay.WordValidator;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.GUI.Buttons.Boosts;
using WordsToolkit.Scripts.Debugger;

namespace WordsToolkit.Scripts.Infrastructure.DI
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private DebugSettings debugSettings;
        [SerializeField] private SpinSettings spinSettings;
        [SerializeField] private AdsSettings adsSettings;
        [SerializeField] private DailyBonusSettings dailyBonusSettings;
        [SerializeField] private BannedWordsConfiguration bannedWordsConfiguration;
        [SerializeField] private LanguageConfiguration languageConfiguration;
        [SerializeField] private TutorialSettings tutorialSettings;
        [SerializeField] private GiftsSettings giftSettings;
        protected override void Configure(IContainerBuilder builder)
        {
            if (!Application.isPlaying && !SceneManager.GetActiveScene().isLoaded)
            {
                return;
            }

            builder.RegisterInstance(gameSettings);
            builder.RegisterInstance(debugSettings);
            builder.RegisterInstance(spinSettings);
            builder.RegisterInstance(adsSettings);
            builder.RegisterInstance(dailyBonusSettings);
            builder.RegisterInstance(bannedWordsConfiguration);
            builder.RegisterInstance(languageConfiguration);
            builder.RegisterInstance(tutorialSettings);
            builder.RegisterInstance(giftSettings);

            // Register EntryPoints - LanguageService must initialize before GameManager
            builder.RegisterEntryPoint<LanguageService>().As<ILanguageService>();
            builder.RegisterEntryPoint<GameManager>().AsSelf();
            builder.RegisterEntryPoint<TutorialManager>().AsSelf();

            // Register interfaces and their implementations
            builder.Register<IBannedWordsService, BannedWordsService>(Lifetime.Singleton);
            builder.Register<IExtraWordService, PlayerPrefsExtraWordService>(Lifetime.Singleton);
            builder.Register<IPopupFactory, DefaultPopupFactory>(Lifetime.Singleton);
            builder.Register<ICoinsFactory, CoinsFactory>(Lifetime.Singleton);
            builder.Register<IModelController, ModelController>(Lifetime.Singleton);
            builder.Register<ICustomWordRepository, CustomWordRepository>(Lifetime.Singleton);
            builder.Register<IInitializeGamingServices, InitializeGamingServices>(Lifetime.Singleton);
            builder.Register<IIAPManager, IAPManager>(Lifetime.Singleton);
            builder.Register<IIAPService, IAPController>(Lifetime.Singleton);
            builder.Register<ILevelLoaderService, LevelLoaderService>(Lifetime.Singleton);
            builder.Register<IWordValidator, DefaultWordValidator>(Lifetime.Singleton);
            builder.Register<ButtonViewController>(Lifetime.Singleton);

            // Register audio service
            builder.RegisterComponentInHierarchy<SoundBase>()
                .As<IAudioService>();

            // Register ads manager
            builder.RegisterComponentInHierarchy<AdsManager>()
                .As<IAdsManager>();

            // Register other components
            builder.RegisterComponentInHierarchy<StateManager>();
            builder.RegisterComponentInHierarchy<ResourceManager>();
            builder.RegisterComponentInHierarchy<GiftButton>();
            builder.RegisterComponentInHierarchy<UIManager>();
            builder.RegisterComponentInHierarchy<LanguageSelectionButton>();
            builder.RegisterComponentInHierarchy<TimerDisplay>();
            builder.RegisterComponentInHierarchy<LevelManager>();
            builder.RegisterComponentInHierarchy<FieldManager>();
            builder.RegisterComponentInHierarchy<SceneLoader>();
            builder.RegisterComponentInHierarchy<MenuManager>();
            builder.RegisterComponentInHierarchy<BaseBoostButton>();
            builder.RegisterComponentInHierarchy<Popup>();
            builder.RegisterComponentInHierarchy<BaseExtraWordsProgressBar>();
            builder.RegisterComponentInHierarchy<CustomButton>();
            builder.RegisterComponentInHierarchy<WordSelectionManager>();
            builder.RegisterComponentInHierarchy<LabelAnim>();
            builder.RegisterComponentInHierarchy<BackgroundChanger>();
            builder.RegisterComponentInHierarchy<CrosswordWordsDebug>();

            // Register localization components
            builder.RegisterComponentInHierarchy<LocalizationManager>()
                   .As<ILocalizationService>();
            builder.RegisterComponentInHierarchy<LocalizedTextMeshProUGUI>();
        }
    }
}