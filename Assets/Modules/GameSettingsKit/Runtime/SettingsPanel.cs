using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;
#if GSK_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameSettingsKit
{
    // 설정 창 UI와 SettingsStore를 연결한다. 모든 UI 참조는 선택 사항이며, 비어 있는 항목은 무시한다.
    // 이 컴포넌트가 붙은 오브젝트는 항상 활성 상태로 두고, 실제로 보이고 숨기는 것은 window 오브젝트로 제어한다.
    public class SettingsPanel : MonoBehaviour
    {
        [Header("창")]
        public GameObject window; // 열고 닫을 실제 표시 루트
        public bool closeOnEscape = true;

        [Header("오디오")]
        public Slider masterVolumeSlider;
        public Text masterVolumeValue;
        public Slider musicVolumeSlider;
        public Text musicVolumeValue;
        public Slider sfxVolumeSlider;
        public Text sfxVolumeValue;
        [Tooltip("음악/효과음 분리 볼륨용. 비워 두면 음악·효과음 행은 숨기고 마스터 볼륨만 사용")]
        public AudioMixer audioMixer;
        public string musicVolumeParameter = "MusicVolume";
        public string sfxVolumeParameter = "SfxVolume";
        public GameObject musicRow;
        public GameObject sfxRow;

        [Header("화면")]
        public Toggle fullscreenToggle;
        public Dropdown resolutionDropdown;
        public Dropdown qualityDropdown;
        public Toggle vSyncToggle;

        [Header("버튼")]
        public Button resetButton;
        public Button closeButton;

        [Header("이벤트")]
        public UnityEvent onOpened;
        public UnityEvent onClosed;

        private static readonly List<SettingsPanel> openPanels = new List<SettingsPanel>();
        private static int escapeHandledFrame = -1;

        public static bool IsAnyOpen => openPanels.Count > 0;

        // 같은 프레임에 게임 쪽 ESC(일시정지 토글 등)가 중복 처리되지 않도록 확인할 때 사용
        public static bool BlocksEscapeThisFrame => IsAnyOpen || escapeHandledFrame == Time.frameCount;

        public bool IsOpen => window != null && window.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            openPanels.Clear();
            escapeHandledFrame = -1;
        }

        private void Awake()
        {
            SettingsStore.EnsureLoaded();

            // 믹서 값 적용은 Start에서 한다(AudioMixer.SetFloat는 Awake에서 호출하면 무시됨 — 실측 확인)
            bool hasMixer = audioMixer != null || SettingsStore.HasMixer;
            if (musicRow != null) musicRow.SetActive(hasMixer);
            if (sfxRow != null) sfxRow.SetActive(hasMixer);

            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(v => Modify(d => { d.masterVolume = v; return d; }));
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(v => Modify(d => { d.musicVolume = v; return d; }));
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(v => Modify(d => { d.sfxVolume = v; return d; }));
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(v => Modify(d => { d.fullscreen = v; return d; }));
            if (vSyncToggle != null) vSyncToggle.onValueChanged.AddListener(v => Modify(d => { d.vSync = v; return d; }));
            if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(i => Modify(d => { d.qualityLevel = i; return d; }));
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(i =>
                {
                    Vector2Int size = SettingsStore.GetResolutions()[i];
                    Modify(d => { d.resolutionWidth = size.x; d.resolutionHeight = size.y; return d; });
                });
            }
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
            if (closeButton != null) closeButton.onClick.AddListener(Close);

            BuildOptions();
            SpeedUpDropdownScroll(resolutionDropdown);
            SpeedUpDropdownScroll(qualityDropdown);
            if (window != null) window.SetActive(false);
        }

        private void Start()
        {
            if (audioMixer != null)
            {
                SettingsStore.BindMixer(audioMixer, musicVolumeParameter, sfxVolumeParameter);
            }
        }

        private void OnDisable()
        {
            openPanels.Remove(this);
        }

        private void Update()
        {
            if (closeOnEscape && IsOpen && EscapePressed())
            {
                escapeHandledFrame = Time.frameCount;
                Close();
            }
        }

        public void Open()
        {
            if (window == null || IsOpen)
            {
                return;
            }
            RefreshControls();
            window.SetActive(true);
            if (!openPanels.Contains(this)) openPanels.Add(this);
            onOpened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }
            SettingsStore.Save();
            window.SetActive(false);
            openPanels.Remove(this);
            onClosed?.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public void ResetToDefaults()
        {
            SettingsStore.ResetToDefaults();
            RefreshControls();
        }

        // 현재 값의 복사본을 바꿔 즉시 적용(저장은 창을 닫을 때)
        private void Modify(System.Func<SettingsData, SettingsData> change)
        {
            SettingsStore.Set(change(SettingsStore.Current));
            RefreshValueLabels();
        }

        // 드롭다운 목록의 기본 스크롤 속도(1)는 너무 느려 휠 한 칸에 항목이 거의 움직이지 않는다
        private const float DropdownScrollSensitivity = 60f;

        private static void SpeedUpDropdownScroll(Dropdown dropdown)
        {
            if (dropdown == null || dropdown.template == null)
            {
                return;
            }
            ScrollRect scroll = dropdown.template.GetComponent<ScrollRect>();
            if (scroll != null)
            {
                scroll.scrollSensitivity = DropdownScrollSensitivity;
            }
        }

        private void BuildOptions()
        {
            if (resolutionDropdown != null)
            {
                var options = new List<string>();
                foreach (Vector2Int r in SettingsStore.GetResolutions())
                {
                    options.Add(r.x + " x " + r.y);
                }
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(options);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
            }
        }

        // 현재 설정값을 UI에 반영(이벤트를 발생시키지 않음)
        public void RefreshControls()
        {
            SettingsData d = SettingsStore.Current;
            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(d.masterVolume);
            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(d.musicVolume);
            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(d.sfxVolume);
            if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(d.fullscreen);
            if (vSyncToggle != null) vSyncToggle.SetIsOnWithoutNotify(d.vSync);
            if (qualityDropdown != null)
            {
                qualityDropdown.SetValueWithoutNotify(d.qualityLevel >= 0 ? d.qualityLevel : QualitySettings.GetQualityLevel());
            }
            if (resolutionDropdown != null)
            {
                int w = d.resolutionWidth > 0 ? d.resolutionWidth : Screen.width;
                int h = d.resolutionHeight > 0 ? d.resolutionHeight : Screen.height;
                resolutionDropdown.SetValueWithoutNotify(FindResolutionIndex(w, h));
            }
            RefreshValueLabels();
        }

        private void RefreshValueLabels()
        {
            SettingsData d = SettingsStore.Current;
            SetPercent(masterVolumeValue, d.masterVolume);
            SetPercent(musicVolumeValue, d.musicVolume);
            SetPercent(sfxVolumeValue, d.sfxVolume);
        }

        private static void SetPercent(Text label, float value)
        {
            if (label != null)
            {
                label.text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }

        // 정확히 일치하는 해상도가 없으면 면적이 가장 가까운 항목을 선택
        private static int FindResolutionIndex(int width, int height)
        {
            IReadOnlyList<Vector2Int> list = SettingsStore.GetResolutions();
            int best = 0;
            long bestDiff = long.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                long diff = System.Math.Abs((long)list[i].x * list[i].y - (long)width * height)
                    + System.Math.Abs(list[i].x - width);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = i;
                }
            }
            return best;
        }

        private static bool EscapePressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#elif GSK_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return false;
#endif
        }
    }
}
