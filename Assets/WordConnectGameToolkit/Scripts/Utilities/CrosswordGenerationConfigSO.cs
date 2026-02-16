using System;
using UnityEngine;

namespace WordsToolkit.Scripts.Utilities
{
    public class CrosswordGenerationConfigSO : ScriptableObject
    {
        // Columns and rows are now managed in the editor UI
        
        [Header("Word Placement")]
        [Tooltip("Minimum percentage of words that should be horizontal")]
        [Range(50, 100)]
        public int minHorizontalRatio = 70;
        
        [Tooltip("Words longer than this prefer horizontal placement")]
        [Range(3, 10)]
        public int verticalWordMaxLength = 4;
        
        [Tooltip("Words this size or smaller can go vertical more easily")]
        [Range(1, 5)]
        public int smallWordMaxLength = 3;
        
        [Tooltip("Whether to ensure different layouts on regeneration")]
        public bool forceUniqueLayout = true;
        
        [Header("Performance")]
        [Tooltip("Maximum number of attempts before using fallback algorithm")]
        [Range(5, 50)]
        public int maxAttempts = 20;
        
        [Tooltip("Random seed for deterministic generation (0 for random)")]
        public int seed = 0;

        [Tooltip("Maximum retries for overlapping words")]
        public int maxOverlapRetries = 10;
        
        // Convert this scriptable object to the struct used by CrosswordGenerator
        public CrosswordGenerationConfig ToConfig()
        {
            return new CrosswordGenerationConfig
            {
                // Columns and rows will be supplied by the editor
                columns = 15, // Default value, but will be overridden
                rows = 15, // Default value, but will be overridden
                seed = this.seed,
                maxAttempts = this.maxAttempts,
                maxOverlapRetries = this.maxOverlapRetries
            };
        }
        
        [ContextMenu("Reset To Defaults")]
        public void ResetToDefaults()
        {
            // Columns and rows are now managed in the editor UI
            seed = 0;
            maxAttempts = 20;
            minHorizontalRatio = 70;
            verticalWordMaxLength = 4;
            smallWordMaxLength = 3;
            forceUniqueLayout = true;

            // Notify that the object has been modified
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}
