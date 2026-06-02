using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BFunCoreKit
{
    public enum AudioType
    {
        Sound,
        Music
    }

    [System.Serializable]
    public struct AudioSetting
    {
        public bool loop;
        public int priority;
        public float pitch;
        public float volume;
        public AudioType audioType;
        public float fadeTime;

        public static AudioSetting Default => new AudioSetting
        {
            loop = false,
            priority = 128,
            pitch = 1f,
            volume = 1f,
            audioType = AudioType.Sound,
            fadeTime = 0.25f
        };
    }

    [System.Serializable]
    public class SoundData
    {
        public string soundName;
        public AudioClip clip;
    }

    public class SoundManager : Singleton<SoundManager>
    {
        [ReadOnly] public SoundSettingData soundSettingData;

        private Dictionary<string, SoundData> dicSound;
        [SerializeField] private AudioSourcePool audioSourcePool;
        private Dictionary<string, AudioSource> activeLoopingSounds = new Dictionary<string, AudioSource>();

        // --- EVENTS (Observer Pattern giống Coin) ---
        public event Action OnSFXVolumeChanged;
        public event Action OnMusicVolumeChanged;

        // --- BACKING FIELDS ---
        private float _sfxVolume;
        private float _musicVolume;

        // --- PROPERTIES ---

        public float SFXVolume
        {
            get { return _sfxVolume; }
            set
            {
                // Chỉ xử lý nếu giá trị thực sự thay đổi (dùng sai số nhỏ cho float)
                if (Mathf.Abs(_sfxVolume - value) > 0.001f)
                {
                    _sfxVolume = Mathf.Clamp(value, 0f, 1f);

                    // 1. Cập nhật Mixer (Chuyển từ Linear 0-1 sang dB)
                    // Clamp min 0.0001 để tránh Log(0) = -Infinity
                    float dbValue = Mathf.Log10(Mathf.Clamp(_sfxVolume, 0.0001f, 1f)) * 20f;
                    soundSettingData.audioMixer.SetFloat("SFXVolume", dbValue);

                    // 2. Lưu PlayerPrefs
                    PlayerPrefs.SetFloat("Setting_SFXVolume", _sfxVolume);
                    PlayerPrefs.Save();

                    // 3. Bắn Event thông báo
                    OnSFXVolumeChanged?.Invoke();
                }
            }
        }

        public float MusicVolume
        {
            get { return _musicVolume; }
            set
            {
                if (Mathf.Abs(_musicVolume - value) > 0.001f)
                {
                    _musicVolume = Mathf.Clamp(value, 0f, 1f);

                    // 1. Cập nhật Mixer
                    float dbValue = Mathf.Log10(Mathf.Clamp(_musicVolume, 0.0001f, 1f)) * 20f;
                    soundSettingData.audioMixer.SetFloat("MusicVolume", dbValue);

                    // 2. Lưu PlayerPrefs
                    PlayerPrefs.SetFloat("Setting_MusicVolume", _musicVolume);
                    PlayerPrefs.Save();
                    // 3. Bắn Event thông báo
                    OnMusicVolumeChanged?.Invoke();
                }
            }
        }

        // Global Audio Listener Volume
        public float AudioVolume
        {
            get { return AudioListener.volume; }
            set { AudioListener.volume = soundSettingData.AudioVolume = Mathf.Clamp(value, 0, 1); }
        }

        // --- INITIALIZATION ---

        public override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            if (soundSettingData == null)
            {
                soundSettingData = AssetDatabase.LoadAssetAtPath<SoundSettingData>(GlobalConst.SettingFolder + "/Sound Setting.asset");
            }
#endif

            dicSound = new Dictionary<string, SoundData>();
            foreach (var s in soundSettingData.sounds)
            {
                if (!dicSound.ContainsKey(s.soundName))
                    dicSound.Add(s.soundName, s);
            }

            // Load dữ liệu từ PlayerPrefs vào biến Backing Field
            // Mặc định là 1 (Full Volume) nếu chưa lưu bao giờ
            _sfxVolume = PlayerPrefs.GetFloat("Setting_SFXVolume", 1f);
            _musicVolume = PlayerPrefs.GetFloat("Setting_MusicVolume", 1f);
        }

        private void Start()
        {
            // Apply volume lên Mixer khi game bắt đầu (Start đảm bảo Mixer đã init xong)
            // Gọi trực tiếp set param Mixer thay vì qua Property để tránh kích hoạt Event/Save không cần thiết lúc init
            float sfxDb = Mathf.Log10(Mathf.Clamp(_sfxVolume, 0.0001f, 1f)) * 20f;
            soundSettingData.audioMixer.SetFloat("SFXVolume", sfxDb);

            float musicDb = Mathf.Log10(Mathf.Clamp(_musicVolume, 0.0001f, 1f)) * 20f;
            soundSettingData.audioMixer.SetFloat("MusicVolume", musicDb);
        }

        // --- PLAY SOUND FUNCTIONS ---

        public void PlaySound(SoundName soundName)
        {
            PlaySound(soundName, AudioSetting.Default);
        }

        public void PlaySound(SoundName soundName, AudioSetting setting)
        {
            if (!dicSound.TryGetValue(soundName.ToString(), out var data) || data.clip == null)
            {
                Debug.LogWarning($"SoundManager: sound '{soundName}' not found!");
                return;
            }
            PlaySoundInternal(soundName, data.clip, setting);
        }

        public void PlaySound(AudioClip clip, AudioSetting setting = default)
        {
            if (clip == null) return;
            PlaySoundInternal(null, clip, setting);
        }

        private void PlaySoundInternal(SoundName? soundKey, AudioClip clip, AudioSetting setting)
        {
            var src = audioSourcePool.Get();

            src.clip = clip;
            src.loop = setting.loop;
            src.priority = setting.priority;
            src.pitch = setting.pitch;
            src.volume = 0f; // Bắt đầu từ 0 để fade in
            src.outputAudioMixerGroup = setting.audioType == AudioType.Music ? soundSettingData.musicGroup : soundSettingData.sfxGroup;

            src.Play();
            StartCoroutine(FadeVolume(src, 0f, setting.volume, setting.fadeTime));

            if (setting.loop && soundKey.HasValue)
            {
                activeLoopingSounds[soundKey.Value.ToString()] = src;
            }
            else if (!setting.loop)
            {
                StartCoroutine(ReleaseWhenDone(src, setting.fadeTime));
            }
        }

        // --- STOP SOUND FUNCTIONS ---

        public void StopSound(SoundName soundName, float fadeTime = 0.25f)
        {
            if (activeLoopingSounds.TryGetValue(soundName.ToString(), out var src))
            {
                activeLoopingSounds.Remove(soundName.ToString());
                if (src != null && src.isPlaying)
                    StartCoroutine(StopAndRelease(src, fadeTime));
            }
        }

        public void StopAllSounds(float fadeTime = 0.25f)
        {
            foreach (var kvp in activeLoopingSounds)
            {
                if (kvp.Value != null && kvp.Value.isPlaying)
                    StartCoroutine(StopAndRelease(kvp.Value, fadeTime));
            }
            activeLoopingSounds.Clear();
        }

        // --- INTERNAL COROUTINES ---

        private IEnumerator ReleaseWhenDone(AudioSource src, float fadeTime)
        {
            yield return new WaitWhile(() => src.isPlaying);
            if (src != null) audioSourcePool.Release(src);
        }

        private IEnumerator StopAndRelease(AudioSource src, float fadeTime)
        {
            if (src == null) yield break;
            // Fade out volume hiện tại về 0
            yield return FadeVolume(src, src.volume, 0f, fadeTime);
            src.Stop();
            audioSourcePool.Release(src);
        }

        private IEnumerator FadeVolume(AudioSource src, float from, float to, float time)
        {
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                if (src == null) yield break;
                src.volume = Mathf.Lerp(from, to, t / time);
                yield return null;
            }
            if (src != null) src.volume = to;
        }
    }
}