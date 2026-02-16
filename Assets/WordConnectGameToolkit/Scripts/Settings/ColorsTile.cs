using UnityEngine;
using WordsToolkit.Scripts.Gameplay;

namespace WordsToolkit.Scripts.Settings
{
    [CreateAssetMenu(fileName = "ColorsTile", menuName ="WordConnectGameToolkit/Editor/ColorsTile", order = 1)]
    public class ColorsTile : ScriptableData
    {
        public Color faceColor = Color.white;
        public Color topColor = Color.white;
        public Color bottomColor = Color.white;
        // public GameObject customItemPrefab;

        public bool HasCustomPrefab() => false;
    }
}