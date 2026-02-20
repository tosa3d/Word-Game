using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using WordsToolkit.Scripts.Enums;
using WordsToolkit.Scripts.System;
using WordsToolkit.Scripts.System.Haptic;
using RTLTMPro;


namespace WordsToolkit.Scripts.Gameplay
{
    public class LetterButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        public TextMeshProUGUI letterText;
        public Image circleImage;

        private WordSelectionManager wordSelectionManager;
        private bool isSelected = false;
        private Color color;

        // متغیر برای ذخیره حرف اصلی و سالم
        private string originalLetter;

        private void Awake()
        {
            wordSelectionManager = GetComponentInParent<WordSelectionManager>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (wordSelectionManager != null && EventManager.GameStatus == EGameState.Playing)
            {
                wordSelectionManager.StartSelection(this);
                SetSelected(true);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (wordSelectionManager != null && wordSelectionManager.IsSelecting && EventManager.GameStatus == EGameState.Playing)
            {
                wordSelectionManager.AddToSelection(this);
                SetSelected(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (wordSelectionManager != null && EventManager.GameStatus == EGameState.Playing)
            {
                wordSelectionManager.EndSelection();
            }
        }

        public void SetSelected(bool selected)
        {
            if (isSelected != selected && selected)
            {
                HapticFeedback.TriggerHapticFeedback(HapticFeedback.HapticForce.Light);
            }
            isSelected = selected;
            circleImage.color = new Color(color.r, color.g, color.b, selected ? 1f : 0);
            letterText.color = selected ? Color.white : Color.black;
        }

        // این متد توسط WordSelectionManager صدا زده می‌شود تا کلمه را بسازد
        public string GetLetter()
        {
            // برگرداندن حرف اصلی (مثلاً "ب") نه حرف گرافیکی (مثلاً "ﺐ")
            return originalLetter;
        }

        public void SetText(string text)
        {
            // 1. ذخیره اصلِ حرف برای منطق بازی (بدون تغییر)
            originalLetter = text;

            // 2. تبدیل حرف برای نمایش صحیح در UI
            // طبق تستی که فرستادید، پارامتر آخر باید true باشد تا فارسی درست کار کند
            if (!string.IsNullOrEmpty(text))
            {
                // Use a FastStringBuilder for output as required by RTLSupport.FixRTL
                var output = new FastStringBuilder(RTLSupport.DefaultBufferSize);
                RTLSupport.FixRTL(text, output, true, true, false);
                Debug.Log(GetLetter() + " -> " + output.ToString());
                letterText.text = output.ToString();
            }
            else
            {
                letterText.text = "";
            }
        }

        public void SetColor(Color color)
        {
            this.color = color;
            circleImage.color = new Color(color.r, color.g, color.b, isSelected ? 1f : 0f);
        }
    }
}