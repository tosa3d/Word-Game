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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using VContainer;
using VContainer.Unity;
#if UNITY_2023_1_OR_NEWER
using Awaitable = UnityEngine.Awaitable;
#else
using Awaitable = System.Threading.Tasks.Task;
#endif
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Popups;
using WordsToolkit.Scripts.Popups.Daily;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.Services.IAP;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.Utils;
using ResourceManager = WordsToolkit.Scripts.Data.ResourceManager;

namespace WordsToolkit.Scripts.System
{
    public class GameManager : IInitializable, IDisposable, IAsyncStartable
    {
        public Action<string> purchaseSucceded;

        private (string id, ProductTypeWrapper.ProductType productType)[] products;
        private int lastBackgroundIndex = -1;
        private bool isTutorialMode;
        private MainMenu mainMenu;
        public Action<bool, List<string>> OnPurchasesRestored;
        public ProductID noAdsProduct;
        public string language = "en";
        public int Score { get=> resourceManager.GetResource("Score").GetValue(); set => resourceManager.GetResource("Score").Set(value); }

        private readonly StateManager stateManager;
        private readonly SceneLoader sceneLoader;
        private readonly MenuManager menuManager;
        private readonly GameSettings gameSettings;
        private readonly DailyBonusSettings dailyBonusSettings;
        private readonly IIAPManager iapManager;
        private readonly IInitializeGamingServices gamingServices;
        private readonly ResourceManager resourceManager;
        private readonly ILanguageService languageService;

        public GameManager(
            StateManager stateManager,
            SceneLoader sceneLoader,
            MenuManager menuManager,
            GameSettings gameSettings,
            DailyBonusSettings dailyBonusSettings,
            IIAPManager iapManager,
            IInitializeGamingServices gamingServices,
            ResourceManager resourceManager,
            ILanguageService languageService)
        {
            this.stateManager = stateManager;
            this.sceneLoader = sceneLoader;
            this.menuManager = menuManager;
            this.gameSettings = gameSettings;
            this.dailyBonusSettings = dailyBonusSettings;
            this.iapManager = iapManager;
            this.gamingServices = gamingServices;
            this.resourceManager = resourceManager;
            this.languageService = languageService;
        }

        public void Initialize()
        {
            mainMenu = menuManager.GetMainMenu();
            if (mainMenu != null)
            {
                CustomButton.BlockInput(CheckDailyBonusConditions());
                mainMenu.OnAnimationEnded += OnMainMenuAnimationEnded;
            }
            else
            {
                CustomButton.BlockInput(false);
            }

            // Get current language from LanguageService (already initialized as EntryPoint)
            var langName = languageService.GetCurrentLanguageCode();
            language = langName;
            EventManager.GetEvent<string>(EGameEvent.LanguageChanged).Subscribe(LanguageChanged);
            iapManager.SubscribeToPurchaseEvent(PurchaseSucceeded);

            stateManager.OnStateChanged.AddListener((state) => {
                if (state != EScreenStates.MainMenu)
                        CustomButton.BlockInput(false);
            });

            if (!IsTutorialShown() && !GameDataManager.isTestPlay)
            {
                SetTutorialMode(true);
            }
        }

        private void LanguageChanged(string obj)
        {
            language = obj;
        }

        public void Dispose()
        {
            EventManager.GetEvent<string>(EGameEvent.LanguageChanged).Unsubscribe(LanguageChanged);
            iapManager.UnsubscribeFromPurchaseEvent(PurchaseSucceeded);
            if (mainMenu != null)
            {
                mainMenu.OnAnimationEnded -= OnMainMenuAnimationEnded;
            }
            if (GameDataManager.isTestPlay)
            {
                GameDataManager.CleanupAfterTest();
            }
        }

        private bool IsTutorialShown()
        {
            return PlayerPrefs.GetInt("tutorial", 0) == 1;
        }

        public void SetTutorialCompleted()
        {
            PlayerPrefs.SetInt("tutorial", 1);
            PlayerPrefs.Save();
        }

