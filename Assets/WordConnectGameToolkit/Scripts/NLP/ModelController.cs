using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unity.InferenceEngine;
using UnityEngine;
using System.Text;
using System.Globalization;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.Services;
using WordsToolkit.Scripts.Services.BannedWords;
using VContainer;
using Random = System.Random;

namespace WordsToolkit.Scripts.NLP
{
    public class ModelController : IModelController
    {
        private ILanguageService languageService;
        private LanguageConfiguration languageConfiguration;

        [SerializeField]
        Dictionary<string, ModelAsset> languageModels = new Dictionary<string, ModelAsset>();
        private IBannedWordsService bannedWordsService;

        private Dictionary<string, Worker> m_Workers = new Dictionary<string, Worker>();
        private Dictionary<string, Dictionary<string, int>> wordToIndexByLanguage = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<string, int> m_VectorDimensionByLanguage = new Dictionary<string, int>();
        private Dictionary<string, int> m_VocabSizeByLanguage = new Dictionary<string, int>();
        private Dictionary<string, Tensor<int>> m_InputsByLanguage = new Dictionary<string, Tensor<int>>();

        private string m_DefaultLanguage = "en";
        public int VectorDimension = 100;

        // Protection flag to prevent accidental binary overwrite when you have custom words  
        // NOTE: This is now mainly for the old SaveModelBinary method - new architecture uses custom words files
        private bool protectBinaryFile = false;

        /// <summary>
        /// Normalizes text by removing diacritics, accents, and converting to lowercase.
        /// This allows word matching to ignore emphasis marks.
        /// </summary>
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            text = text.ToLower();
            
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public bool IsModelLoaded(string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);
            return m_Workers.ContainsKey(language) &&
                   wordToIndexByLanguage.ContainsKey(language) &&
                   wordToIndexByLanguage[language].Count > 0;
        }

        public ModelController(IBannedWordsService bannedWordsService,
                              LanguageConfiguration languageConfiguration,
                              ILanguageService languageService = null)
        {
            this.languageService = languageService;
            this.languageConfiguration = languageConfiguration;
            this.bannedWordsService = bannedWordsService;
            InitializeFromConfiguration();
            LoadModels();
        }

        public IEnumerable<string> AvailableLanguages => m_Workers.Keys;

        private void InitializeFromConfiguration()
        {
            // Use LanguageService to get current language if available, fallback to configuration default
            if (languageService != null)
            {
                m_DefaultLanguage = languageService.GetCurrentLanguageCode();
            }
            else
            {
                m_DefaultLanguage = languageConfiguration?.defaultLanguage ?? "en";
            }
            
            languageModels.Clear();

            foreach (var langInfo in languageConfiguration.languages)
            {
                if (!string.IsNullOrEmpty(langInfo.code) && langInfo.languageModel != null)
                {
                    languageModels[langInfo.code] = langInfo.languageModel;
                }
            }
        }

        public void LoadModels()
        {
            InitializeFromConfiguration();
            foreach (var languagePair in languageModels)
            {
                LoadModelBin(languagePair.Key, languagePair.Value);
            }
        }

        /// <summary>
        /// Sets whether to protect existing binary files from being overwritten.
        /// When true, LoadModel() won't overwrite existing .bin files that might contain custom words.
        /// </summary>
        public void SetBinaryFileProtection(bool protect)
        {
            protectBinaryFile = protect;
            Debug.Log($"[ModelController] Binary file protection {(protect ? "ENABLED" : "DISABLED")}");
        }

