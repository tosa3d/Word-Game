using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using WordsToolkit.Scripts.Settings;

namespace WordConnectGameToolkit.Scripts.Settings.Editor
{
    [CustomEditor(typeof(InterstitialSettings))]
    public class InterstitialSettingsEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var interstitialSettings = (InterstitialSettings)target;
            
            // if (interstitialSettings.interstitials == null || interstitialSettings.interstitials.Length == 0)
            {
                var helpBox = new HelpBox("InterstitialSettings is empty. Click the button below to populate from AdsSettings.", HelpBoxMessageType.Info);
                root.Add(helpBox);
                
                var populateButton = new Button(() =>
                {
                    var adsSettings = FindAdsSettings();
                    if (adsSettings != null)
                    {
                        interstitialSettings.PopulateFromAdsSettings(adsSettings);
                        EditorUtility.SetDirty(interstitialSettings);
                        AssetDatabase.SaveAssets();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("AdsSettings Not Found", 
                            "Could not find AdsSettings ScriptableObject in the project.", "OK");
                    }
                })
                {
                    text = "Populate from AdsSettings"
                };
                root.Add(populateButton);
            }
                CreateDefaultInspector(root);

            return root;
        }

        private void CreateDefaultInspector(VisualElement root)
        {
            var interstitialsProperty = serializedObject.FindProperty("interstitials");
            var interstitialsField = new PropertyField(interstitialsProperty);
            interstitialsField.Bind(serializedObject);
            root.Add(interstitialsField);
        }
        
        private AdsSettings FindAdsSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:AdsSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AdsSettings>(path);
            }
            return null;
        }
    }
}