        async Awaitable IAsyncStartable.StartAsync(CancellationToken cancellation)
        {
            Application.targetFrameRate = 60;
            DOTween.SetTweensCapacity(1250, 512);

            if (gameSettings.enableInApps) {
                products = Resources.LoadAll<ProductID>("ProductIDs")
                    .Select(p => (p.ID, p.productType))
                    .ToArray();

                // Initialize gaming services
                await gamingServices.Initialize(
                    OnInitializeSuccess,
                    OnInitializeError
                );
                // Initialize IAP directly if InitializeGamingServices is not used
                await iapManager.InitializePurchasing(products);
            }


            if (GameDataManager.isTestPlay)
            {
                GameDataManager.SetLevel(GameDataManager.GetLevel());
            }
        }

        private void OnInitializeSuccess()
        {
            Debug.Log("Gaming services initialized successfully");
        }

        private void OnInitializeError(string errorMessage)
        {
            Debug.LogError($"Failed to initialize gaming services: {errorMessage}");
        }

        private void HandleDailyBonus()
        {
            if (stateManager.CurrentState != EScreenStates.MainMenu || !dailyBonusSettings.dailyBonusEnabled || !gameSettings.enableInApps || !CheckDailyBonusConditions())
            {
                CustomButton.BlockInput(false);
                return;
            }

            menuManager.ShowPopup<DailyBonus>();
            DOVirtual.DelayedCall(0.5f, () => { CustomButton.BlockInput(false); });
        }

        private bool CheckDailyBonusConditions()
        {
            var today = DateTime.Today;
            var lastRewardDate = DateTime.Parse(PlayerPrefs.GetString("DailyBonusDay", today.Subtract(TimeSpan.FromDays(1)).ToString(CultureInfo.CurrentCulture)));
            return today.Date > lastRewardDate.Date && dailyBonusSettings.dailyBonusEnabled;
        }

        public void RestartLevel()
        {
            DOTween.KillAll();
            menuManager.CloseAllPopups();
            EventManager.GetEvent(EGameEvent.RestartLevel).Invoke();
        }

        public void RemoveAds()
        {
            if (gameSettings.enableAds) {
                menuManager.ShowPopup<NoAds>();
            }
        }

        public void MainMenu()
        {
            DOTween.KillAll();
            sceneLoader.GoMain();
        }

        public void OpenGame()
        {
            sceneLoader.StartGameScene();
        }

        public void PurchaseSucceeded(string id)
        {
            EventManager.GetEvent<string>(EGameEvent.PurchaseSucceeded).Invoke(id);
        }

        public void SetGameMode(EGameMode gameMode)
        {
            GameDataManager.SetGameMode(gameMode);
        }

        private EGameMode GetGameMode()
        {
            return GameDataManager.GetGameMode();
        }

        public int GetLastBackgroundIndex()
        {
            return lastBackgroundIndex;
        }

        public void SetLastBackgroundIndex(int index)
        {
            lastBackgroundIndex = index;
        }

        public void NextLevel()
        {
            // Get current level and increment it
            int currentLevel = GameDataManager.GetLevelNum();
            GameDataManager.SetLevelNum(currentLevel + 1);
            OpenGame();
            RestartLevel();
        }

        public void SetTutorialMode(bool tutorial)
        {
            Debug.Log("Tutorial mode set to " + tutorial);
            isTutorialMode = tutorial;
        }

        public bool IsTutorialMode()
        {
            return isTutorialMode;
        }

        private void OnMainMenuAnimationEnded()
        {
            HandleDailyBonus();
        }

        internal void RestorePurchases(Action<bool, List<string>> OnPurchasesRestored)
        {
            if (!gameSettings.enableInApps) return;

            this.OnPurchasesRestored = OnPurchasesRestored;
            iapManager.RestorePurchases(OnPurchasesRestored);
        }

        public bool IsPurchased(string id)
        {
            if (!gameSettings.enableInApps) return false;
            return iapManager.IsProductPurchased(id);
        }
    }
}