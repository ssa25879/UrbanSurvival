# Game Settings Kit

다른 프로젝트에 폴더째 복사해서 쓸 수 있는 설정 창 모듈이다. 게임 코드에 의존하지 않는다.

- 의존성: Unity 6 이상, `com.unity.ugui`. Input System 패키지는 있으면 사용하고, 없어도 동작한다(구 Input Manager 사용).
- 설정 항목: 마스터 볼륨, 음악/효과음 볼륨(AudioMixer 연결 시), 전체 화면, 해상도, 그래픽 품질, VSync
- 저장: `PlayerPrefs`(키 접두어 `SettingsStore.KeyPrefix`, 기본 `GameSettingsKit.`)
- 저장된 설정은 게임 시작 시(첫 씬 로드 전) 자동으로 적용된다. 설정 창이 없는 씬에서도 적용된다.

## 폴더 구성

| 경로 | 내용 |
|---|---|
| `Runtime/SettingsData.cs` | 설정값 구조체와 기본값 |
| `Runtime/SettingsStore.cs` | 로드·저장·적용(UI와 무관, 코드에서 직접 사용 가능) |
| `Runtime/SettingsPanel.cs` | 설정 창 UI와 저장소 연결, 열기/닫기, ESC로 닫기 |
| `Runtime/SettingsPanelTheme.cs` | 자동 생성 시 사용할 폰트·스프라이트·색·문구 |
| `Editor/SettingsPanelBuilder.cs` | 설정 창 UI 계층 자동 생성 메뉴 |

## 사용법

1. (선택) Project 창에서 `Create > Game Settings Kit > Settings Panel Theme`로 테마를 만들고 폰트·스프라이트·색을 지정한다. 테마가 없으면 Unity 기본 UI 리소스로 만든다.
2. Hierarchy에서 Canvas를 선택하고 `GameObject > UI > Game Settings Panel`을 실행한다. 프로젝트에 테마 에셋이 있으면 처음 찾은 테마가 적용된다.
3. 설정을 열 버튼의 `onClick`에 `Settings Panel` 오브젝트의 `SettingsPanel.Open`을 연결한다.
4. 게임에 ESC 일시정지 같은 처리가 있으면, 설정 창이 ESC를 먼저 쓰도록 아래처럼 확인한다.

```csharp
if (Input.GetKeyDown(KeyCode.Escape) && !GameSettingsKit.SettingsPanel.BlocksEscapeThisFrame)
{
    TogglePause();
}
```

## 음악/효과음 볼륨

`SettingsPanel.audioMixer`에 AudioMixer를 지정하고, 믹서에서 노출(Exposed)한 파라미터 이름을 `musicVolumeParameter`, `sfxVolumeParameter`에 맞춘다(기본 `MusicVolume`, `SfxVolume`). 믹서를 지정하지 않으면 음악·효과음 행은 자동으로 숨겨지고 마스터 볼륨(`AudioListener.volume`)만 쓴다.

- 믹서 값은 `SettingsPanel.Start`에서 적용한다. `AudioMixer.SetFloat`는 `Awake`에서 호출하면 무시되기 때문이다(Unity 6.3에서 실측). 코드에서 `SettingsStore.BindMixer`를 직접 부를 때도 `Start` 이후에 호출한다.
- 음악을 재생하는 AudioSource는 `Music` 그룹, 효과음 AudioSource는 `SFX` 그룹으로 출력을 지정해야 슬라이더가 적용된다.

## 동작 규칙

- 값은 바꾸는 즉시 적용되고, 창을 닫을 때 저장된다.
- 창을 여는 동안 `SettingsPanel.IsAnyOpen`이 `true`다. Dim 이미지가 뒤쪽 UI 클릭을 막는다.
- `Time.timeScale = 0`(일시정지) 상태에서도 동작한다.
- 에디터 Game View에서는 해상도·전체 화면 변경이 반영되지 않는다. 빌드에서 확인한다.
- Linear 색 공간 프로젝트에서는 반투명 UI 뒤 내용이 예상보다 밝게 비친다(알파 0.96 창도 뒤 글씨가 보임). 테마의 창 배경색은 알파 1로 두는 것을 권장한다.
