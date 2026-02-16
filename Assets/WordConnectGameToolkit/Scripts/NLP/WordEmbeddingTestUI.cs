using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace WordsToolkit.Scripts.NLP
{
    public class WordEmbeddingTestUI : MonoBehaviour
    {
        [FormerlySerializedAs("wordModel")]
        [SerializeField] private ModelController wordModelController;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button testButton;

        private Color validColor = new Color(0.7f, 1f, 0.7f); // Light green
        private Color invalidColor = new Color(1f, 0.7f, 0.7f); // Light red
        private Color defaultColor = Color.white;

        void Start()
        {
            testButton.onClick.AddListener(TestInputWord);
            inputField.onEndEdit.AddListener(delegate { TestInputWord(); });
        }

        public void TestInputWord()
        {
            string word = inputField.text.Trim().ToLower();
            if (string.IsNullOrEmpty(word))
            {
                inputField.GetComponent<Image>().color = defaultColor;
                return;
            }

            float[] vector = wordModelController.GetWordVector(word);
            bool isKnown = vector != null && !IsZeroVector(vector);
            inputField.GetComponent<Image>().color = isKnown ? validColor : invalidColor;
        }

        private bool IsZeroVector(float[] vector)
        {
            foreach (float value in vector)
            {
                if (!Mathf.Approximately(value, 0f))
                    return false;
            }
            return true;
        }
    }
}