        public void LoadModelBin(string language, ModelAsset modelAsset)
        {
            // Always load the model structure first
            var model = ModelLoader.Load(modelAsset);

            // (Re)create worker + input tensor
            if (m_Workers.ContainsKey(language))
            {
                m_Workers[language].Dispose();
                m_InputsByLanguage[language]?.Dispose();
            }

            m_Workers[language] = new Worker(model, BackendType.CPU);
            m_InputsByLanguage[language] = new Tensor<int>(new TensorShape(1));

            // Always load base vocabulary from ONNX JSON (no caching)
            var worker = m_Workers[language];
            var dummyInput = new Tensor<int>(new TensorShape(1));
            dummyInput[0] = 0;

            try
            {
                worker.Schedule(dummyInput);
                var vocabJsonTensor = worker.PeekOutput("wc_vocab_json");

                if (vocabJsonTensor != null)
                {
                    try
                    {
                        var byteTensor = vocabJsonTensor as Tensor<byte>;
                        if (byteTensor != null)
                        {
                            var jsonData = new byte[byteTensor.shape.length];
                            var cpuTensor = byteTensor.ReadbackAndClone();
                            try
                            {
                                for (int i = 0; i < jsonData.Length; i++)
                                    jsonData[i] = cpuTensor[i];

                                string jsonString = Encoding.UTF8.GetString(jsonData);
                                ParseAndLoadVocabulary(language, jsonString);
                            }
                            finally
                            {
                                cpuTensor.Dispose();
                            }
                        }
                        else if (vocabJsonTensor is Tensor<int> intTensor)
                        {
                            var cpuTensor = intTensor.ReadbackAndClone();
                            try
                            {
                                var jsonBytes = new List<byte>();
                                for (int i = 0; i < cpuTensor.shape.length; i++)
                                {
                                    int value = cpuTensor[i];
                                    if (value == 0)
                                        break;
                                    if (value >= 0 && value <= 255)
                                        jsonBytes.Add((byte)value);
                                }

                                if (jsonBytes.Count > 0)
                                {
                                    string jsonString = Encoding.UTF8.GetString(jsonBytes.ToArray());
                                    ParseAndLoadVocabulary(language, jsonString);
                                }
                            }
                            finally
                            {
                                cpuTensor.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        vocabJsonTensor.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error loading base vocabulary for {language}: {e.Message}", e);
            }
            finally
            {
                dummyInput.Dispose();
            }

            // Now load any custom words from binary file
            LoadCustomWordsFromBinary(language);
        }

        /// <summary>
        /// Loads bytes from StreamingAssets using UnityWebRequest for Android compatibility
        /// </summary>
        private byte[] LoadStreamingAssetBytes(string path)
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using var request = UnityEngine.Networking.UnityWebRequest.Get(path);
                var operation = request.SendWebRequest();
                while (!operation.isDone) { }
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.data;
                }
                return null;
#else
                if (File.Exists(path))
                {
                    return File.ReadAllBytes(path);
                }
                return null;
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelController] Exception in LoadStreamingAssetBytes: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads custom words from binary file and adds them to the existing vocabulary.
        /// Binary file contains ONLY custom words, not the entire model cache.
        /// </summary>
        private void LoadCustomWordsFromBinary(string language)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "WordConnectGameToolkit", "model",
                "custom", $"{language}_custom_words.bin");
            
            if (!wordToIndexByLanguage.ContainsKey(language))
            {
                return;
            }

            byte[] fileData = LoadStreamingAssetBytes(path);
            if (fileData == null)
            {
                return;
            }

            try
            {
                using var ms = new MemoryStream(fileData);
                using var br = new BinaryReader(ms, Encoding.UTF8);

                // Read header
                if (br.ReadInt32() != 0x43555354) // "CUST" magic number
                {
                    return;
                }

                int baseVocabSize = br.ReadInt32();    // Original vocab size when custom words were added
                int customWordCount = br.ReadInt32();  // Number of custom words
                int vectorDim = br.ReadInt32();        // Vector dimension

                // Verify compatibility
                if (vectorDim != m_VectorDimensionByLanguage[language])
                {
                    return;
                }

                // Load custom words
                int currentVocabSize = wordToIndexByLanguage[language].Count;
                int nextIndex = currentVocabSize;

                for (int i = 0; i < customWordCount; i++)
                {
                    string word = br.ReadString();
                    
                    // Add to vocabulary
                    wordToIndexByLanguage[language][word] = nextIndex++;
                }

                // Update vocabulary size
                m_VocabSizeByLanguage[language] = nextIndex;

            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelController] Error loading custom words for '{language}': {e.Message}");
            }
        }

        /// <summary>
        /// Saves only the custom words (not base vocabulary) to binary file.
        /// This creates a lightweight file with just the added words.
        /// </summary>
        private void SaveCustomWordsToBinary(string language)
        {
            if (!wordToIndexByLanguage.ContainsKey(language))
            {
                Debug.LogError($"[ModelController] No vocabulary loaded for '{language}' - cannot save custom words");
                return;
            }

            // Create StreamingAssets folder structure
            string streamingAssetsDir = Path.Combine(Application.dataPath, "StreamingAssets");
            string modelDir = Path.Combine(streamingAssetsDir, "WordConnectGameToolkit", "model", "custom");
            string path = Path.Combine(modelDir, $"{language}_custom_words.bin");
            Directory.CreateDirectory(modelDir);

            try
            {
                // Get all custom words (assuming they have higher indices than base vocabulary)
                var vocab = wordToIndexByLanguage[language];
                var baseVocabSize = GetEstimatedBaseVocabSize(language);
                var customWords = vocab.Where(kvp => kvp.Value >= baseVocabSize)
                                      .OrderBy(kvp => kvp.Value)
                                      .ToList();

                if (customWords.Count == 0)
                {
                    Debug.Log($"[ModelController] No custom words to save for '{language}'");
                    return;
                }

                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                using var bw = new BinaryWriter(fs, Encoding.UTF8);

                // Write header
                bw.Write(0x43555354); // "CUST" magic number
                bw.Write(baseVocabSize); // Original vocab size when custom words were added
                bw.Write(customWords.Count); // Number of custom words
                bw.Write(m_VectorDimensionByLanguage[language]); // Vector dimension

                // Write custom words (just the words, not embeddings since we can't access them easily)
                foreach (var kvp in customWords)
                {
                    bw.Write(kvp.Key); // Word string
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelController] Error saving custom words for '{language}': {e.Message}");
            }
        }

        /// <summary>
        /// Estimates the base vocabulary size by looking at the original model.
        /// This is a heuristic - in a real implementation you might want to store this value.
        /// </summary>
        private int GetEstimatedBaseVocabSize(string language)
        {
            // Try to load the original model and get its vocabulary size
            if (languageModels.TryGetValue(language, out var modelAsset) && modelAsset != null)
            {
                try
                {
                    var model = ModelLoader.Load(modelAsset);
                    using var worker = new Worker(model, BackendType.CPU);
                    using var dummyInput = new Tensor<int>(new TensorShape(1));
                    dummyInput[0] = 0;
                    
                    worker.Schedule(dummyInput);
                    var vocabJsonTensor = worker.PeekOutput("wc_vocab_json");
                    
                    if (vocabJsonTensor != null)
                    {
                        // Parse JSON to get original vocab size
                        // This is simplified - you'd need to extract and parse the JSON
                        vocabJsonTensor.Dispose();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ModelController] Could not determine base vocab size for '{language}': {e.Message}");
                }
            }
            
            // Fallback heuristic - assume custom words start after a reasonable base size
            var currentSize = wordToIndexByLanguage[language].Count;
            return currentSize > 1000 ? currentSize - 100 : Math.Max(1, currentSize / 2);
        }

        private void ParseAndLoadVocabulary(string language, string jsonString)
        {
            try
            {
                var jsonObject = JObject.Parse(jsonString);
                var wordToIndex = new Dictionary<string, int>();
                var wordIndexDict = jsonObject["word_to_index"].ToObject<Dictionary<string, int>>();

                foreach (var pair in wordIndexDict)
                {
                    string normalizedWord = NormalizeText(pair.Key);
                    wordToIndex[normalizedWord] = pair.Value;
                }


                int vectorDimension = jsonObject["vector_size"].Value<int>();
                int vocabSize = jsonObject["vocab_size"].Value<int>();

                wordToIndexByLanguage[language] = wordToIndex;
                m_VectorDimensionByLanguage[language] = vectorDimension;
                m_VocabSizeByLanguage[language] = vocabSize;
            }
            catch (Exception e)
            {
                throw new Exception($"Error parsing vocabulary JSON for {language}: {e.Message}", e);
            }
        }

        public float[] GetWordVector(string word, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            if (!IsModelLoaded(language))
            {
                return null;
            }

            word = NormalizeText(word);
            if (!wordToIndexByLanguage[language].ContainsKey(word))
            {
                return null;
            }

            m_InputsByLanguage[language][0] = wordToIndexByLanguage[language][word];
            m_Workers[language].Schedule(m_InputsByLanguage[language]);

            var outputTensor = m_Workers[language].PeekOutput() as Tensor<float>;
            if (outputTensor == null)
            {
                return null;
            }

            var cpuTensor = outputTensor.ReadbackAndClone();
            try
            {
                float[] result = new float[m_VectorDimensionByLanguage[language]];
                for (int i = 0; i < m_VectorDimensionByLanguage[language]; i++)
                {
                    result[i] = cpuTensor[i];
                }
                return result;
            }
            finally
            {
                cpuTensor.Dispose();
                outputTensor.Dispose();
            }
        }

        public bool IsWordKnown(string word, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);
            string normalizedWord = NormalizeText(word);
            if (bannedWordsService.IsWordBanned(normalizedWord, language))
            {
                return false;
            }

            if (!IsModelLoaded(language))
            {
                return false;
            }

            float[] vector = GetWordVector(word, language);
            if (vector == null)
            {
                return false;
            }

            return true;
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

        public float GetCosineSimilarity(string word1, string word2, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            if (!IsModelLoaded(language))
            {
                return -1f;
            }

            word1 = NormalizeText(word1);
            word2 = NormalizeText(word2);
            float[] vector1 = GetWordVector(word1, language);
            float[] vector2 = GetWordVector(word2, language);

            if (vector1 == null || vector2 == null)
                return -1f;

            return CosineSimilarity(vector1, vector2);
        }

        public bool AddWordAndSave(string newWord, string language = null)
        {
            language ??= languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage;

            if (!IsModelLoaded(language))
            {
                Debug.LogWarning($"[ModelController] AddWord failed – model for '{language}' not loaded.");
                return false;
            }

            newWord = NormalizeText(newWord);
            if (wordToIndexByLanguage[language].ContainsKey(newWord))
            {
                Debug.LogWarning($"[ModelController] Word '{newWord}' already exists in vocab.");
                return false;
            }

            var newVector = new float[m_VectorDimensionByLanguage[language]];
            int dim = m_VectorDimensionByLanguage[language];
            if (newVector == null || newVector.Length != dim)
            {
                Debug.LogWarning($"[ModelController] Vector length mismatch (expected {dim}).");
                return false;
            }

            // 1️⃣ Update dictionaries
            int newIndex = m_VocabSizeByLanguage[language];
            wordToIndexByLanguage[language][newWord] = newIndex;
            m_VocabSizeByLanguage[language] = newIndex + 1;

            // 2️⃣ Load the Model anew so we can mutate its constants safely
            if (!languageModels.TryGetValue(language, out var modelAsset) || modelAsset == null)
            {
                Debug.LogError($"[ModelController] Missing ModelAsset for language '{language}'.");
                return false;
            }

            var model = ModelLoader.Load(modelAsset);

            // We assume the <b>first</b> constant is the [vocab, dim] embedding matrix.
            // If your graph changes, adjust the index or name match here.
            if (model.constants == null || model.constants.Count == 0)
            {
                Debug.LogError("[ModelController] Model has no constants — cannot extend.");
                return false;
            }

            var embConst = model.constants[0];
            if (embConst.dataType != DataType.Float || embConst.shape.rank != 2 || embConst.shape[1] != dim)
            {
                Debug.LogError("[ModelController] Unexpected embedding tensor layout.");
                return false;
            }

            int oldVocab  = embConst.shape[0];
            int oldElems  = embConst.shape.length;              // vocab * dim
            float[] oldBuf = new float[oldElems];
            NativeTensorArray.Copy(embConst.weights, 0, oldBuf, 0, oldElems);

            // Build new buffer = old + newVector
            var newBuf = new float[oldElems + dim];
            Buffer.BlockCopy(oldBuf,   0, newBuf, 0, oldElems * sizeof(float));
            Buffer.BlockCopy(newVector,0, newBuf, oldElems * sizeof(float), dim * sizeof(float));

            // Inference Engine requires a non‑generic NativeTensorArrayFromManagedArray
            // Inference Engine requires (Array, bytesPerElem, length, channels)
            // ctor args: (Array data, int srcElementOffset, int srcElementSize, int numDestElement)
            var newWeights = new NativeTensorArrayFromManagedArray(
                newBuf,   // managed float[]
                0,        // start at element 0
                sizeof(float),
                newBuf.Length);  // total elements
#pragma warning disable 618 // Constant.weights setter is obsolete but still functional
            embConst.weights = newWeights;
#pragma warning restore 618
            embConst.shape   = new TensorShape(oldVocab + 1, dim);   // update shape metadata

            // 3️⃣ Write new BIN
            SaveCustomWordsToBinary(language);

            Debug.Log($"[ModelController] Successfully added word '{newWord}' to {language} vocabulary at index {newIndex}");
            return true;
        }

        private float CosineSimilarity(float[] v1, float[] v2)
        {
            float dotProduct = 0;
            float norm1 = 0;
            float norm2 = 0;

            for (int i = 0; i < v1.Length; i++)
            {
                dotProduct += v1[i] * v2[i];
                norm1 += v1[i] * v1[i];
                norm2 += v2[i] * v2[i];
            }

            return dotProduct / (Mathf.Sqrt(norm1) * Mathf.Sqrt(norm2));
        }

        private void OnDisable()
        {
            foreach (var worker in m_Workers.Values)
            {
                worker?.Dispose();
            }

            foreach (var input in m_InputsByLanguage.Values)
            {
                input?.Dispose();
            }

            m_Workers.Clear();
            m_InputsByLanguage.Clear();
            wordToIndexByLanguage.Clear();
            m_VectorDimensionByLanguage.Clear();
            m_VocabSizeByLanguage.Clear();
        }

        public void Dispose()
        {
            OnDisable();
        }

        public List<string> GetRandomWords(int wordCount, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            if (!IsModelLoaded(language))
            {
                return new List<string>();
            }

            var result = new List<string>();
            var words = new List<string>(wordToIndexByLanguage[language].Keys)
                .Where(word => !bannedWordsService.IsWordBanned(word, language))
                .ToList();
            var random = new Random();

            wordCount = Mathf.Min(wordCount, words.Count);

            while (result.Count < wordCount)
            {
                int randomIndex = random.Next(words.Count);
                string word = words[randomIndex];

                if (!result.Contains(word))
                    result.Add(word);
            }

            return result;
        }

        public List<string> GetAllWords(string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            if (!IsModelLoaded(language))
            {
                return new List<string>();
            }

            return new List<string>(wordToIndexByLanguage[language].Keys)
                .Where(word => !bannedWordsService.IsWordBanned(word, language))
                .ToList();
        }

        public List<string> GetWordsFromSymbols(string inputSymbols, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            if(!IsModelLoaded(language))
            {
                return new List<string>();
            }

            if (string.IsNullOrEmpty(inputSymbols))
                return new List<string>();

            inputSymbols = NormalizeText(inputSymbols);
            Dictionary<char, int> charCounts = new Dictionary<char, int>();
            foreach (char c in inputSymbols)
            {
                if (charCounts.ContainsKey(c))
                    charCounts[c]++;
                else
                    charCounts[c] = 1;
            }

            var candidateWords = new List<string>();
            foreach (var word in wordToIndexByLanguage[language].Keys)
            {
                if (bannedWordsService != null && bannedWordsService.IsWordBanned(word, language))
                    continue;

                Dictionary<char, int> remainingCounts = new Dictionary<char, int>(charCounts);
                bool isValid = true;

                foreach (char c in word)
                {
                    if (!remainingCounts.ContainsKey(c) || remainingCounts[c] <= 0)
                    {
                        isValid = false;
                        break;
                    }
                    remainingCounts[c]--;
                }

                if (isValid)
                {
                    candidateWords.Add(word);
                }
            }

            if(!candidateWords.Contains(inputSymbols) &&
               !bannedWordsService.IsWordBanned(inputSymbols, language) && IsWordKnown(inputSymbols, language))
            {
                candidateWords.Add(inputSymbols);
            }

            float[] referenceVector = CreateInputTensor(inputSymbols, language);
            if (referenceVector != null && candidateWords.Count > 0)
            {
                return RankWordsBySimilarity(candidateWords, referenceVector, 200, language);
            }

            return candidateWords
                .OrderByDescending(w => w.Length)
                .ToList();
        }

        public float[] CreateInputTensor(string inputSymbols, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            if (!IsModelLoaded(language))
            {
                return null;
            }

            if (string.IsNullOrEmpty(inputSymbols))
                return null;

            inputSymbols = NormalizeText(inputSymbols);
            var symbolSet = new HashSet<char>(inputSymbols);

            var bestMatches = wordToIndexByLanguage[language].Keys
                .Select(word => new {
                    Word = word,
                    SharedChars = word.Count(c => symbolSet.Contains(c))
                })
                .OrderByDescending(x => (float)x.SharedChars / x.Word.Length)
                .Take(5)
                .ToList();

            if (bestMatches.Count == 0)
                return null;

            float[] compositeVector = new float[m_VectorDimensionByLanguage[language]];
            foreach (var match in bestMatches)
            {
                float[] wordVector = GetWordVector(match.Word, language);
                if (wordVector != null)
                {
                    for (int i = 0; i < m_VectorDimensionByLanguage[language]; i++)
                    {
                        compositeVector[i] += wordVector[i];
                    }
                }
            }

            float magnitude = 0;
            for (int i = 0; i < m_VectorDimensionByLanguage[language]; i++)
            {
                magnitude += compositeVector[i] * compositeVector[i];
            }

            magnitude = Mathf.Sqrt(magnitude);
            if (magnitude > 0)
            {
                for (int i = 0; i < m_VectorDimensionByLanguage[language]; i++)
                {
                    compositeVector[i] /= magnitude;
                }
            }

            return compositeVector;
        }

        private List<string> RankWordsBySimilarity(List<string> candidateWords, float[] referenceVector, int maxResults, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            var scoredWords = new List<(string word, float score)>();

            foreach (var word in candidateWords)
            {
                float[] wordVector = GetWordVector(word, language);
                if (wordVector != null)
                {
                    float similarity = CosineSimilarity(referenceVector, wordVector);
                    float adjustedScore = similarity * (0.8f + 0.2f * Mathf.Min(1f, word.Length / 10f));
                    scoredWords.Add((word, adjustedScore));
                }
            }

            return scoredWords
                .OrderByDescending(pair => pair.score)
                .Take(maxResults)
                .Select(pair => pair.word)
                .ToList();
        }

        /// <summary>
        /// Gets a list of words that are exactly the specified length.
        /// Returns complete words only, does not truncate or modify the words.
        /// </summary>
        /// <param name="length">The exact length of words to return</param>
        /// <param name="language">Language code, defaults to current language if not specified</param>
        /// <returns>List of complete words of the specified length</returns>
        public List<string> GetWordsWithLength(int length, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            if (!IsModelLoaded(language))
            {
                return new List<string>();
            }

            if (length <= 0)
            {
                Debug.LogWarning($"Invalid word length requested: {length}. Length must be greater than 0.");
                return new List<string>();
            }

            var random = new Random();
            var matchingWords = wordToIndexByLanguage[language].Keys
                .Where(word => word != null && word.Length == length)
                .Where(word => bannedWordsService == null || !bannedWordsService.IsWordBanned(word, language))
                .ToList();

            if (matchingWords.Count == 0)
            {
                Debug.LogWarning($"No words found with exact length: {length}");
                return new List<string>();
            }

            var sampleWords = matchingWords
                .OrderBy(word => random.Next())
                .ToList();

            if (sampleWords.Count == 0)
            {
                return new List<string>();
            }

            return sampleWords;
        }

        private List<string> FindSimilarWords(float[] targetVector, int maxResults, string language = null)
        {
            language = language ?? (languageService?.GetCurrentLanguageCode() ?? m_DefaultLanguage);

            var scoredWords = new List<(string word, float score)>();

            foreach (var word in wordToIndexByLanguage[language].Keys)
            {
                if (bannedWordsService.IsWordBanned(word, language))
                    continue;

                float[] wordVector = GetWordVector(word, language);
                if (wordVector != null)
                {
                    float similarity = CosineSimilarity(targetVector, wordVector);
                    scoredWords.Add((word, similarity));
                }
            }

            return scoredWords
                .OrderByDescending(pair => pair.score)
                .Take(maxResults)
                .Select(pair => pair.word)
                .ToList();
        }

        public IEnumerable<string> GetAvailableLanguages()
        {
            return AvailableLanguages;
        }

        /// <summary>
        /// Clears custom words cache for a specific language or all languages.
        /// This only removes custom word files, not the base model data.
        /// </summary>
        /// <param name="language">Language to clear, or null to clear all</param>
        public void ClearCustomWordsCache(string language = null)
        {
            string customDir = Path.Combine(Application.dataPath, "StreamingAssets", "WordConnectGameToolkit", "model", "custom");
            
            if (!Directory.Exists(customDir))
                return;
                
            try
            {
                if (language != null)
                {
                    string customPath = Path.Combine(customDir, $"{language}_custom_words.bin");
                    if (File.Exists(customPath))
                    {
                        File.Delete(customPath);
                        Debug.Log($"[ModelController] Cleared custom words cache for language: {language}");
                    }
                }
                else
                {
                    var customFiles = Directory.GetFiles(customDir, "*_custom_words.bin");
                    foreach (var file in customFiles)
                    {
                        File.Delete(file);
                    }
                    Debug.Log($"[ModelController] Cleared all custom words cache files ({customFiles.Length} files)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelController] Error clearing custom words cache: {e.Message}");
            }
        }
    }

    public interface IModelController : IDisposable
    {
        bool IsModelLoaded(string language = null);
        void LoadModels();
        float[] GetWordVector(string word, string language = null);
        bool IsWordKnown(string word, string language = null);
        float GetCosineSimilarity(string word1, string word2, string language = null);
        List<string> GetRandomWords(int wordCount, string language = null);
        List<string> GetAllWords(string language = null);
        List<string> GetWordsFromSymbols(string inputSymbols, string language = null);
        List<string> GetWordsWithLength(int length, string language = null);
        IEnumerable<string> GetAvailableLanguages();
        float[] CreateInputTensor(string input, string language);
        bool AddWordAndSave(string newWord, string language = null);
        void ClearCustomWordsCache(string language = null);
        void SetBinaryFileProtection(bool protect);
    }
}