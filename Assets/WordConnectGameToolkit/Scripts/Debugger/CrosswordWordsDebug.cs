using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WordsToolkit.Scripts.Gameplay.Managers;
using WordsToolkit.Scripts.Levels;
using WordsToolkit.Scripts.System;
using VContainer;

namespace WordsToolkit.Scripts.Debugger
{
    public class CrosswordWordsDebug : MonoBehaviour
    {
        [SerializeField] private bool showDebugButton = true;
        [SerializeField] private bool showWordsList = false;
        private LevelManager levelManager;
        private StateManager stateManager;
        private Vector2 scrollPosition = Vector2.zero;

        [Inject]
        public void Construct(LevelManager levelManager, StateManager stateManager)
        {
            this.levelManager = levelManager;
            this.stateManager = stateManager;
        }

        private void OnGUI()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (!showDebugButton)
                return;

            // Only show debug button when in Game state
            if (stateManager == null || stateManager.CurrentState != EScreenStates.Game)
                return;

            // Debug button in the bottom-right corner - scale with screen width
            float buttonWidth = Screen.width * 0.16f; // 12% of screen width
            float buttonHeight = Screen.width * 0.06f; // 3% of screen width for proportional height
            float margin = Screen.width * 0.01f; // 1% of screen width for margin
            
            // Create scaled button style
            int buttonFontSize = Mathf.RoundToInt(buttonHeight * 0.4f); // 40% of button height
            GUIStyle buttonStyle = new GUIStyle(UnityEngine.GUI.skin.button);
            buttonStyle.fontSize = buttonFontSize;
            
            if (UnityEngine.GUI.Button(new Rect(Screen.width - buttonWidth - margin, Screen.height - buttonHeight - margin, buttonWidth, buttonHeight), "Show Words", buttonStyle))
            {
                showWordsList = !showWordsList;
            }

            // Words list panel along the bottom border
            if (showWordsList)
            {
                // Scale everything based on screen width
                float panelWidth = Screen.width * 0.85f; // 85% of screen width
                float panelHeight = Screen.width * 0.15f; // 15% of screen width for consistent proportions
                float minPanelWidth = Screen.width * 0.3f; // Minimum 30% of screen width
                float minPanelHeight = Screen.width * 0.08f; // Minimum 8% of screen width
                
                panelWidth = Mathf.Max(panelWidth, minPanelWidth);
                panelHeight = Mathf.Max(panelHeight, minPanelHeight);
                
                // Position the panel down and to the left
                float panelX = (Screen.width - panelWidth) * 0.3f; // Move left (30% from left instead of centered)
                float panelY = Screen.height - panelHeight - (Screen.width * 0.02f); // Move down (smaller bottom margin)
                
                // Ensure panel doesn't go off screen
                panelX = Mathf.Clamp(panelX, Screen.width * 0.005f, Screen.width - panelWidth - Screen.width * 0.005f);
                panelY = Mathf.Clamp(panelY, Screen.width * 0.005f, Screen.height - panelHeight - Screen.width * 0.005f);
                
                // Background panel
                UnityEngine.GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "");
                
                float padding = Screen.width * 0.005f; // Padding scaled to screen width
                GUILayout.BeginArea(new Rect(panelX + padding, panelY + padding, panelWidth - padding * 2, panelHeight - padding * 2));
                
                // Compact header with close button - font sizes based on panel height
                GUILayout.BeginHorizontal();
                int headerFontSize = Mathf.RoundToInt(panelHeight * 0.15f); // 15% of panel height
                GUILayout.Label("Words", new GUIStyle(UnityEngine.GUI.skin.label)
                { 
                    fontSize = headerFontSize,
                    fontStyle = FontStyle.Bold
                });
                GUILayout.FlexibleSpace();
                float closeButtonSize = panelHeight * 0.2f; // Close button size based on panel height
                if (GUILayout.Button("X", GUILayout.Width(closeButtonSize), GUILayout.Height(closeButtonSize)))
                {
                    showWordsList = false;
                }
                GUILayout.EndHorizontal();
                
                // Get current level and display words
                Level currentLevel = levelManager.GetCurrentLevel();
                if (currentLevel != null)
                {
                    var languageData = currentLevel.GetLanguageData(levelManager.GetCurrentLanguage());

                    if (languageData != null && languageData.crosswordData != null && languageData.crosswordData.placements != null)
                    {
                        // Get words from crossword placements
                        var words = languageData.crosswordData.placements
                            .Where(p => !string.IsNullOrEmpty(p.word))
                            .Select(p => p.word.ToUpper())
                            .Distinct()
                            .OrderBy(w => w)
                            .ToList();

                        // Responsive scrollable word list
                        float scrollViewHeight = panelHeight - (Screen.width * 0.04f); // Adjust based on panel height minus header, scaled
                        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scrollViewHeight));

                        // Display words in horizontal rows, dynamically fitting the width
                        int wordFontSize = Mathf.RoundToInt(panelHeight * 0.12f); // 12% of panel height
                        
                        // Calculate approximate character width for font size estimation
                        float charWidth = wordFontSize * 0.6f; // Approximate character width
                        float separatorWidth = charWidth * 3; // Width for "  •  " separator
                        
                        string currentLine = "";
                        float currentLineWidth = 0;
                        
                        foreach (var word in words)
                        {
                            float wordWidth = word.Length * charWidth;
                            float totalWidth = currentLineWidth + (currentLine.Length > 0 ? separatorWidth : 0) + wordWidth;
                            
                            // Check if adding this word would exceed the panel width
                            if (currentLine.Length > 0 && totalWidth > panelWidth - 40) // 40px padding buffer
                            {
                                // Display current line and start new one
                                GUILayout.Label(currentLine, new GUIStyle(UnityEngine.GUI.skin.label) { fontSize = wordFontSize });
                                currentLine = word;
                                currentLineWidth = wordWidth;
                            }
                            else
                            {
                                // Add word to current line
                                if (currentLine.Length > 0)
                                {
                                    currentLine += "  •  " + word;
                                    currentLineWidth += separatorWidth + wordWidth;
                                }
                                else
                                {
                                    currentLine = word;
                                    currentLineWidth = wordWidth;
                                }
                            }
                        }
                        
                        // Display the last line if it has content
                        if (currentLine.Length > 0)
                        {
                            GUILayout.Label(currentLine, new GUIStyle(UnityEngine.GUI.skin.label) { fontSize = wordFontSize });
                        }


                        GUILayout.EndScrollView();
                    }
                }
                GUILayout.EndArea();
            }
#endif
        }
    }
}