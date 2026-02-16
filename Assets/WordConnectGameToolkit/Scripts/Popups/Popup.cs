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
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using WordsToolkit.Scripts.Audio;
using WordsToolkit.Scripts.Data;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.GUI;
using WordsToolkit.Scripts.GUI.Buttons;
using WordsToolkit.Scripts.Localization;
using WordsToolkit.Scripts.NLP;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.Services.IAP;
using WordsToolkit.Scripts.Settings;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.Utils;

namespace WordsToolkit.Scripts.Popups
{
    [RequireComponent(typeof(Animator), typeof(CanvasGroup))]
    public class Popup : MonoBehaviour
    {
        public bool fade = true;
        private Animator animator;
        public CustomButton closeButton;
        private CanvasGroup canvasGroup;
        public Action OnShowAction;
        public Action<EPopupResult> OnCloseAction;
        protected EPopupResult result;
        protected GameManager gameManager;
        protected MenuManager menuManager;
        protected LevelManager levelManager;
        protected StateManager stateManager;
        protected GameSettings gameSettings;
        protected IAdsManager adsManager;
        protected IIAPManager iapManager;
        protected IAudioService audioService;
        protected ResourceManager resourceManager;
        public delegate void PopupEvents(Popup popup);

        public static event PopupEvents OnOpenPopup;
        public static event PopupEvents OnClosePopup;
        public static event PopupEvents OnBeforeCloseAction;
        protected IObjectResolver _container;

        [TagFieldUI, SerializeField]
        public string[] tagsToShow;
        private Dictionary<Transform, Transform> _tagsToShowDic = new Dictionary<Transform, Transform>();
        [SerializeField]
        private bool isPopupAboveTags;

        protected ILocalizationService localizationManager;
        protected FieldManager fieldManager;
        protected ICustomWordRepository customWordRepository;
        [SerializeField]
        public float fadeAlpha = 1;

        [SerializeField]
        private AudioClip appearSound;

        [SerializeField]
        private AudioClip disappearSound;


        [Inject]
        public void Construct(GameManager gameManager, MenuManager menuManager, LevelManager levelManager, StateManager stateManager, GameSettings gameSettings, IAdsManager adsManager, IIAPManager iapManager, IAudioService audioService, ResourceManager resourceManager, IObjectResolver container, ILocalizationService localizationManager, FieldManager fieldManager, ICustomWordRepository customWordRepository)
        {
            this.gameManager = gameManager;
            this.menuManager = menuManager;
            this.levelManager = levelManager;
            this.stateManager = stateManager;
            this.gameSettings = gameSettings;
            this.adsManager = adsManager;
            this.iapManager = iapManager;
            this.audioService = audioService;
            this.resourceManager = resourceManager;
            _container = container;
            this.localizationManager = localizationManager;
            this.fieldManager = fieldManager;
            this.customWordRepository = customWordRepository;
        }

        protected virtual void Awake()
        {
            animator = GetComponent<Animator>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        public void Show<T>(Action onShow = null, Action<EPopupResult> onClose = null)
        {
            if (onShow != null)
            {
                OnShowAction = onShow;
            }

            if (onClose != null)
            {
                OnCloseAction = onClose;
            }

            OnOpenPopup?.Invoke(this);
            PlayShowAnimation();
            OnActivate();
        }

        public List<GameObject> ShowTags(string[] tutorialDataTagsToShow)
        {
            List<GameObject> lObjects = new List<GameObject>();
            tagsToShow = tutorialDataTagsToShow;
            foreach (var t in tutorialDataTagsToShow)
            {
                var tagObject = GameObject.FindGameObjectsWithTag(t).FirstOrDefault(i=>i.gameObject.activeSelf);
                if (tagObject)
                {
                    lObjects.Add(tagObject);
                    MakeObjectVisible(tagObject);
                }
            }

            if (isPopupAboveTags)
            {
                var canvas = transform.AddComponentIfNotExists<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingLayerID = SortingLayer.NameToID("UI");
                canvas.sortingOrder = 5; // Ensure popup is above tags
            }

            return lObjects;
        }

        public void MakeObjectVisible(GameObject tagObject, int sortingOrder = 4)
        {
            _tagsToShowDic.TryAdd(tagObject.transform, tagObject.transform.parent);
            var canvas = tagObject.AddComponentIfNotExists<Canvas>();
            if (canvas != null && canvas.sortingLayerID == SortingLayer.NameToID("UI"))
                return;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = SortingLayer.NameToID("UI");
            canvas.sortingOrder = sortingOrder;
            tagObject.AddComponentIfNotExists<GraphicRaycaster>();
        }

        private void PlayShowAnimation()
        {
            if (animator != null)
            {
                animator.Play("popup_show");
            }
        }

        public virtual void ShowAnimationSound()
        {
            audioService.PlaySound(appearSound);
        }

        public virtual void AfterShowAnimation()
        {
            OnShowAction?.Invoke();
        }

        public virtual void CloseAnimationSound()
        {
            audioService.PlaySound(disappearSound);
        }

        public virtual void Close()
        {
            if (closeButton)
            {
                closeButton.interactable = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
            }

            OnBeforeCloseAction?.Invoke(this);
            if (animator != null)
            {
                animator.Play("popup_hide");
            }
            HideTags();
        }

        private void HideTags()
        {
            foreach (var t in _tagsToShowDic)
            {
                if (!t.Key || !t.Key.gameObject)
                    continue;
                var customButton = t.Key?.gameObject?.GetComponent<CustomButton>();
                if (customButton)
                {
                    customButton.interactable = true;
                }
                Destroy(t.Key.GetComponent<GraphicRaycaster>());
                Destroy(t.Key.GetComponent<Canvas>());
            }
            _tagsToShowDic.Clear();
        }

        public virtual void AfterHideAnimation()
        {
            OnClosePopup?.Invoke(this);
            OnCloseAction?.Invoke(result);
            Destroy(gameObject, .5f);
        }

        protected virtual void OnDisable()
        {
            DOTween.Kill(gameObject);
        }

        public void OnActivate()
        {
            ShowTags(tagsToShow);
            canvasGroup.interactable = true;
            canvasGroup.DOFade(1, 0.1f);
        }

        public virtual void Hide()
        {
            canvasGroup.interactable = false;
            canvasGroup.DOFade(0, 0.5f);
        }

        public void CloseDelay()
        {
            Invoke(nameof(Close), 0.5f);
        }

        protected void StopInteration()
        {
            canvasGroup.interactable = false;
        }

    }
}