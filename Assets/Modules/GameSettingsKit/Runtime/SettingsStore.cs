using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameSettingsKit
{
    // 설정값의 로드·저장(PlayerPrefs)·적용을 담당한다. UI와 무관하게 동작하므로 패널 없이도 사용할 수 있다.
    public static class SettingsStore
    {
        // 다른 프로젝트/같은 PC의 다른 게임과 키가 겹치지 않게 하려면 첫 Load 전에 바꾼다.
        public static string KeyPrefix = "GameSettingsKit.";

        public static SettingsData Current;
        public static event Action<SettingsData> Changed;

        private static bool loaded;
        private static AudioMixer mixer;
        private static string musicParam;
        private static string sfxParam;
        private static List<Vector2Int> resolutionCache;
        private static SettingsData? defaults;

        // 기본값은 저장값을 적용하기 전(게임 시작 시점)의 실행 환경 상태로 한 번만 기록한다.
        // 매번 새로 계산하면 사용자가 바꾼 현재 상태가 기본값이 되어 "기본값 복원"이 동작하지 않는다.
        public static SettingsData Defaults
        {
            get
            {
                if (!defaults.HasValue)
                {
                    defaults = SettingsData.CreateDefault();
                }
                return defaults.Value;
            }
        }

        public static bool HasMixer => mixer != null;

        // Enter Play Mode에서 도메인 리로드를 끈 경우에도 이전 플레이의 정적 상태가 남지 않도록 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            loaded = false;
            defaults = null;
            mixer = null;
            resolutionCache = null;
            Changed = null;
        }

        // 게임 시작 시 저장된 설정을 자동 적용(패널이 씬에 없어도 적용되도록)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup()
        {
            EnsureLoaded();
            Apply();
        }

        public static void EnsureLoaded()
        {
            if (!loaded)
            {
                Load();
            }
        }

        public static void Load()
        {
            SettingsData d = Defaults;
            d.masterVolume = PlayerPrefs.GetFloat(KeyPrefix + "MasterVolume", d.masterVolume);
            d.musicVolume = PlayerPrefs.GetFloat(KeyPrefix + "MusicVolume", d.musicVolume);
            d.sfxVolume = PlayerPrefs.GetFloat(KeyPrefix + "SfxVolume", d.sfxVolume);
            d.fullscreen = PlayerPrefs.GetInt(KeyPrefix + "Fullscreen", d.fullscreen ? 1 : 0) == 1;
            d.resolutionWidth = PlayerPrefs.GetInt(KeyPrefix + "ResolutionWidth", d.resolutionWidth);
            d.resolutionHeight = PlayerPrefs.GetInt(KeyPrefix + "ResolutionHeight", d.resolutionHeight);
            d.qualityLevel = PlayerPrefs.GetInt(KeyPrefix + "QualityLevel", d.qualityLevel);
            d.vSync = PlayerPrefs.GetInt(KeyPrefix + "VSync", d.vSync ? 1 : 0) == 1;
            Current = d;
            loaded = true;
        }

        public static void Save()
        {
            SettingsData d = Current;
            PlayerPrefs.SetFloat(KeyPrefix + "MasterVolume", d.masterVolume);
            PlayerPrefs.SetFloat(KeyPrefix + "MusicVolume", d.musicVolume);
            PlayerPrefs.SetFloat(KeyPrefix + "SfxVolume", d.sfxVolume);
            PlayerPrefs.SetInt(KeyPrefix + "Fullscreen", d.fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(KeyPrefix + "ResolutionWidth", d.resolutionWidth);
            PlayerPrefs.SetInt(KeyPrefix + "ResolutionHeight", d.resolutionHeight);
            PlayerPrefs.SetInt(KeyPrefix + "QualityLevel", d.qualityLevel);
            PlayerPrefs.SetInt(KeyPrefix + "VSync", d.vSync ? 1 : 0);
            PlayerPrefs.Save();
        }

        // 값을 바꾸고 즉시 적용한다(저장은 Save 호출 시점에 수행).
        public static void Set(SettingsData data)
        {
            EnsureLoaded();
            SettingsData previous = Current;
            Current = data;
            Apply(previous);
            Changed?.Invoke(Current);
        }

        public static void ResetToDefaults()
        {
            Set(Defaults);
        }

        // 음악/효과음 분리 볼륨을 쓰려면 AudioMixer의 노출(Exposed) 파라미터 이름과 함께 연결한다.
        public static void BindMixer(AudioMixer audioMixer, string musicVolumeParam, string sfxVolumeParam)
        {
            mixer = audioMixer;
            musicParam = musicVolumeParam;
            sfxParam = sfxVolumeParam;
            ApplyAudio();
        }

        public static void Apply()
        {
            ApplyAudio();
            ApplyDisplay(true);
            ApplyQuality();
        }

        private static void Apply(SettingsData previous)
        {
            ApplyAudio();
            bool displayChanged = previous.fullscreen != Current.fullscreen
                || previous.resolutionWidth != Current.resolutionWidth
                || previous.resolutionHeight != Current.resolutionHeight;
            ApplyDisplay(displayChanged);
            ApplyQuality();
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = Mathf.Clamp01(Current.masterVolume);
            if (mixer != null)
            {
                SetMixerVolume(musicParam, Current.musicVolume);
                SetMixerVolume(sfxParam, Current.sfxVolume);
            }
        }

        private static void SetMixerVolume(string param, float linear)
        {
            if (!string.IsNullOrEmpty(param))
            {
                // 선형(0~1) 값을 데시벨로 변환, 0은 -80dB(무음)로 처리
                mixer.SetFloat(param, linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f);
            }
        }

        private static void ApplyDisplay(bool force)
        {
            if (!force)
            {
                return;
            }

            FullScreenMode mode = Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Current.resolutionWidth > 0 && Current.resolutionHeight > 0)
            {
                Screen.SetResolution(Current.resolutionWidth, Current.resolutionHeight, mode);
            }
            else
            {
                Screen.fullScreenMode = mode;
            }
        }

        private static void ApplyQuality()
        {
            if (Current.qualityLevel >= 0 && Current.qualityLevel < QualitySettings.names.Length
                && QualitySettings.GetQualityLevel() != Current.qualityLevel)
            {
                QualitySettings.SetQualityLevel(Current.qualityLevel, true);
            }
            // 품질 단계 변경이 vSync 값을 덮어쓰므로 항상 그 뒤에 적용
            QualitySettings.vSyncCount = Current.vSync ? 1 : 0;
        }

        // 선택 가능한 해상도 목록(주사율만 다른 중복 항목 제거, 큰 것부터)
        public static IReadOnlyList<Vector2Int> GetResolutions()
        {
            if (resolutionCache == null)
            {
                resolutionCache = new List<Vector2Int>();
                foreach (Resolution r in Screen.resolutions)
                {
                    Vector2Int size = new Vector2Int(r.width, r.height);
                    if (!resolutionCache.Contains(size))
                    {
                        resolutionCache.Add(size);
                    }
                }
                if (resolutionCache.Count == 0)
                {
                    resolutionCache.Add(new Vector2Int(Screen.width, Screen.height));
                }

                // 모니터가 지원하는 가장 큰 해상도(보통 모니터 기본 해상도)를 맨 위로, 아래로 갈수록 작아지게 정렬
                resolutionCache.Sort((a, b) =>
                {
                    int byWidth = b.x.CompareTo(a.x);
                    return byWidth != 0 ? byWidth : b.y.CompareTo(a.y);
                });
            }
            return resolutionCache;
        }
    }
}
