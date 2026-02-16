using UnityEngine;

namespace WordsToolkit.Scripts.Audio 
{
    public interface IAudioService 
    {
        void PlaySound(AudioClip clip);
        void PlayDelayed(AudioClip clip, float delay);
        void PlaySoundsRandom(AudioClip[] clips);
        void PlayPopupShow();
        void PlayPopupClose();
        void PlayClick(AudioClip overrideClickSound);
        void PlayCoins();
        void PlayIncremental(int selectedLettersCount);
        void PlayOpenWord();
        void PlayBonus();
        void PlayWrong();
        void PlayWin();
        void PlayBonusGetting();
        void PlaySoundExclusive(AudioClip sound);
    }
}