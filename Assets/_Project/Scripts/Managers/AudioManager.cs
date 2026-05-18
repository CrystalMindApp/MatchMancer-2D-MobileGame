using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrystalMind.MatchMancer
{
    public class AudioManager : MonoBehaviour
    {
        #region Variables

        private const string MasterVolumeKey = "MatchMancer.Audio.MasterVolume";
        private const string BgmVolumeKey = "MatchMancer.Audio.BgmVolume";
        private const string SfxVolumeKey = "MatchMancer.Audio.SfxVolume";
        private const string MutedKey = "MatchMancer.Audio.Muted";

        [Header("Settings")]
        [SerializeField, Min(0f)] private float bgmFadeDuration = 0.75f;
        [SerializeField, Min(0f)] private float defaultSfxMinInterval = 0.05f;

        [Header("Volume Limits")]
        [SerializeField, Range(0f, 1f)] private float maxMasterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float maxBgmVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float maxSfxVolume = 1f;

        [Header("References")]
        [SerializeField] private AudioSource bgmSourceA;
        [SerializeField] private AudioSource bgmSourceB;
        [SerializeField] private AudioSource sfxSource;

        // Cache
        private readonly Dictionary<AudioClip, float> lastSfxTimes = new Dictionary<AudioClip, float>();
        private Coroutine bgmFadeRoutine;
        private AudioSource activeBgmSource;
        private AudioSource inactiveBgmSource;

        // State
        private float masterVolume = 1f;
        private float bgmVolume = 1f;
        private float sfxVolume = 1f;
        private bool muted;
        private AudioClip currentBgmClip;
        private AudioClip pendingBgmClip;

        #endregion

        #region Properties

        public static AudioManager Instance { get; private set; }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            SetInitialBgmSources();
            LoadSettings();
            ApplySourceVolumes();
        }

        #endregion

        #region Public Methods

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            SaveSettings();
            ApplySourceVolumes();
        }

        public void SetBgmVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            SaveSettings();
            ApplySourceVolumes();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            SaveSettings();
            ApplySourceVolumes();
        }

        public void SetMuted(bool value)
        {
            muted = value;
            SaveSettings();
            ApplySourceVolumes();
        }

        public void ToggleMute()
        {
            SetMuted(!muted);
        }

        public float GetMasterVolume()
        {
            return masterVolume;
        }

        public float GetBgmVolume()
        {
            return bgmVolume;
        }

        public float GetSfxVolume()
        {
            return sfxVolume;
        }

        public bool IsMuted()
        {
            return muted;
        }

        public void PlayBgm(AudioClip clip)
        {
            if (clip == null || currentBgmClip == clip || pendingBgmClip == clip)
            {
                return;
            }

            EnsureAudioSources();
            SetInitialBgmSources();

            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
                bgmFadeRoutine = null;
                pendingBgmClip = null;
            }

            AudioSource fromSource = activeBgmSource;
            AudioSource toSource = inactiveBgmSource;

            if (toSource == null)
            {
                return;
            }

            toSource.Stop();
            toSource.clip = clip;
            toSource.loop = true;
            toSource.volume = 0f;
            toSource.Play();

            if (fromSource == null || !fromSource.isPlaying || bgmFadeDuration <= 0f)
            {
                if (fromSource != null)
                {
                    fromSource.Stop();
                    fromSource.volume = 0f;
                }

                toSource.volume = GetEffectiveBgmVolume();
                activeBgmSource = toSource;
                inactiveBgmSource = fromSource != null ? fromSource : GetOtherBgmSource(toSource);
                currentBgmClip = clip;
                return;
            }

            pendingBgmClip = clip;
            bgmFadeRoutine = StartCoroutine(CrossfadeBgmRoutine(fromSource, toSource, clip));
        }

        public void StopBgm()
        {
            if (bgmFadeRoutine != null)
            {
                StopCoroutine(bgmFadeRoutine);
                bgmFadeRoutine = null;
            }

            bgmSourceA?.Stop();
            bgmSourceB?.Stop();
            currentBgmClip = null;
            pendingBgmClip = null;
        }

        public void PlaySfx(AudioClip clip)
        {
            PlaySfx(clip, defaultSfxMinInterval);
        }

        public void PlaySfx(AudioClip clip, float minInterval)
        {
            if (clip == null)
            {
                return;
            }

            float safeMinInterval = Mathf.Max(0f, minInterval);
            float currentTime = Time.unscaledTime;

            if (lastSfxTimes.TryGetValue(clip, out float lastTime) && currentTime - lastTime < safeMinInterval)
            {
                return;
            }

            EnsureAudioSources();
            lastSfxTimes[clip] = currentTime;

            if (GetEffectiveSfxVolume() > 0f)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator CrossfadeBgmRoutine(AudioSource fromSource, AudioSource toSource, AudioClip newClip)
        {
            float duration = Mathf.Max(0f, bgmFadeDuration);
            float elapsed = 0f;
            float fromStartVolume = fromSource != null ? fromSource.volume : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float targetVolume = GetEffectiveBgmVolume();

                if (fromSource != null)
                {
                    fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, t);
                }

                if (toSource != null)
                {
                    toSource.volume = Mathf.Lerp(0f, targetVolume, t);
                }

                yield return null;
            }

            float finalTargetVolume = GetEffectiveBgmVolume();

            if (fromSource != null)
            {
                fromSource.Stop();
                fromSource.volume = 0f;
            }

            if (toSource != null)
            {
                toSource.volume = finalTargetVolume;
            }

            activeBgmSource = toSource;
            inactiveBgmSource = fromSource != null ? fromSource : GetOtherBgmSource(toSource);
            currentBgmClip = newClip;
            pendingBgmClip = null;
            bgmFadeRoutine = null;
        }

        private void EnsureAudioSources()
        {
            bgmSourceA = EnsureAudioSource(bgmSourceA, "BGM Source A");
            bgmSourceB = EnsureAudioSource(bgmSourceB, "BGM Source B");
            sfxSource = EnsureAudioSource(sfxSource, "SFX Source");
        }

        private void SetInitialBgmSources()
        {
            if (activeBgmSource != null && inactiveBgmSource != null)
            {
                return;
            }

            activeBgmSource = bgmSourceA;
            inactiveBgmSource = bgmSourceB;
        }

        private AudioSource EnsureAudioSource(AudioSource source, string sourceName)
        {
            if (source != null)
            {
                if (source.transform.parent != transform)
                {
                    source.transform.SetParent(transform);
                }

                source.playOnAwake = false;
                return source;
            }

            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform);
            AudioSource newSource = sourceObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            return newSource;
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplySourceVolumes()
        {
            float bgmEffectiveVolume = GetEffectiveBgmVolume();

            if (bgmFadeRoutine == null)
            {
                if (bgmSourceA != null)
                {
                    bgmSourceA.volume = bgmSourceA == activeBgmSource && bgmSourceA.isPlaying ? bgmEffectiveVolume : 0f;
                }

                if (bgmSourceB != null)
                {
                    bgmSourceB.volume = bgmSourceB == activeBgmSource && bgmSourceB.isPlaying ? bgmEffectiveVolume : 0f;
                }
            }

            if (sfxSource != null)
            {
                sfxSource.volume = GetEffectiveSfxVolume();
            }
        }

        private float GetEffectiveBgmVolume()
        {
            return muted ? 0f : maxMasterVolume * masterVolume * maxBgmVolume * bgmVolume;
        }

        private float GetEffectiveSfxVolume()
        {
            return muted ? 0f : maxMasterVolume * masterVolume * maxSfxVolume * sfxVolume;
        }

        private AudioSource GetOtherBgmSource(AudioSource source)
        {
            return source == bgmSourceA ? bgmSourceB : bgmSourceA;
        }

        #endregion
    }
}
