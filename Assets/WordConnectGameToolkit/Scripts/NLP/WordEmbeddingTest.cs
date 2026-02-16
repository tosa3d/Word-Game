using UnityEngine;
using UnityEngine.Serialization;

namespace WordsToolkit.Scripts.NLP
{
    public class WordEmbeddingTest : MonoBehaviour
    {
        [FormerlySerializedAs("wordModel")]
        [SerializeField]
        private ModelController wordModelController;

        private string[] testWords = new string[]
        {
            "hello",
            "world",
            "xyz123", // unknown word
            "computer",
            "asdfghjkl", // unknown word
            "programming",
            "@#$%",
            "автор",
            "программирование",
            "привет",
            "мир",
        };

        void Start()
        {
            if (wordModelController == null)
            {
                wordModelController = GetComponent<ModelController>();
            }

            TestWordRecognition();
        }

        void TestWordRecognition()
        {
            Debug.Log("=== Testing Word Recognition ===");
            foreach (var word in testWords)
            {
                float[] vector = wordModelController.GetWordVector(word);
                bool isKnown = vector != null && !IsZeroVector(vector);
                Debug.Log($"Word: '{word}' - {(isKnown ? "✓ Known" : "✗ Unknown")}");
            }
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