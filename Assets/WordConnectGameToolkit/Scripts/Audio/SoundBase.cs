// // ©2015 - 2025 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Audio;
using WordsToolkit.Scripts.System;
using Random = UnityEngine.Random;

namespace WordsToolkit.Scripts.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundBase : MonoBehaviour, IAudioService
    {
        [SerializeField]
        private AudioMixer mixer;

        [SerializeField]
        private string soundParameter = "soundVolume";

        [SerializeField]
        public AudioClip click;

        public AudioClip[] swish;
        public AudioClip coins;
        public AudioClip coinsSpend;
        public AudioClip luckySpin;
        public AudioClip warningTime;
        public AudioClip bonus;
        public AudioClip gemSound;
        public AudioClip[] combo;

        private AudioSource audioSource;

        private readonly HashSet<AudioClip> clipsPlaying = new();
        [SerializeField]
        private AudioClip openWord;

        [SerializeField]
        private AudioClip wrong;

        [SerializeField]
        private AudioClip win;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            mixer.SetFloat(soundParameter, PlayerPrefs.GetInt("Sound", 1) == 0 ? -80 : 0);
        }

        public void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void PlayDelayed(AudioClip clip, float delay)
        {
            StartCoroutine(PlayDelayedCoroutine(clip, delay));
        }

        private IEnumerator PlayDelayedCoroutine(AudioClip clip, float delay)
        {
            yield return new WaitForSeconds(delay);
            PlaySound(clip);
        }

        public void PlaySoundsRandom(AudioClip[] clip)
        {
            PlaySound(clip[Random.Range(0, clip.Length)]);
        }

        public void PlayPopupShow()
        {
            // PlaySound(swish[0]);
        }

        public void PlayPopupClose()
        {
            // PlaySound(swish[1]);
        }

        public void PlayClick(AudioClip overrideClickSound)
        {
            PlaySound(overrideClickSound != null ? overrideClickSound : click);
        }

        public void PlayCoins()
        {
            PlaySound(coins);
        }

        public void PlayBonusGetting()
        {
            PlaySound(gemSound);
        }

        public void PlaySoundExclusive([NotNull] AudioClip sound)
        {
            if (sound == null)
            {
                return;
            }

            if (clipsPlaying.Add(sound))
            {
                audioSource.PlayOneShot(sound);
                StartCoroutine(WaitForCompleteSound(sound, 1));
            }
        }

        public void PlayIncremental(int selectedLettersCount)
        {
            if (selectedLettersCount > 0 && selectedLettersCount <= combo.Length)
            {
                PlaySound(combo[selectedLettersCount - 1]);
            }
        }

        public void PlayOpenWord()
        {
            PlayLimitSound(openWord, 1);
        }

        public void PlayBonus()
        {
            PlaySound(bonus);
        }

        public void PlayWrong()
        {
            PlaySound(wrong);
        }

        public void PlayWin()
        {
            PlaySound(win);
        }

        public void PlayLimitSound(AudioClip clip, int sec)
        {
            if (clipsPlaying.Add(clip))
            {
                PlaySound(clip);
                StartCoroutine(WaitForCompleteSound(clip, sec));
            }
        }

        private IEnumerator WaitForCompleteSound(AudioClip clip, int sec)
        {
            yield return new WaitForSeconds(sec);
            clipsPlaying.Remove(clip);
        }
    }
}