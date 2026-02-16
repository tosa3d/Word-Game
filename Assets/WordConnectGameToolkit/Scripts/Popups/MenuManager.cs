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
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using WordsToolkit.Scripts.System;
using VContainer;

namespace WordsToolkit.Scripts.Popups
{
    public class MenuManager : MonoBehaviour
    {
        public Fader fader;
        private List<Popup> popupStack = new();

        [SerializeField]
        private Canvas canvas;
        
        [Inject]
        private IPopupFactory popupFactory;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void OnEnable()
        {
            Popup.OnClosePopup += ClosePopup;
            Popup.OnBeforeCloseAction += OnBeforeCloseAction;
            SceneManager.activeSceneChanged += OnSceneLoaded;
        }

        private void OnBeforeCloseAction(Popup popup)
        {
            if (fader != null && popupStack.Count == 1)
            {
                fader.FadeOut();
            }
        }

        private void OnSceneLoaded(Scene scene, Scene scene1)
        {
            if (canvas == null && this != null)
            {
                canvas = GetComponent<Canvas>();
            }

            canvas.worldCamera = Camera.main;
        }

        private void OnDisable()
        {
            Popup.OnClosePopup -= ClosePopup;
            SceneManager.activeSceneChanged -= OnSceneLoaded;
            Popup.OnBeforeCloseAction -= OnBeforeCloseAction;
        }

        public T ShowPopup<T>(Action onShow = null, Action<EPopupResult> onClose = null) where T : Popup
        {
            if (popupStack.OfType<T>().Any())
            {
                return popupStack.OfType<T>().First();
            }

            var popup = popupFactory.CreatePopup<T>(transform);
            return (T)ShowPopupInternal(popup, onShow, onClose);
        }

        public Popup ShowPopup(string pathWithType, Action onShow = null, Action<EPopupResult> onClose = null)
        {
            if (popupStack.Any(p => p.GetType().Name == pathWithType.Split('/').Last()))
            {
                return popupStack.First(p => p.GetType().Name == pathWithType.Split('/').Last());
            }

            var popup = popupFactory.CreatePopup(pathWithType, transform);
            return ShowPopupInternal(popup, onShow, onClose);
        }

        public Popup ShowPopup(Popup popupPrefab, Action onShow = null, Action<EPopupResult> onClose = null)
        {
            if (popupStack.Any(p => p.GetType() == popupPrefab.GetType()))
            {
                return popupStack.First(p => p.GetType() == popupPrefab.GetType());
            }
            var popup = popupFactory.CreatePopup(popupPrefab, transform);
            return ShowPopupInternal(popup, onShow, onClose);
        }

        private Popup ShowPopupInternal(Popup popup, Action onShow = null, Action<EPopupResult> onClose = null)
        {
            if (popupStack.Count > 0)
            {
                popupStack.Last().Hide();
            }

            popupStack.Add(popup);
            popup.Show<Popup>(onShow, onClose);
            
            if (fader != null && popupStack.Count > 0 && popup.fade)
            {
                fader.transform.SetSiblingIndex(popup.transform.GetSiblingIndex() - 1);
                fader.FadeIn(popup.fadeAlpha);
            }

            return popup;
        }

        private void ClosePopup(Popup popupClose)
        {
            if (popupStack.Count > 0)
            {
                popupStack.Remove(popupClose);
                if (popupStack.Count > 0)
                {
                    var popup = popupStack.Last();
                    var siblingIndex = popup.transform.GetSiblingIndex() - 1;
                    siblingIndex = Mathf.Clamp(siblingIndex, 0, transform.childCount - 1);
                    fader.transform.SetSiblingIndex(siblingIndex);
                    popup.OnActivate();
                }
            }
        }

        public void ShowPurchased(GameObject imagePrefab, string boostName)
        {
            var menu = ShowPopup<PurchasedMenu>();
            menu.GetComponent<PurchasedMenu>().SetIconSprite(imagePrefab, boostName);
        }

        private void Update()
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                if (Keyboard.current[Key.Escape].wasReleasedThisFrame)
                {
                    if (popupStack is { Count: > 0 })
                    {
                        var closeButton = popupStack.Last().closeButton;
                        if (closeButton != null)
                        {
                            closeButton.onClick?.Invoke();
                        }
                    }
                }
            }
        }

        public T GetPopupOpened<T>() where T : Popup
        {
            foreach (var popup in popupStack)
            {
                if (popup.GetType() == typeof(T))
                {
                    return (T)popup;
                }
            }

            return null;
        }

        public void CloseAllPopups()
        {
            for (var i = 0; i < popupStack.Count; i++)
            {
                var popup = popupStack[i];
                popup.Close();
            }

            popupStack.Clear();
        }

        public bool IsAnyPopupOpened()
        {
            return popupStack.Count > 0;
        }

        public Popup GetLastPopup()
        {
            return popupStack.Last();
        }

        public MainMenu GetMainMenu()
        {
            return FindObjectOfType<MainMenu>();
        }
    }
}