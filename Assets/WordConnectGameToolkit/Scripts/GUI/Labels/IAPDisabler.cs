using UnityEngine;
using WordsToolkit.Scripts.Settings;

namespace WordsToolkit.Scripts.GUI.Labels
{
    public class IAPDisabler : MonoBehaviour
    {
        private void OnEnable()
        {
            var gameSettings = Resources.Load<GameSettings>("Settings/GameSettings");
            if (gameSettings != null && !gameSettings.enableInApps)
            {
                gameObject.SetActive(false);
            }
        }
    }
}