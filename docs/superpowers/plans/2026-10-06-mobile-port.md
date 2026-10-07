# Urban Survival Android 모바일 포팅 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 PC 게임 규칙을 유지한 채 Android 스마트폰·태블릿에서 터치로 플레이하고 Google Play 테스트 트랙에 올릴 수 있는 서명된 AAB를 만든다.

**Architecture:** 전투 판정은 그대로 두고 입력만 교체한다. 터치 UI가 정적 브리지 `MobileInputState`에 값을 쓰고 `PlayerInput`이 플랫폼에 따라 키보드·마우스 또는 이 브리지를 읽어 기존 출력(`move`, `fire` 등)과 새 `aimWorldDirection`을 낸다. 순수 로직(조이스틱 변환, 오토 에임 대상 선택, 발사 엣지)은 별도 어셈블리 `UrbanSurvival.MobileCore`로 분리해 EditMode 테스트하고, 터치 UI는 런타임에 코드로 생성해(바이너리 씬 수정 없이) 모바일에서만 붙인다.

**Tech Stack:** Unity 6000.3.19f1, URP 17.3.0(호환 모드 유지), uGUI 2.0.0(레거시 `Text`), 레거시 Input Manager, NUnit EditMode 테스트, Unity MCP(`mcp__unityMCP__*`)

**Spec:** `docs/superpowers/specs/2026-10-06-mobile-port-design.md` (기준 문서: `Urban_Survival_Android_모바일_포팅_상세_기획서_20261006_v1.0.docx`)

## Global Constraints

- 모든 모바일 작업은 `SideProject-Mobile` 브랜치에서만 커밋한다. 모든 Task 시작 전 `git branch --show-current`가 `SideProject-Mobile`인지 확인한다(아니면 중단).
- 포팅과 무관한 기존 변경(`Headlights.mat`, `ProjectSettings/ProjectSettings.asset`의 자동 재저장, 핸드오프 문서, `.aiassistant/`)은 커밋에 섞지 않는다. 항상 파일을 지정해서 `git add`한다. 단, Task 12는 의도한 `ProjectSettings.asset` 변경을 커밋한다.
- `PlayerShooter`의 전투 로직(발사·재장전·탄약·피해·Raycast·총구 막힘)과 `Gun`은 수정하지 않는다. `PlayerShooter`에는 읽기 전용 접근자 2개만 추가한다.
- PC 키보드·마우스 동작을 바꾸지 않는다. 모바일 분기는 `MobilePlatform.IsMobile`로 완전히 분리한다.
- `GameSettingsKit` 모듈에 Urban Survival 전용 타입을 넣지 않는다. 모듈에는 플랫폼 일반 가드(`Application.isMobilePlatform`)만 허용한다.
- 모바일 규칙: 가로 화면(Landscape)만, IL2CPP, ARM64, AAB, 최소 API 25, 타깃 API 36 이상, URP 호환 모드 유지(Render Graph 전환 금지).
- 앱 정보: 회사명 `YWS`, 패키지명 `com.yws.urbansurvival`, 제품명 `Urban Survival`.
- 터치 UI Canvas Scaler 기준 해상도 1920×1080, 터치 대상 최소 약 48dp(실기기에서 확인해 조정).
- 키스토어·비밀번호는 Git에 커밋하지 않는다.
- 목표 FPS: 동시 적 500마리 + 보스 소환 상태에서 60 FPS 목표, 최소 30 FPS. S26 Ultra와 XiaoxinPad 2025 두 기기에서 측정한다.
- 주석·로그·문서는 한국어로 쓰고, 수정하는 파일의 기존 코드 스타일(중괄호 위치, 한국어 주석 밀도)을 따른다.
- 스킬·프로젝트 지침: 기존 파일 수정 전 사용자 백업 확인이 AGENTS.md에 있다. 이 계획의 기존 파일 수정(`PlayerInput.cs` 등)은 Git 브랜치(`SideProject-Mobile`)가 원본을 보존하므로 파일 백업 대신 Git으로 갈음한다는 점을 실행 시작 시 사용자에게 한 번 확인한다(Task 0 Step 1).
- 작업 로그는 `D:\Codex\Log\YYYYMMDD_(작업이름)_Log.md`에 Task 묶음(P0~P4)마다 남긴다.

## Review Focus

- 터치 화면 밖 이탈·멀티터치: 이동 스틱을 잡은 채 발사 버튼·재장전 버튼을 동시에 눌러도 서로의 포인터를 빼앗지 않는다(Task 8·9에서 포인터별 추적).
- 적과 플레이어 위치가 같거나(거리 0) 후보가 0개·500개인 경우 오토 에임이 NaN·예외 없이 다음 우선순위(이동 방향 → 마지막 방향)로 넘어간다(Task 4 테스트).
- 사거리 경계: 거리가 정확히 무기 사거리인 적은 대상이다(`<=`), 사거리 0 이하(총 없음)이면 대상 없음(Task 4 테스트).
- 정지·포커스 복귀·조준 모드 전환 직후 손가락이 눌린 채여도 발사하지 않는다(Task 7 검증, Task 9·10 전환 처리).
- 재장전 직후 계속 누르고 있어도 즉시 발사하지 않는다(기존 `blockFireUntilRelease`가 `fire` 값으로 동작하므로 터치도 `fire`가 눌림 유지 중 true인 점을 Task 7에서 확인).
- 노치·펀치홀·둥근 모서리와 16:9·20:9·약 16:10(태블릿) 화면에서 HUD와 터치 버튼 겹침·잘림(Task 9, Task 12 실기기 검수).
- 모바일에서 PC 전용 설정(전체 화면·해상도·VSync)이 적용되어 화면이 깨지거나 프레임이 제한되는 문제(Task 10).
- 에디터 강제 모바일 플래그가 빌드에 남아 PC 빌드 입력을 막는 문제(Task 1 테스트, Task 7).

---

## File Structure

**신규**
- `Assets/Scripts/MobileCore/UrbanSurvival.MobileCore.asmdef` — 순수 로직 어셈블리(게임 코드에 의존하지 않음, `Assembly-CSharp`이 자동 참조)
- `Assets/Scripts/MobileCore/MobileInputState.cs` — 터치 UI → PlayerInput 정적 브리지
- `Assets/Scripts/MobileCore/MobilePlatform.cs` — 모바일 판정(에디터 강제 플래그 포함)
- `Assets/Scripts/MobileCore/JoystickMath.cs` — 스틱 변환·화면→월드 방향 변환
- `Assets/Scripts/MobileCore/TwinStickFireTracker.cs` — 조준 스틱 발사 엣지 판정
- `Assets/Scripts/MobileCore/AutoAimTargeting.cs` — 오토 에임 대상 선택
- `Assets/Scripts/MobileCore/MobileAimMode.cs` — 조준 모드 enum과 PlayerPrefs 저장
- `Assets/Scripts/MobileUI/MobileUIFactory.cs` — 런타임 UI 생성 보조(원형 스프라이트, Image/Text/RectTransform)
- `Assets/Scripts/MobileUI/TouchJoystick.cs` — 플로팅 스틱 위젯(이동·조준 공용)
- `Assets/Scripts/MobileUI/TouchButton.cs` — 누름/뗌 버튼 위젯(발사·재장전·일시정지·슬롯 공용)
- `Assets/Scripts/MobileUI/SafeAreaFitter.cs` — `Screen.safeArea` 적용
- `Assets/Scripts/MobileUI/MobileTouchOverlay.cs` — 오버레이 생성·상태 갱신·부트스트랩
- `Assets/Scripts/MobileUI/MobileAimModeSettingsBinder.cs` — 설정 창 조준 모드 토글 연결(모듈 밖)
- `Assets/Scripts/MobileUI/MobilePerformance.cs` — 모바일 프레임·품질 부트스트랩, 개발 빌드 전용 스트레스 도구
- `Assets/Editor/MobileEditorMenu.cs` — 에디터 강제 모바일 토글 메뉴, Android 설정 적용 메뉴
- `Assets/Game/Tests/EditMode/MobileInputStateTests.cs`, `JoystickMathTests.cs`, `TwinStickFireTrackerTests.cs`, `AutoAimTargetingTests.cs`, `MobileAimSettingsTests.cs`
- `docs/superpowers/plans/mobile-store-checklist.md` — Play Console 준비 체크리스트(Task 14)

**수정**
- `Assets/Game/Tests/EditMode/UrbanSurvival.EditMode.Tests.asmdef` — `UrbanSurvival.MobileCore` 참조 추가
- `Assets/Scripts/Zombie.cs` — 정적 `alive` 목록 추가
- `Assets/Scripts/PlayerShooter.cs` — 읽기 전용 접근자 `CurrentSlotIndex`, `IsSlotUnlocked`
- `Assets/Scripts/PlayerInput.cs` — 모바일 입력 경로, `aimWorldDirection`, `RequireFireRelease`
- `Assets/Scripts/PlayerMovement.cs` — `hasAimWorldDirection`이면 모바일 방향 사용
- `Assets/Scripts/ReloadIndicator.cs`, `IntroMenu.cs`, `UIManager.cs` — 모바일 문구·QUIT 숨김
- `Assets/Modules/GameSettingsKit/Runtime/SettingsStore.cs` — 모바일에서 해상도·전체 화면 적용 건너뛰기(플랫폼 일반 가드)
- `ProjectSettings/ProjectSettings.asset`, `ProjectSettings/QualitySettings.asset` — Android 설정, 모바일 품질 단계
- `.gitignore` — 키스토어 패턴

---

## Task 0: P0 — 브랜치·기준 확보

**Files:**
- Modify: `.gitignore`

- [ ] **Step 1: 실행 전 사용자 확인 사항 한 번 묻기**

  사용자에게 다음을 한 번에 확인한다: (a) 기존 파일 수정 백업을 Git 브랜치로 갈음해도 되는지(AGENTS.md는 수정 전 백업 여부 확인을 요구한다), (b) 에디터가 열려 있고 Unity MCP가 연결되어 있는지. 사용자가 파일 백업을 원하면 파일명 `원본파일명_YYYYMMDD_HHMM_백업.확장자`로 각 수정 파일을 백업한다.

- [ ] **Step 2: 브랜치·원격 상태 확인**

  Run: `git branch --show-current && git status --short && git ls-remote --heads origin SideProject-Mobile`
  Expected: `SideProject-Mobile`, 작업 트리에 기존 무관 변경만 있음, 원격에 `SideProject-Mobile` 존재.

- [ ] **Step 3: 키스토어 무시 패턴 추가**

  `.gitignore`의 `*.aab` 아래에 다음 줄을 추가한다.

  ```
  # Android signing keys (never commit)
  *.keystore
  *.jks
  keystore.properties
  /Keystore/
  ```

- [ ] **Step 4: PC 기준 동작 기록(회귀 비교 기준)**

  Unity MCP로 `Assets/Game/Scenes/UrbanSurvival.unity`를 열고 플레이한 뒤 다음 동작을 확인하고 결과를 작업 로그에 적는다: WASD 이동, 마우스 조준, 좌클릭 발사, R 재장전, 1~4 무기 교체(권총만 보유 시 2~4 무시), Esc 일시정지, 탄창 0일 때 자동 재장전 없음. 이상이 있으면 그 상태를 "기존 PC 문제"로 따로 기록하고 모바일 작업 범위에 넣지 않는다.

- [ ] **Step 5: Android 모듈 설치 확인**

  Run: `ls "C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data/PlaybackEngines/AndroidPlayer"`
  Expected: `SDK`, `NDK`, `OpenJDK` 폴더가 보인다. 없으면 Unity Hub에서 Android Build Support(SDK·NDK·OpenJDK 포함) 설치를 사용자에게 요청한다.

- [ ] **Step 6: Commit**

  ```bash
  git add .gitignore
  git commit -m "chore: Android 키스토어 파일 Git 추적 제외" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 1: MobileCore 어셈블리와 MobileInputState·MobilePlatform

**Files:**
- Create: `Assets/Scripts/MobileCore/UrbanSurvival.MobileCore.asmdef`
- Create: `Assets/Scripts/MobileCore/MobileInputState.cs`
- Create: `Assets/Scripts/MobileCore/MobilePlatform.cs`
- Modify: `Assets/Game/Tests/EditMode/UrbanSurvival.EditMode.Tests.asmdef`
- Test: `Assets/Game/Tests/EditMode/MobileInputStateTests.cs`

**Interfaces:**
- Produces:
  - `static class MobileInputState` — `Vector2 Move`, `Vector2 AimStick`, `bool FireHeld`, `void RequestFireDown()`, `void RequestReload()`, `void RequestSwap(int slotIndex)`, `bool ConsumeFireDown()`, `bool ConsumeReload()`, `int ConsumeSwap()`(요청 없으면 -1), `void ResetAll()`
  - `static class MobilePlatform` — `bool IsMobile`, 에디터 전용 `bool ForceMobileInEditor { get; set; }`

- [ ] **Step 1: 어셈블리 정의 만들기**

  `Assets/Scripts/MobileCore/UrbanSurvival.MobileCore.asmdef`:

  ```json
  {
      "name": "UrbanSurvival.MobileCore",
      "rootNamespace": "",
      "references": [],
      "includePlatforms": [],
      "excludePlatforms": [],
      "allowUnsafeCode": false,
      "overrideReferences": false,
      "precompiledReferences": [],
      "autoReferenced": true,
      "defineConstraints": [],
      "versionDefines": [],
      "noEngineReferences": false
  }
  ```

- [ ] **Step 2: 테스트 어셈블리가 MobileCore를 참조하게 수정**

  `UrbanSurvival.EditMode.Tests.asmdef`의 `references`를 다음으로 바꾼다.

  ```json
  "references": [
      "Assembly-CSharp",
      "UrbanSurvival.MobileCore",
      "UnityEngine.TestRunner",
      "UnityEditor.TestRunner"
  ],
  ```

- [ ] **Step 3: 실패하는 테스트 작성**

  `Assets/Game/Tests/EditMode/MobileInputStateTests.cs`:

  ```csharp
  using NUnit.Framework;
  using UnityEngine;

  public class MobileInputStateTests {
      [SetUp]
      public void SetUp() {
          MobileInputState.ResetAll();
      }

      [Test]
      public void FireDownRequest_IsConsumedOnce() {
          MobileInputState.RequestFireDown();
          Assert.IsTrue(MobileInputState.ConsumeFireDown());
          Assert.IsFalse(MobileInputState.ConsumeFireDown());
      }

      [Test]
      public void ReloadRequest_IsConsumedOnce() {
          MobileInputState.RequestReload();
          Assert.IsTrue(MobileInputState.ConsumeReload());
          Assert.IsFalse(MobileInputState.ConsumeReload());
      }

      [Test]
      public void SwapRequest_ReturnsSlotThenMinusOne() {
          MobileInputState.RequestSwap(2);
          Assert.AreEqual(2, MobileInputState.ConsumeSwap());
          Assert.AreEqual(-1, MobileInputState.ConsumeSwap());
      }

      [TestCase(-1)]
      [TestCase(4)]
      [TestCase(99)]
      public void SwapRequest_OutOfRange_IsIgnored(int slot) {
          MobileInputState.RequestSwap(slot);
          Assert.AreEqual(-1, MobileInputState.ConsumeSwap());
      }

      [Test]
      public void ResetAll_ClearsEverything() {
          MobileInputState.Move = new Vector2(1f, 0f);
          MobileInputState.AimStick = new Vector2(0f, 1f);
          MobileInputState.FireHeld = true;
          MobileInputState.RequestFireDown();
          MobileInputState.RequestReload();
          MobileInputState.RequestSwap(1);

          MobileInputState.ResetAll();

          Assert.AreEqual(Vector2.zero, MobileInputState.Move);
          Assert.AreEqual(Vector2.zero, MobileInputState.AimStick);
          Assert.IsFalse(MobileInputState.FireHeld);
          Assert.IsFalse(MobileInputState.ConsumeFireDown());
          Assert.IsFalse(MobileInputState.ConsumeReload());
          Assert.AreEqual(-1, MobileInputState.ConsumeSwap());
      }

      [Test]
      public void MobilePlatform_InEditor_DefaultsToNotMobile_AndFlagTogglesIt() {
          bool original = MobilePlatform.ForceMobileInEditor;
          try
          {
              MobilePlatform.ForceMobileInEditor = false;
              Assert.IsFalse(MobilePlatform.IsMobile, "에디터 기본값은 PC 입력이어야 한다");
              MobilePlatform.ForceMobileInEditor = true;
              Assert.IsTrue(MobilePlatform.IsMobile);
          }
          finally
          {
              MobilePlatform.ForceMobileInEditor = original;
          }
      }
  }
  ```

- [ ] **Step 4: 실패 확인**

  Unity MCP `refresh_unity` 후 `run_tests`(EditMode, 필터 `MobileInputStateTests`).
  Expected: 컴파일 오류(`MobileInputState`·`MobilePlatform` 없음).

- [ ] **Step 5: MobileInputState 구현**

  `Assets/Scripts/MobileCore/MobileInputState.cs`:

  ```csharp
  using UnityEngine;

  // 터치 UI가 값을 쓰고 PlayerInput이 읽는 정적 브리지. 게임 코드에 의존하지 않는다.
  // 일회성 요청(FireDown·Reload·Swap)은 Consume으로 읽으면 바로 초기화되어 다음 프레임까지 남지 않는다.
  public static class MobileInputState {
      public static Vector2 Move;      // 이동 스틱 출력(크기 1 이하, 데드존 적용 후)
      public static Vector2 AimStick;  // 쌍둥이 스틱 모드의 조준 스틱 출력(크기 1 이하, 데드존 적용 후)
      public static bool FireHeld;     // 오토 에임 모드의 발사 버튼을 누르고 있는 동안 true

      private static bool fireDownRequested;
      private static bool reloadRequested;
      private static int swapRequested = -1;

      public static void RequestFireDown() {
          fireDownRequested = true;
      }

      public static void RequestReload() {
          reloadRequested = true;
      }

      // 슬롯 인덱스 0~3(권총, 소총, SMG, 산탄총). 범위 밖 값은 무시한다
      public static void RequestSwap(int slotIndex) {
          if (slotIndex >= 0 && slotIndex <= 3)
          {
              swapRequested = slotIndex;
          }
      }

      public static bool ConsumeFireDown() {
          bool value = fireDownRequested;
          fireDownRequested = false;
          return value;
      }

      public static bool ConsumeReload() {
          bool value = reloadRequested;
          reloadRequested = false;
          return value;
      }

      // 요청이 없으면 -1
      public static int ConsumeSwap() {
          int value = swapRequested;
          swapRequested = -1;
          return value;
      }

      // 씬 전환·일시정지·포커스 변경·조준 모드 전환 때 모든 터치 입력 상태를 초기화한다
      public static void ResetAll() {
          Move = Vector2.zero;
          AimStick = Vector2.zero;
          FireHeld = false;
          fireDownRequested = false;
          reloadRequested = false;
          swapRequested = -1;
      }

      // 도메인 리로드를 끈 에디터 설정에서도 플레이 시작 때 상태가 남지 않게 한다
      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
      private static void ResetOnPlay() {
          ResetAll();
      }
  }
  ```

- [ ] **Step 6: MobilePlatform 구현**

  `Assets/Scripts/MobileCore/MobilePlatform.cs`:

  ```csharp
  using UnityEngine;

  // 모바일 입력·터치 UI 사용 여부 판단. 에디터에서만 강제 플래그를 허용하고 실제 빌드의 판정은 덮어쓰지 않는다
  public static class MobilePlatform {
  #if UNITY_EDITOR
      private const string ForceKey = "UrbanSurvival.ForceMobileInEditor";

      public static bool ForceMobileInEditor {
          get { return UnityEditor.EditorPrefs.GetBool(ForceKey, false); }
          set { UnityEditor.EditorPrefs.SetBool(ForceKey, value); }
      }

      public static bool IsMobile {
          get { return ForceMobileInEditor || Application.isMobilePlatform; }
      }
  #else
      public static bool IsMobile {
          get { return Application.isMobilePlatform; }
      }
  #endif
  }
  ```

  주의: 테스트의 `MobilePlatform.ForceMobileInEditor`는 `#if UNITY_EDITOR`에서만 존재한다. EditMode 테스트는 에디터에서만 돌아 문제 없다.

- [ ] **Step 7: 통과 확인**

  `refresh_unity` 후 `run_tests`(EditMode, 필터 `MobileInputStateTests`).
  Expected: 전부 PASS. 기존 `SpecDataTests`(14건)도 함께 돌려 PASS 확인.

- [ ] **Step 8: Commit**

  ```bash
  git add Assets/Scripts/MobileCore Assets/Game/Tests/EditMode/UrbanSurvival.EditMode.Tests.asmdef Assets/Game/Tests/EditMode/MobileInputStateTests.cs Assets/Game/Tests/EditMode/MobileInputStateTests.cs.meta Assets/Scripts/MobileCore.meta
  git commit -m "feat: 모바일 입력 브리지(MobileInputState)와 플랫폼 판정 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 2: JoystickMath (스틱 변환·화면→월드 방향)

**Files:**
- Create: `Assets/Scripts/MobileCore/JoystickMath.cs`
- Test: `Assets/Game/Tests/EditMode/JoystickMathTests.cs`

**Interfaces:**
- Produces:
  - `static Vector2 JoystickMath.Evaluate(Vector2 offset, float radius, float deadZone)` — 크기 1 이하. 데드존 이하이면 `Vector2.zero`, 이상이면 데드존을 뺀 값을 다시 0~1로 펴서 방향 유지
  - `static Vector3 JoystickMath.ToWorldDirection(Vector2 stick, Vector3 cameraForward, Vector3 cameraUp)` — 카메라 기준 평면(y=0) 방향. 입력 크기가 0이면 `Vector3.zero`. 결과는 정규화하지 않고 `PlayerMovement.GetMoveDirection`과 같은 방식(`right*x + forward*y`)

- [ ] **Step 1: 실패하는 테스트 작성**

  `Assets/Game/Tests/EditMode/JoystickMathTests.cs`:

  ```csharp
  using NUnit.Framework;
  using UnityEngine;

  public class JoystickMathTests {
      private const float Tolerance = 0.0001f;

      [Test]
      public void NoInput_ReturnsZero() {
          Assert.AreEqual(Vector2.zero, JoystickMath.Evaluate(Vector2.zero, 100f, 0.2f));
      }

      [Test]
      public void InsideDeadZone_IsIgnored() {
          Vector2 result = JoystickMath.Evaluate(new Vector2(15f, 0f), 100f, 0.2f);
          Assert.AreEqual(Vector2.zero, result);
      }

      [Test]
      public void AtMaxRadius_ReturnsUnitVector() {
          Vector2 result = JoystickMath.Evaluate(new Vector2(0f, 100f), 100f, 0.2f);
          Assert.AreEqual(0f, result.x, Tolerance);
          Assert.AreEqual(1f, result.y, Tolerance);
      }

      [Test]
      public void BeyondRadius_IsClampedToUnit() {
          Vector2 result = JoystickMath.Evaluate(new Vector2(300f, 400f), 100f, 0.2f);
          Assert.AreEqual(1f, result.magnitude, Tolerance);
      }

      [Test]
      public void Diagonal_MaxInput_MagnitudeNotAboveOne() {
          Vector2 result = JoystickMath.Evaluate(new Vector2(100f, 100f), 100f, 0.2f);
          Assert.LessOrEqual(result.magnitude, 1f + Tolerance);
          Assert.AreEqual(result.x, result.y, Tolerance);
      }

      [Test]
      public void OutsideDeadZone_KeepsDirection() {
          Vector2 result = JoystickMath.Evaluate(new Vector2(60f, 0f), 100f, 0.2f);
          Assert.Greater(result.x, 0f);
          Assert.AreEqual(0f, result.y, Tolerance);
          // (0.6 - 0.2) / (1 - 0.2) = 0.5
          Assert.AreEqual(0.5f, result.magnitude, Tolerance);
      }

      [Test]
      public void InvalidRadius_ReturnsZeroWithoutNaN() {
          Vector2 result = JoystickMath.Evaluate(new Vector2(10f, 10f), 0f, 0.2f);
          Assert.AreEqual(Vector2.zero, result);
      }

      [Test]
      public void DeadZoneAtOrAboveOne_IsClampedAndNeverDividesByZero() {
          Vector2 result = JoystickMath.Evaluate(new Vector2(100f, 0f), 100f, 1f);
          Assert.IsFalse(float.IsNaN(result.x) || float.IsNaN(result.y));
          Assert.LessOrEqual(result.magnitude, 1f + Tolerance);
      }

      [Test]
      public void ToWorld_TopDownCameraLookingNorth_MapsStickUpToForward() {
          Vector3 forward = JoystickMath.ToWorldDirection(new Vector2(0f, 1f), Vector3.forward, Vector3.up);
          Vector3 right = JoystickMath.ToWorldDirection(new Vector2(1f, 0f), Vector3.forward, Vector3.up);
          Assert.AreEqual(0f, forward.x, Tolerance);
          Assert.AreEqual(1f, forward.z, Tolerance);
          Assert.AreEqual(1f, right.x, Tolerance);
          Assert.AreEqual(0f, right.z, Tolerance);
      }

      [Test]
      public void ToWorld_VerticalCamera_UsesCameraUpAsForward() {
          // 수직 탑뷰 카메라: forward가 아래를 향해 평면 투영이 0이면 camera.up을 화면 위쪽으로 쓴다
          Vector3 result = JoystickMath.ToWorldDirection(new Vector2(0f, 1f), Vector3.down, Vector3.forward);
          Assert.AreEqual(1f, result.z, Tolerance);
          Assert.AreEqual(0f, result.y, Tolerance);
      }

      [Test]
      public void ToWorld_ZeroStick_ReturnsZeroVector() {
          Assert.AreEqual(Vector3.zero, JoystickMath.ToWorldDirection(Vector2.zero, Vector3.forward, Vector3.up));
      }
  }
  ```

- [ ] **Step 2: 실패 확인**

  `refresh_unity` 후 `run_tests`(필터 `JoystickMathTests`). Expected: 컴파일 오류.

- [ ] **Step 3: 구현**

  `Assets/Scripts/MobileCore/JoystickMath.cs`:

  ```csharp
  using UnityEngine;

  // 조이스틱 입력 변환(순수 함수). 위젯과 PlayerInput이 같은 규칙을 쓰도록 한 곳에 둔다
  public static class JoystickMath {
      // offset: 스틱 중심에서 손가락까지의 화면 거리(px), radius: 스틱 반지름(px)
      // 반환값은 크기 1 이하. 데드존 이하는 0, 그 위는 데드존만큼 빼고 0~1로 다시 편다
      public static Vector2 Evaluate(Vector2 offset, float radius, float deadZone) {
          if (radius <= 0f)
          {
              return Vector2.zero;
          }

          float dead = Mathf.Clamp(deadZone, 0f, 0.95f);
          Vector2 normalized = offset / radius;
          float magnitude = normalized.magnitude;
          if (magnitude <= dead || magnitude <= 0f)
          {
              return Vector2.zero;
          }

          float scaled = Mathf.Clamp01((magnitude - dead) / (1f - dead));
          return normalized / magnitude * scaled;
      }

      // 화면 기준 스틱 방향을 카메라 기준 평면(y=0) 월드 방향으로 바꾼다. PlayerMovement.GetMoveDirection과 같은 규칙
      public static Vector3 ToWorldDirection(Vector2 stick, Vector3 cameraForward, Vector3 cameraUp) {
          if (stick.sqrMagnitude <= 0f)
          {
              return Vector3.zero;
          }

          Vector3 forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
          if (forward.sqrMagnitude < 0.0001f)
          {
              forward = Vector3.ProjectOnPlane(cameraUp, Vector3.up);
          }
          if (forward.sqrMagnitude < 0.0001f)
          {
              forward = Vector3.forward;
          }
          forward.Normalize();

          Vector3 right = Vector3.Cross(Vector3.up, forward);
          return right * stick.x + forward * stick.y;
      }
  }
  ```

- [ ] **Step 4: 통과 확인**

  `run_tests`(필터 `JoystickMathTests`). Expected: 전부 PASS.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/MobileCore/JoystickMath.cs Assets/Scripts/MobileCore/JoystickMath.cs.meta Assets/Game/Tests/EditMode/JoystickMathTests.cs Assets/Game/Tests/EditMode/JoystickMathTests.cs.meta
  git commit -m "feat: 조이스틱 변환 로직(JoystickMath) 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 3: TwinStickFireTracker (조준 스틱 발사 엣지)

**Files:**
- Create: `Assets/Scripts/MobileCore/TwinStickFireTracker.cs`
- Test: `Assets/Game/Tests/EditMode/TwinStickFireTrackerTests.cs`

**Interfaces:**
- Consumes: `JoystickMath.Evaluate` (테스트에서 스틱 출력 생성)
- Produces: `sealed class TwinStickFireTracker` — `bool Held`, `bool Down`, `void Update(Vector2 stick)`(스틱 출력이 0이 아니면 눌림), `void Reset()`

- [ ] **Step 1: 실패하는 테스트 작성**

  `Assets/Game/Tests/EditMode/TwinStickFireTrackerTests.cs`:

  ```csharp
  using NUnit.Framework;
  using UnityEngine;

  public class TwinStickFireTrackerTests {
      private static Vector2 Stick(float x, float y) {
          return JoystickMath.Evaluate(new Vector2(x, y), 100f, 0.25f);
      }

      [Test]
      public void InsideDeadZone_DoesNotFire() {
          var tracker = new TwinStickFireTracker();
          tracker.Update(Stick(10f, 0f));
          Assert.IsFalse(tracker.Held);
          Assert.IsFalse(tracker.Down);
      }

      [Test]
      public void CrossingDeadZone_ProducesDownOnce_ThenHeld() {
          var tracker = new TwinStickFireTracker();
          tracker.Update(Stick(80f, 0f));
          Assert.IsTrue(tracker.Held);
          Assert.IsTrue(tracker.Down);

          tracker.Update(Stick(80f, 0f));
          Assert.IsTrue(tracker.Held);
          Assert.IsFalse(tracker.Down, "유지 중에는 새 FireDown이 없어야 한다");
      }

      [Test]
      public void ReleaseToCenter_StopsFire_AndNextPullIsNewDown() {
          var tracker = new TwinStickFireTracker();
          tracker.Update(Stick(80f, 0f));
          tracker.Update(Vector2.zero);
          Assert.IsFalse(tracker.Held);
          Assert.IsFalse(tracker.Down);

          tracker.Update(Stick(0f, 80f));
          Assert.IsTrue(tracker.Down);
      }

      [Test]
      public void ChangingDirectionWhileHeld_DoesNotCreateNewDown() {
          var tracker = new TwinStickFireTracker();
          tracker.Update(Stick(80f, 0f));
          tracker.Update(Stick(0f, 80f));
          Assert.IsTrue(tracker.Held);
          Assert.IsFalse(tracker.Down);
      }

      [Test]
      public void Reset_ClearsState_SoHeldStickIsNewPull() {
          var tracker = new TwinStickFireTracker();
          tracker.Update(Stick(80f, 0f));
          tracker.Reset();
          Assert.IsFalse(tracker.Held);
          Assert.IsFalse(tracker.Down);
      }
  }
  ```

- [ ] **Step 2: 실패 확인**

  `refresh_unity` 후 `run_tests`(필터 `TwinStickFireTrackerTests`). Expected: 컴파일 오류.

- [ ] **Step 3: 구현**

  `Assets/Scripts/MobileCore/TwinStickFireTracker.cs`:

  ```csharp
  using UnityEngine;

  // 쌍둥이 스틱 모드: 조준 스틱이 데드존 밖에 있는 동안 발사 유지, 데드존 밖으로 처음 나간 프레임에 FireDown
  // 입력은 JoystickMath.Evaluate의 출력(데드존 이하는 0)이다
  public sealed class TwinStickFireTracker {
      private const float PullThresholdSqr = 0.0001f;

      private bool wasHeld;

      public bool Held { get; private set; }
      public bool Down { get; private set; }

      public void Update(Vector2 stick) {
          bool pulled = stick.sqrMagnitude > PullThresholdSqr;
          Down = pulled && !wasHeld;
          Held = pulled;
          wasHeld = pulled;
      }

      public void Reset() {
          Held = false;
          Down = false;
          wasHeld = false;
      }
  }
  ```

- [ ] **Step 4: 통과 확인**

  `run_tests`(필터 `TwinStickFireTrackerTests`). Expected: 전부 PASS.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/MobileCore/TwinStickFireTracker.cs Assets/Scripts/MobileCore/TwinStickFireTracker.cs.meta Assets/Game/Tests/EditMode/TwinStickFireTrackerTests.cs Assets/Game/Tests/EditMode/TwinStickFireTrackerTests.cs.meta
  git commit -m "feat: 쌍둥이 스틱 발사 엣지 판정(TwinStickFireTracker) 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 4: AutoAimTargeting (오토 에임 대상 선택)

**Files:**
- Create: `Assets/Scripts/MobileCore/AutoAimTargeting.cs`
- Test: `Assets/Game/Tests/EditMode/AutoAimTargetingTests.cs`

**Interfaces:**
- Produces:
  - `struct AimCandidate { public Vector3 position; public bool alive; public AimCandidate(Vector3 position, bool alive) }`
  - `struct AimResult { public bool valid; public bool hasTarget; public Vector3 direction; }` — `valid`가 false이면 쓸 방향이 없음, `hasTarget`은 적을 골랐는지, `direction`은 y=0 정규화 방향
  - `static AimResult AutoAimTargeting.Select(Vector3 origin, IReadOnlyList<AimCandidate> candidates, float range, Vector3 moveDirection, Vector3 lastDirection)`

- [ ] **Step 1: 실패하는 테스트 작성**

  `Assets/Game/Tests/EditMode/AutoAimTargetingTests.cs`:

  ```csharp
  using System.Collections.Generic;
  using NUnit.Framework;
  using UnityEngine;

  public class AutoAimTargetingTests {
      private const float Tolerance = 0.0001f;
      private static readonly Vector3 Origin = Vector3.zero;

      private static List<AimCandidate> List(params AimCandidate[] items) {
          return new List<AimCandidate>(items);
      }

      [Test]
      public void PicksNearestAliveEnemyInRange() {
          var enemies = List(
              new AimCandidate(new Vector3(10f, 0f, 0f), true),
              new AimCandidate(new Vector3(0f, 0f, 4f), true));

          AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, Vector3.zero, Vector3.forward);

          Assert.IsTrue(result.valid);
          Assert.IsTrue(result.hasTarget);
          Assert.AreEqual(0f, result.direction.x, Tolerance);
          Assert.AreEqual(1f, result.direction.z, Tolerance);
      }

      [Test]
      public void IgnoresEnemiesOutsideRange() {
          var enemies = List(new AimCandidate(new Vector3(20f, 0f, 0f), true));

          AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, new Vector3(0f, 0f, 1f), Vector3.right);

          Assert.IsFalse(result.hasTarget);
          Assert.AreEqual(1f, result.direction.z, Tolerance, "대상이 없으면 이동 방향을 쓴다");
      }

      [Test]
      public void EnemyExactlyAtRange_IsATarget() {
          var enemies = List(new AimCandidate(new Vector3(15f, 0f, 0f), true));
          AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, Vector3.zero, Vector3.forward);
          Assert.IsTrue(result.hasTarget);
      }

      [Test]
      public void ExcludesDeadEnemies() {
          var enemies = List(
              new AimCandidate(new Vector3(0f, 0f, 2f), false),
              new AimCandidate(new Vector3(8f, 0f, 0f), true));

          AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, Vector3.zero, Vector3.forward);

          Assert.IsTrue(result.hasTarget);
          Assert.AreEqual(1f, result.direction.x, Tolerance);
      }

      [Test]
      public void NoTarget_UsesMoveDirection() {
          AimResult result = AutoAimTargeting.Select(Origin, List(), 15f, new Vector3(-3f, 0f, 0f), Vector3.forward);
          Assert.IsTrue(result.valid);
          Assert.IsFalse(result.hasTarget);
          Assert.AreEqual(-1f, result.direction.x, Tolerance, "이동 방향은 정규화된다");
      }

      [Test]
      public void NoTargetAndNoMove_KeepsLastDirection() {
          AimResult result = AutoAimTargeting.Select(Origin, List(), 15f, Vector3.zero, Vector3.right);
          Assert.IsTrue(result.valid);
          Assert.AreEqual(1f, result.direction.x, Tolerance);
      }

      [Test]
      public void NothingAvailable_IsInvalid() {
          AimResult result = AutoAimTargeting.Select(Origin, List(), 15f, Vector3.zero, Vector3.zero);
          Assert.IsFalse(result.valid);
      }

      [Test]
      public void EnemyAtSamePosition_IsSkippedWithoutNaN() {
          var enemies = List(
              new AimCandidate(Origin, true),
              new AimCandidate(new Vector3(0f, 0f, 6f), true));

          AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, Vector3.zero, Vector3.right);

          Assert.IsTrue(result.hasTarget);
          Assert.IsFalse(float.IsNaN(result.direction.x));
          Assert.AreEqual(1f, result.direction.z, Tolerance, "겹친 적은 방향을 만들 수 없어 건너뛴다");
      }

      [Test]
      public void ZeroOrNegativeRange_HasNoTarget() {
          var enemies = List(new AimCandidate(new Vector3(0f, 0f, 1f), true));
          AimResult result = AutoAimTargeting.Select(Origin, enemies, 0f, Vector3.zero, Vector3.right);
          Assert.IsFalse(result.hasTarget);
      }

      [Test]
      public void DistanceIgnoresHeight() {
          var enemies = List(new AimCandidate(new Vector3(0f, 5f, 10f), true));
          AimResult result = AutoAimTargeting.Select(Origin, enemies, 10.5f, Vector3.zero, Vector3.right);
          Assert.IsTrue(result.hasTarget, "수평 거리 10 이므로 사거리 안");
          Assert.AreEqual(0f, result.direction.y, Tolerance);
      }

      [Test]
      public void FiveHundredCandidates_PicksNearestQuickly() {
          var enemies = new List<AimCandidate>();
          for (int i = 0; i < 500; i++)
          {
              enemies.Add(new AimCandidate(new Vector3(10f + i * 0.01f, 0f, 0f), true));
          }
          enemies.Add(new AimCandidate(new Vector3(0f, 0f, 3f), true));

          AimResult result = AutoAimTargeting.Select(Origin, enemies, 30f, Vector3.zero, Vector3.right);

          Assert.AreEqual(1f, result.direction.z, Tolerance);
      }
  }
  ```

- [ ] **Step 2: 실패 확인**

  `refresh_unity` 후 `run_tests`(필터 `AutoAimTargetingTests`). Expected: 컴파일 오류.

- [ ] **Step 3: 구현**

  `Assets/Scripts/MobileCore/AutoAimTargeting.cs`:

  ```csharp
  using System.Collections.Generic;
  using UnityEngine;

  public struct AimCandidate {
      public Vector3 position;
      public bool alive;

      public AimCandidate(Vector3 position, bool alive) {
          this.position = position;
          this.alive = alive;
      }
  }

  public struct AimResult {
      public bool valid;      // 쓸 수 있는 방향이 있는지
      public bool hasTarget;  // 사거리 안의 적을 골랐는지
      public Vector3 direction; // y=0으로 정규화된 방향
  }

  // 오토 에임 대상 선택(순수 함수). 방향만 정하고 명중·산포·사거리·탄약 판정은 건드리지 않는다
  public static class AutoAimTargeting {
      private const float MinSqrDistance = 0.000001f;

      // 우선순위: 1) 사거리 안 가장 가까운 살아 있는 적 2) 이동 방향 3) 마지막 유효 방향
      // 거리는 수평(XZ)으로 계산하며 사거리와 같은 거리의 적도 대상이다. range가 0 이하이면 대상이 없다
      public static AimResult Select(Vector3 origin, IReadOnlyList<AimCandidate> candidates, float range, Vector3 moveDirection, Vector3 lastDirection) {
          if (range > 0f && candidates != null)
          {
              float rangeSqr = range * range;
              float bestSqr = float.MaxValue;
              Vector3 best = Vector3.zero;
              bool found = false;

              for (int i = 0; i < candidates.Count; i++)
              {
                  AimCandidate candidate = candidates[i];
                  if (!candidate.alive)
                  {
                      continue;
                  }

                  Vector3 offset = candidate.position - origin;
                  offset.y = 0f;
                  float sqr = offset.sqrMagnitude;
                  if (sqr < MinSqrDistance || sqr > rangeSqr || sqr >= bestSqr)
                  {
                      continue;
                  }

                  bestSqr = sqr;
                  best = offset;
                  found = true;
              }

              if (found)
              {
                  return new AimResult { valid = true, hasTarget = true, direction = best.normalized };
              }
          }

          Vector3 move = Flatten(moveDirection);
          if (move.sqrMagnitude > MinSqrDistance)
          {
              return new AimResult { valid = true, hasTarget = false, direction = move.normalized };
          }

          Vector3 last = Flatten(lastDirection);
          if (last.sqrMagnitude > MinSqrDistance)
          {
              return new AimResult { valid = true, hasTarget = false, direction = last.normalized };
          }

          return new AimResult { valid = false, hasTarget = false, direction = Vector3.zero };
      }

      private static Vector3 Flatten(Vector3 direction) {
          direction.y = 0f;
          return direction;
      }
  }
  ```

- [ ] **Step 4: 통과 확인**

  `run_tests`(필터 `AutoAimTargetingTests`). Expected: 전부 PASS.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/MobileCore/AutoAimTargeting.cs Assets/Scripts/MobileCore/AutoAimTargeting.cs.meta Assets/Game/Tests/EditMode/AutoAimTargetingTests.cs Assets/Game/Tests/EditMode/AutoAimTargetingTests.cs.meta
  git commit -m "feat: 오토 에임 대상 선택 로직(AutoAimTargeting) 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 5: MobileAimSettings (조준 모드 저장)

**Files:**
- Create: `Assets/Scripts/MobileCore/MobileAimMode.cs`
- Test: `Assets/Game/Tests/EditMode/MobileAimSettingsTests.cs`

**Interfaces:**
- Produces:
  - `enum MobileAimMode { AutoAim = 0, TwinStick = 1 }`
  - `static class MobileAimSettings` — `string PrefsKey`(`"MobileAimMode"`), `MobileAimMode Mode { get; set; }`(기본 `AutoAim`, 설정 시 PlayerPrefs 저장 후 `Changed` 발생, 값이 같으면 발생하지 않음), `event Action<MobileAimMode> Changed`

- [ ] **Step 1: 실패하는 테스트 작성**

  `Assets/Game/Tests/EditMode/MobileAimSettingsTests.cs`:

  ```csharp
  using NUnit.Framework;
  using UnityEngine;

  public class MobileAimSettingsTests {
      private bool hadKey;
      private int originalValue;

      [SetUp]
      public void SetUp() {
          hadKey = PlayerPrefs.HasKey(MobileAimSettings.PrefsKey);
          originalValue = PlayerPrefs.GetInt(MobileAimSettings.PrefsKey, 0);
          PlayerPrefs.DeleteKey(MobileAimSettings.PrefsKey);
      }

      [TearDown]
      public void TearDown() {
          if (hadKey)
          {
              PlayerPrefs.SetInt(MobileAimSettings.PrefsKey, originalValue);
          }
          else
          {
              PlayerPrefs.DeleteKey(MobileAimSettings.PrefsKey);
          }
      }

      [Test]
      public void Default_IsAutoAim() {
          Assert.AreEqual(MobileAimMode.AutoAim, MobileAimSettings.Mode);
      }

      [Test]
      public void SetMode_PersistsAcrossReads() {
          MobileAimSettings.Mode = MobileAimMode.TwinStick;
          Assert.AreEqual(1, PlayerPrefs.GetInt(MobileAimSettings.PrefsKey, -1));
          Assert.AreEqual(MobileAimMode.TwinStick, MobileAimSettings.Mode);
      }

      [Test]
      public void InvalidStoredValue_FallsBackToAutoAim() {
          PlayerPrefs.SetInt(MobileAimSettings.PrefsKey, 99);
          Assert.AreEqual(MobileAimMode.AutoAim, MobileAimSettings.Mode);
      }

      [Test]
      public void Changed_FiresOnlyWhenValueChanges() {
          int count = 0;
          MobileAimMode last = MobileAimMode.AutoAim;
          System.Action<MobileAimMode> handler = m => { count++; last = m; };
          MobileAimSettings.Changed += handler;
          try
          {
              MobileAimSettings.Mode = MobileAimMode.AutoAim; // 같은 값
              Assert.AreEqual(0, count);
              MobileAimSettings.Mode = MobileAimMode.TwinStick;
              Assert.AreEqual(1, count);
              Assert.AreEqual(MobileAimMode.TwinStick, last);
          }
          finally
          {
              MobileAimSettings.Changed -= handler;
          }
      }
  }
  ```

- [ ] **Step 2: 실패 확인**

  `refresh_unity` 후 `run_tests`(필터 `MobileAimSettingsTests`). Expected: 컴파일 오류.

- [ ] **Step 3: 구현**

  `Assets/Scripts/MobileCore/MobileAimMode.cs`:

  ```csharp
  using System;
  using UnityEngine;

  public enum MobileAimMode {
      AutoAim = 0,   // 가장 가까운 적 자동 조준 + 발사 버튼(기본값)
      TwinStick = 1  // 오른쪽 스틱으로 조준·발사
  }

  // 모바일 조준 모드 저장·복원(PlayerPrefs). 설정 창 연결은 모듈 밖의 MobileAimModeSettingsBinder가 맡는다
  public static class MobileAimSettings {
      public const string PrefsKey = "MobileAimMode";

      public static event Action<MobileAimMode> Changed;

      public static MobileAimMode Mode {
          get {
              int stored = PlayerPrefs.GetInt(PrefsKey, (int)MobileAimMode.AutoAim);
              return Enum.IsDefined(typeof(MobileAimMode), stored) ? (MobileAimMode)stored : MobileAimMode.AutoAim;
          }
          set {
              if (Mode == value)
              {
                  return;
              }

              PlayerPrefs.SetInt(PrefsKey, (int)value);
              PlayerPrefs.Save();
              Changed?.Invoke(value);
          }
      }
  }
  ```

- [ ] **Step 4: 통과 확인**

  `run_tests`(필터 `MobileAimSettingsTests`). Expected: 전부 PASS.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/MobileCore/MobileAimMode.cs Assets/Scripts/MobileCore/MobileAimMode.cs.meta Assets/Game/Tests/EditMode/MobileAimSettingsTests.cs Assets/Game/Tests/EditMode/MobileAimSettingsTests.cs.meta
  git commit -m "feat: 모바일 조준 모드 저장(MobileAimSettings) 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 6: Zombie.alive 목록과 PlayerShooter 읽기 접근자

**Files:**
- Modify: `Assets/Scripts/Zombie.cs:27`(목록 선언), `:107-111`(Setup), `:114-116`(OnDestroy), `:238-246`(Die)
- Modify: `Assets/Scripts/PlayerShooter.cs:37-38` 근처(접근자)

**Interfaces:**
- Produces:
  - `public static readonly List<Zombie> Zombie.alive` — 살아 있는 모든 좀비(보스 포함, 사망·파괴 시 제거)
  - `public int PlayerShooter.CurrentSlotIndex` (읽기 전용), `public bool PlayerShooter.IsSlotUnlocked(int slotIndex)` (범위 밖이면 false, 보유하고 총 오브젝트가 있으면 true)

- [ ] **Step 1: Zombie에 alive 목록 추가**

  `Zombie.cs`의 `bosses` 선언 바로 아래(27행 다음)에 추가:

  ```csharp
  public static readonly System.Collections.Generic.List<Zombie> alive = new System.Collections.Generic.List<Zombie>(); // 살아있는 좀비 전체 목록(모바일 오토 에임 대상 탐색용)
  ```

  `Setup(ZombieData, float)` 안, `this.zombieData = zombieData;` 바로 다음 줄에 추가:

  ```csharp
          if (!alive.Contains(this))
          {
              alive.Add(this);
          }
  ```

  `OnDestroy()`를 다음으로 바꾼다:

  ```csharp
      private void OnDestroy() {
          bosses.Remove(this);
          alive.Remove(this);
      }
  ```

  `Die()`에서 `base.Die();` 바로 다음 줄에 추가:

  ```csharp
          alive.Remove(this);
  ```

  주의: `Setup` 호출 경로를 확인한다. `Grep "Setup(" Assets/Scripts/ZombieSpawner.cs`로 `CreateZombie`가 `Setup`을 호출하는지 본다. 호출하지 않는 경로가 있으면 그 경로에서도 등록되게 `OnEnable`로 옮긴다.

- [ ] **Step 2: PlayerShooter 접근자 추가**

  `PlayerShooter.cs`의 `private int currentSlot;` 줄 아래에 추가:

  ```csharp
      public int CurrentSlotIndex => currentSlot; // 현재 장착 슬롯(모바일 슬롯 UI 표시용, 읽기 전용)

      // 슬롯을 보유하고 무기 오브젝트가 있는지(모바일 슬롯 UI 활성 표시용, 읽기 전용)
      public bool IsSlotUnlocked(int slotIndex) {
          return slotIndex >= 0 && slotIndex < SlotCount && unlocked[slotIndex] && guns[slotIndex] != null;
      }
  ```

- [ ] **Step 3: 컴파일 확인**

  Unity MCP `refresh_unity` 후 `read_console`(에러만).
  Expected: 컴파일 에러 없음.

- [ ] **Step 4: 에디터 플레이로 alive 목록 확인**

  `UrbanSurvival.unity`를 열고 플레이 후 10초 뒤 `execute_code`:

  ```csharp
  return "alive=" + Zombie.alive.Count + ", scene=" + Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None).Length;
  ```

  Expected: `alive`가 씬의 살아 있는 좀비 수와 같다(시체는 `alive`에 없으므로 씬 개수 ≥ alive). 좀비를 몇 마리 죽인 뒤 다시 실행해 `alive`가 줄었는지 확인한다.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/Zombie.cs Assets/Scripts/PlayerShooter.cs
  git commit -m "feat: 살아있는 좀비 목록(Zombie.alive)과 무기 슬롯 읽기 접근자 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 7: PlayerInput 모바일 경로와 PlayerMovement 조준 방향

**Files:**
- Modify: `Assets/Scripts/PlayerInput.cs` (전체 구조 재배치, PC 경로 동작 유지)
- Modify: `Assets/Scripts/PlayerMovement.cs:78-102`(`GetAimDirection` 시작부)
- Create: `Assets/Editor/MobileEditorMenu.cs`

**Interfaces:**
- Consumes: `MobileInputState`, `MobilePlatform`, `MobileAimSettings`, `JoystickMath`, `TwinStickFireTracker`, `AutoAimTargeting`/`AimCandidate`, `Zombie.alive`, `PlayerShooter.gun.gunData.range`
- Produces: `PlayerInput.aimWorldDirection`(Vector3, y=0 정규화), `PlayerInput.hasAimWorldDirection`(bool), `PlayerInput.RequireFireRelease()`(다음 발사를 한 번 뗀 뒤로 미룸)

- [ ] **Step 1: 에디터 강제 모바일 메뉴 만들기**

  `Assets/Editor/MobileEditorMenu.cs`:

  ```csharp
  using UnityEditor;

  // 에디터에서 모바일 입력·터치 UI를 강제로 켜고 끄는 메뉴(실제 빌드의 모바일 판정에는 영향이 없다)
  public static class MobileEditorMenu {
      private const string MenuPath = "Urban Survival/Mobile/Force Mobile Input In Editor";

      [MenuItem(MenuPath)]
      private static void Toggle() {
          MobilePlatform.ForceMobileInEditor = !MobilePlatform.ForceMobileInEditor;
      }

      [MenuItem(MenuPath, true)]
      private static bool ToggleValidate() {
          Menu.SetChecked(MenuPath, MobilePlatform.ForceMobileInEditor);
          return true;
      }
  }
  ```

  `Assets/Editor` 폴더가 이미 어셈블리(`Assembly-CSharp-Editor`)에 속한다. `MobilePlatform`은 `UrbanSurvival.MobileCore`(자동 참조)에 있어 접근 가능하다.

- [ ] **Step 2: PlayerInput을 다음 전체 내용으로 교체**

  기존 PC 경로의 동작 순서(입력 읽기 → 오발 차단 → 포인터 차단)를 유지한다. 모바일 경로만 추가한다.

  ```csharp
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.EventSystems;
  using UnityEngine.UI;

  // 플레이어 캐릭터를 조작하기 위한 사용자 입력을 감지
  // 감지된 입력값을 다른 컴포넌트들이 사용할 수 있도록 제공
  // PC는 키보드·마우스, 모바일(MobilePlatform.IsMobile)은 터치 UI가 쓰는 MobileInputState를 읽어 같은 출력 값을 낸다
  public class PlayerInput : MonoBehaviour {
      public string moveAxisName = "Vertical"; // 앞뒤 움직임을 위한 입력축 이름
      public string rotateAxisName = "Horizontal"; // 좌우 회전을 위한 입력축 이름
      public string fireButtonName = "Fire1"; // 발사를 위한 입력 버튼 이름
      public string reloadButtonName = "Reload"; // 재장전을 위한 입력 버튼 이름

      // 값 할당은 내부에서만 가능
      public float move { get; private set; } // 감지된 움직임 입력값
      public float rotate { get; private set; } // 감지된 회전 입력값
      public bool fire { get; private set; } // 감지된 발사 입력값(누르고 있는 동안 true, 연사용)
      public bool fireDown { get; private set; } // 발사 버튼을 누른 첫 프레임(단발/엣지 판정용)
      public bool reload { get; private set; } // 감지된 재장전 입력값
      public Vector2 aimPosition { get; private set; } // 감지된 마우스 조준 화면 좌표

      public bool swapToSlot1 { get; private set; } // 1번 슬롯(권총) 스왑 입력
      public bool swapToSlot2 { get; private set; } // 2번 슬롯(소총) 스왑 입력
      public bool swapToSlot3 { get; private set; } // 3번 슬롯(SMG) 스왑 입력
      public bool swapToSlot4 { get; private set; } // 4번 슬롯(산탄총) 스왑 입력

      // 모바일 조준: 유효한 월드 방향(y=0, 정규화)이 있으면 PlayerMovement가 마우스 Ray 투영 대신 이 방향을 쓴다
      public Vector3 aimWorldDirection { get; private set; }
      public bool hasAimWorldDirection { get; private set; }

      private const float AutoAimRefreshInterval = 0.1f; // 오토 에임 대상 재탐색 간격(초)

      // 일시정지·게임오버·창 포커스 변경 직후에는 누르고 있던 발사 버튼이 다시 눌린 것으로 처리되지 않도록,
      // 발사 버튼을 한 번 뗄 때까지 발사 입력을 막는다(UI 버튼 클릭이나 창 클릭으로 돌아온 클릭이 오발이 되는 것을 방지)
      private bool suppressFireUntilRelease;
      private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

      private readonly TwinStickFireTracker twinStickFire = new TwinStickFireTracker();
      private readonly List<AimCandidate> aimCandidates = new List<AimCandidate>(512);
      private PlayerShooter playerShooter;
      private Vector3 lastMobileAim;
      private float nextAutoAimTime;

      private void Start() {
          playerShooter = GetComponent<PlayerShooter>();
          lastMobileAim = transform.forward;
          lastMobileAim.y = 0f;
          lastMobileAim = lastMobileAim.sqrMagnitude > 0.0001f ? lastMobileAim.normalized : Vector3.forward;
      }

      private void OnApplicationFocus(bool hasFocus) {
          suppressFireUntilRelease = true;
          MobileInputState.ResetAll();
          twinStickFire.Reset();
      }

      private void OnApplicationPause(bool paused) {
          suppressFireUntilRelease = true;
          MobileInputState.ResetAll();
          twinStickFire.Reset();
      }

      // 조준 모드 전환 등으로 눌려 있던 발사 입력이 그대로 발사로 이어지지 않도록, 다음 발사를 한 번 뗀 뒤로 미룬다
      public void RequireFireRelease() {
          suppressFireUntilRelease = true;
          MobileInputState.ResetAll();
          twinStickFire.Reset();
      }

      // 마우스 포인터가 게임 화면 밖이거나, 클릭 가능한 UI(버튼 등) 위에 있는지 확인
      private bool IsPointerBlockedForFire() {
          Vector3 pointer = Input.mousePosition;
          if (pointer.x < 0f || pointer.y < 0f || pointer.x > Screen.width || pointer.y > Screen.height)
          {
              return true;
          }

          if (EventSystem.current == null)
          {
              return false;
          }

          // 클릭할 수 없는 HUD 패널 위에서는 사격이 막히지 않도록, 버튼 같은 Selectable UI만 검사한다
          PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = pointer };
          uiRaycastResults.Clear();
          EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
          for (int i = 0; i < uiRaycastResults.Count; i++)
          {
              if (uiRaycastResults[i].gameObject.GetComponentInParent<Selectable>() != null)
              {
                  return true;
              }
          }

          return false;
      }

      // 매프레임 사용자 입력을 감지
      private void Update() {
          // 게임오버·일시정지 상태에서는 사용자 입력을 감지하지 않는다
          // (일시정지는 Time.timeScale=0이라 발사 쿨타임 등은 자연히 멈추지만, Update() 자체는 계속 돌기 때문에
          // 이 가드가 없으면 일시정지 중에도 새로 누른 입력이 그대로 통과해 총이 나가는 문제가 있었음)
          if (GameManager.instance && (GameManager.instance.isGameover || GameManager.instance.isPaused))
          {
              move = 0;
              rotate = 0;
              fire = false;
              fireDown = false;
              reload = false;
              swapToSlot1 = false;
              swapToSlot2 = false;
              swapToSlot3 = false;
              swapToSlot4 = false;
              hasAimWorldDirection = false;
              MobileInputState.ResetAll();
              twinStickFire.Reset();
              // 정지 중에 누른 버튼(예: 계속하기 클릭)이 재개 직후 발사로 이어지지 않도록 막는다
              suppressFireUntilRelease = true;
              return;
          }

          bool mobile = MobilePlatform.IsMobile;
          if (mobile)
          {
              ReadMobileInput();
          }
          else
          {
              hasAimWorldDirection = false;
              ReadDesktopInput();
          }

          // 발사 차단: 정지·포커스 복귀 직후 누른 채로 남은 버튼, 화면 밖 포인터, 클릭 가능한 UI 위 포인터
          if (suppressFireUntilRelease)
          {
              if (!fire)
              {
                  suppressFireUntilRelease = false;
              }
              else
              {
                  fire = false;
                  fireDown = false;
              }
          }

          // 마우스 포인터 검사는 PC 전용(터치의 발사 버튼은 그 자체가 UI라 이 검사를 거치면 항상 막힌다)
          if (!mobile && (fire || fireDown) && IsPointerBlockedForFire())
          {
              fire = false;
              fireDown = false;
          }
      }

      // PC: 키보드·마우스(기존 동작 그대로)
      private void ReadDesktopInput() {
          // move에 관한 입력 감지
          move = Input.GetAxis(moveAxisName);
          // rotate에 관한 입력 감지
          rotate = Input.GetAxis(rotateAxisName);
          // fire에 관한 입력 감지
          fire = Input.GetButton(fireButtonName);
          fireDown = Input.GetButtonDown(fireButtonName);

          // reload에 관한 입력 감지
          reload = Input.GetButtonDown(reloadButtonName);
          // 마우스 조준 위치 감지(이동과 독립적으로 처리)
          aimPosition = Input.mousePosition;

          // 무기 슬롯 스왑 입력(1=권총, 2=소총, 3=SMG, 4=산탄총)
          swapToSlot1 = Input.GetKeyDown(KeyCode.Alpha1);
          swapToSlot2 = Input.GetKeyDown(KeyCode.Alpha2);
          swapToSlot3 = Input.GetKeyDown(KeyCode.Alpha3);
          swapToSlot4 = Input.GetKeyDown(KeyCode.Alpha4);
      }

      // 모바일: 터치 UI가 쓴 MobileInputState를 읽는다(마우스 좌표는 쓰지 않는다)
      private void ReadMobileInput() {
          Vector2 moveStick = MobileInputState.Move;
          rotate = moveStick.x;
          move = moveStick.y;
          aimPosition = Vector2.zero;

          reload = MobileInputState.ConsumeReload();
          int slot = MobileInputState.ConsumeSwap();
          swapToSlot1 = slot == 0;
          swapToSlot2 = slot == 1;
          swapToSlot3 = slot == 2;
          swapToSlot4 = slot == 3;

          Camera cam = Camera.main;
          Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
          Vector3 camUp = cam != null ? cam.transform.up : Vector3.up;

          if (MobileAimSettings.Mode == MobileAimMode.TwinStick)
          {
              Vector2 aimStick = MobileInputState.AimStick;
              twinStickFire.Update(aimStick);
              fire = twinStickFire.Held;
              fireDown = twinStickFire.Down;
              MobileInputState.ConsumeFireDown(); // 이 모드에서는 발사 버튼 요청을 쓰지 않는다

              if (twinStickFire.Held)
              {
                  Vector3 direction = JoystickMath.ToWorldDirection(aimStick, camForward, camUp);
                  direction.y = 0f;
                  if (direction.sqrMagnitude > 0.0001f)
                  {
                      lastMobileAim = direction.normalized;
                  }
              }
          }
          else
          {
              twinStickFire.Reset();
              bool pressed = MobileInputState.ConsumeFireDown();
              fire = MobileInputState.FireHeld || pressed;
              fireDown = pressed;

              // 발사 버튼을 누르는 동안만 대상을 찾는다(버튼을 떼면 마지막 방향 유지)
              if (fire && (pressed || Time.time >= nextAutoAimTime))
              {
                  nextAutoAimTime = Time.time + AutoAimRefreshInterval;
                  lastMobileAim = ResolveAutoAim(moveStick, camForward, camUp);
              }
          }

          aimWorldDirection = lastMobileAim;
          hasAimWorldDirection = true;
      }

      // 사거리 안 가장 가까운 적 → 이동 방향 → 마지막 방향 순으로 조준 방향을 정한다
      private Vector3 ResolveAutoAim(Vector2 moveStick, Vector3 camForward, Vector3 camUp) {
          aimCandidates.Clear();
          for (int i = 0; i < Zombie.alive.Count; i++)
          {
              Zombie zombie = Zombie.alive[i];
              if (zombie != null)
              {
                  aimCandidates.Add(new AimCandidate(zombie.transform.position, !zombie.dead));
              }
          }

          float range = 0f;
          if (playerShooter != null && playerShooter.gun != null && playerShooter.gun.gunData != null)
          {
              range = playerShooter.gun.gunData.range;
          }

          Vector3 moveDirection = JoystickMath.ToWorldDirection(moveStick, camForward, camUp);
          AimResult result = AutoAimTargeting.Select(transform.position, aimCandidates, range, moveDirection, lastMobileAim);
          return result.valid ? result.direction : lastMobileAim;
      }
  }
  ```

  `LivingEntity.dead`는 `public bool dead { get; protected set; }`이라 외부에서 읽을 수 있다(확인 완료).

- [ ] **Step 3: PlayerMovement가 모바일 조준 방향을 우선 쓰게 수정**

  `GetAimDirection()`의 맨 앞(`Camera cam = Camera.main;` 이전)에 추가한다.

  ```csharp
          // 모바일: 터치 입력이 정한 월드 조준 방향이 있으면 마우스 Ray 투영 대신 사용
          if (playerInput.hasAimWorldDirection)
          {
              lastAimDirection = playerInput.aimWorldDirection;
              return lastAimDirection;
          }

  ```

  메서드 위 주석도 한 줄 갱신한다: `// 마우스 스크린 좌표를 ... (모바일은 PlayerInput.aimWorldDirection 우선)`.

- [ ] **Step 4: 컴파일 확인**

  `refresh_unity` 후 `read_console`(에러만). Expected: 에러 없음.

- [ ] **Step 5: PC 회귀 확인(강제 모바일 끔)**

  `Urban Survival/Mobile/Force Mobile Input In Editor` 체크 해제 상태에서 `UrbanSurvival.unity` 플레이. Task 0 Step 4와 같은 항목(WASD, 마우스 조준, 좌클릭 발사, R, 1~4, Esc)이 같게 동작하는지 확인한다. 이상이 있으면 다음 Step으로 가지 않고 수정한다.

- [ ] **Step 6: 모바일 경로 확인(강제 모바일 켬, 오토 에임)**

  메뉴에서 강제 모바일을 체크하고 플레이한 뒤 `execute_code`로 입력을 주입하고 결과를 읽는다.

  ```csharp
  // 1) 이동 스틱 위 입력
  MobileInputState.Move = new Vector2(0f, 1f);
  var pi = Object.FindFirstObjectByType<PlayerInput>();
  return "move=" + pi.move + " rotate=" + pi.rotate + " hasAim=" + pi.hasAimWorldDirection;
  ```

  Expected(다음 프레임 후): `move=1`. 이어서 `MobileInputState.FireHeld = true; MobileInputState.RequestFireDown();`를 실행하고 한 프레임 뒤 `pi.fire`, `pi.fireDown`, `pi.aimWorldDirection`을 읽는다. 좀비가 사거리 안에 있으면 그 방향, 없으면 이동 방향이어야 한다. `MobileInputState.FireHeld = false` 후 `pi.fire == false`.

  쌍둥이 스틱: `MobileAimSettings.Mode = MobileAimMode.TwinStick; MobileInputState.AimStick = new Vector2(1f, 0f);` → `pi.fire == true`, `pi.fireDown`은 첫 프레임만 true, `aimWorldDirection`이 +X 계열 방향이어야 한다. 이후 `MobileInputState.AimStick = Vector2.zero` → `fire == false`. 확인을 마치면 `MobileAimSettings.Mode = MobileAimMode.AutoAim`으로 되돌린다.

- [ ] **Step 7: 오발 방지 확인(강제 모바일)**

  플레이 중 `MobileInputState.FireHeld = true;`를 유지한 채 Esc로 일시정지 → 다시 Esc로 재개 후 한 프레임 뒤 `pi.fire`를 읽는다. Expected: `false`(래치). `MobileInputState.FireHeld = false` 후 `true`로 다시 설정하면 `fire == true`.

  재장전 후 계속 누르기: 탄창이 비도록 `FireHeld = true`로 쏘다가 `MobileInputState.RequestReload()` 후 `FireHeld`를 유지해 재장전이 끝난 뒤 즉시 발사하지 않는지 확인한다(탄 수가 줄지 않아야 함). 기대: 기존 `blockFireUntilRelease` 규칙으로 발사 없음.

- [ ] **Step 8: Commit**

  ```bash
  git add Assets/Scripts/PlayerInput.cs Assets/Scripts/PlayerMovement.cs Assets/Editor/MobileEditorMenu.cs Assets/Editor/MobileEditorMenu.cs.meta
  git commit -m "feat: PlayerInput 모바일 입력 경로와 월드 조준 방향 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 8: 터치 UI 위젯(스틱·버튼·세이프 에리어)

**Files:**
- Create: `Assets/Scripts/MobileUI/MobileUIFactory.cs`
- Create: `Assets/Scripts/MobileUI/TouchJoystick.cs`
- Create: `Assets/Scripts/MobileUI/TouchButton.cs`
- Create: `Assets/Scripts/MobileUI/SafeAreaFitter.cs`

**Interfaces:**
- Consumes: `JoystickMath.Evaluate`
- Produces:
  - `static class MobileUIFactory` — `Sprite Circle`(128×128 원형 스프라이트, 캐시), `RectTransform NewRect(string name, Transform parent)`, `Image NewImage(string name, Transform parent, Color color, Sprite sprite = null)`, `Text NewText(string name, Transform parent, string text, int fontSize, Color color, TextAnchor anchor)`, 색 상수 `Panel`, `Amber`, `Light`, `Dim`
  - `TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler` — 필드 `float radius`(px), `float deadZone`, `bool floating`, `System.Action<Vector2> onValue`, `RectTransform baseImage`, `RectTransform knob`, `void ResetStick()`
  - `TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler` — `System.Action onDown`, `System.Action onUp`, `bool Pressed`, `void SetInteractable(bool)`, `void Release()`
  - `SafeAreaFitter : MonoBehaviour` — 부모 Canvas 전체를 채운 RectTransform에 `Screen.safeArea`를 적용

- [ ] **Step 1: MobileUIFactory 작성**

  `Assets/Scripts/MobileUI/MobileUIFactory.cs`:

  ```csharp
  using UnityEngine;
  using UnityEngine.UI;

  // 모바일 터치 UI를 런타임에 코드로 만들기 위한 보조 함수(바이너리 씬을 수정하지 않기 위함)
  // 색은 기존 HUD 톤(어두운 패널 + 앰버 강조)을 따른다
  public static class MobileUIFactory {
      public static readonly Color Panel = new Color(0.07f, 0.075f, 0.085f, 0.72f);
      public static readonly Color Amber = new Color(0.93f, 0.74f, 0.36f, 1f);
      public static readonly Color Light = new Color(0.95f, 0.95f, 0.93f, 1f);
      public static readonly Color Dim = new Color(0.45f, 0.45f, 0.45f, 0.55f);

      private static Sprite circle;

      // 128x128 흰색 원(가장자리 부드럽게). 색은 Image.color로 입힌다
      public static Sprite Circle {
          get {
              if (circle == null)
              {
                  const int size = 128;
                  Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                  texture.wrapMode = TextureWrapMode.Clamp;
                  float center = (size - 1) * 0.5f;
                  float radius = size * 0.5f - 1f;
                  for (int y = 0; y < size; y++)
                  {
                      for (int x = 0; x < size; x++)
                      {
                          float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                          float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                          texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                      }
                  }
                  texture.Apply();
                  circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                  circle.name = "MobileCircle";
              }
              return circle;
          }
      }

      public static RectTransform NewRect(string name, Transform parent) {
          GameObject go = new GameObject(name, typeof(RectTransform));
          go.layer = LayerMask.NameToLayer("UI");
          RectTransform rect = go.GetComponent<RectTransform>();
          rect.SetParent(parent, false);
          return rect;
      }

      public static Image NewImage(string name, Transform parent, Color color, Sprite sprite = null) {
          RectTransform rect = NewRect(name, parent);
          Image image = rect.gameObject.AddComponent<Image>();
          image.sprite = sprite;
          image.color = color;
          return image;
      }

      public static Text NewText(string name, Transform parent, string text, int fontSize, Color color, TextAnchor anchor) {
          RectTransform rect = NewRect(name, parent);
          Text label = rect.gameObject.AddComponent<Text>();
          label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
          label.text = text;
          label.fontSize = fontSize;
          label.color = color;
          label.alignment = anchor;
          label.horizontalOverflow = HorizontalWrapMode.Overflow;
          label.verticalOverflow = VerticalWrapMode.Overflow;
          label.raycastTarget = false;
          return label;
      }

      public static void Stretch(RectTransform rect) {
          rect.anchorMin = Vector2.zero;
          rect.anchorMax = Vector2.one;
          rect.offsetMin = Vector2.zero;
          rect.offsetMax = Vector2.zero;
      }
  }
  ```

- [ ] **Step 2: TouchJoystick 작성**

  `Assets/Scripts/MobileUI/TouchJoystick.cs`:

  ```csharp
  using UnityEngine;
  using UnityEngine.EventSystems;

  // 플로팅 가상 스틱. 이 오브젝트(투명 터치 영역) 안을 처음 누른 위치가 스틱 중심이 되고, 손을 떼면 숨는다
  // 포인터마다 독립된 이벤트라 다른 버튼과 동시에 눌러도 서로 영향이 없다
  public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {
      public float radius = 110f;     // 캔버스 단위(px)
      public float deadZone = 0.2f;
      public RectTransform baseImage; // 스틱 바탕(원)
      public RectTransform knob;      // 스틱 손잡이
      public System.Action<Vector2> onValue; // 데드존 적용 후 값(크기 1 이하)

      private RectTransform zone;
      private int activePointerId = int.MinValue;
      private Vector2 centerLocal;

      private void Awake() {
          zone = (RectTransform)transform;
          SetVisible(false);
      }

      private void OnDisable() {
          ResetStick();
      }

      public void OnPointerDown(PointerEventData eventData) {
          if (activePointerId != int.MinValue)
          {
              return; // 이미 다른 손가락이 잡고 있음
          }

          Vector2 local;
          if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position, eventData.pressEventCamera, out local))
          {
              return;
          }

          activePointerId = eventData.pointerId;
          centerLocal = local;
          baseImage.anchoredPosition = ZoneLocalToAnchored(local);
          knob.anchoredPosition = Vector2.zero;
          SetVisible(true);
          Emit(Vector2.zero);
      }

      public void OnDrag(PointerEventData eventData) {
          if (eventData.pointerId != activePointerId)
          {
              return;
          }

          Vector2 local;
          if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, eventData.position, eventData.pressEventCamera, out local))
          {
              return;
          }

          Vector2 offset = local - centerLocal;
          knob.anchoredPosition = Vector2.ClampMagnitude(offset, radius);
          Emit(JoystickMath.Evaluate(offset, radius, deadZone));
      }

      public void OnPointerUp(PointerEventData eventData) {
          if (eventData.pointerId != activePointerId)
          {
              return;
          }

          ResetStick();
      }

      public void ResetStick() {
          activePointerId = int.MinValue;
          if (baseImage != null)
          {
              SetVisible(false);
          }
          Emit(Vector2.zero);
      }

      private void Emit(Vector2 value) {
          onValue?.Invoke(value);
      }

      private void SetVisible(bool visible) {
          if (baseImage != null)
          {
              baseImage.gameObject.SetActive(visible);
          }
      }

      // 영역 로컬 좌표를 baseImage 부모 기준 anchoredPosition으로 변환(영역 중앙 피벗·중앙 앵커 가정)
      private Vector2 ZoneLocalToAnchored(Vector2 local) {
          return local;
      }
  }
  ```

  주의: `baseImage`의 앵커·피벗은 영역 중앙(0.5, 0.5)이고 부모가 `zone`이라 로컬 좌표를 그대로 `anchoredPosition`에 쓴다. `zone`의 피벗이 중앙이 아니면 오프셋이 생기므로 오버레이 생성 시 영역 피벗을 (0.5, 0.5)로 맞춘다(Task 9).

- [ ] **Step 3: TouchButton 작성**

  `Assets/Scripts/MobileUI/TouchButton.cs`:

  ```csharp
  using UnityEngine;
  using UnityEngine.EventSystems;
  using UnityEngine.UI;

  // 누름/뗌을 구분하는 터치 버튼(발사·재장전·일시정지·무기 슬롯 공용). 손가락이 버튼 밖으로 나가면 뗀 것으로 본다
  public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler {
      public System.Action onDown;
      public System.Action onUp;
      public Image background;
      public Color normalColor = MobileUIFactory.Panel;
      public Color pressedColor = MobileUIFactory.Amber;
      public Color disabledColor = MobileUIFactory.Dim;

      public bool Pressed { get; private set; }
      private bool interactable = true;
      private int activePointerId = int.MinValue;

      private void OnDisable() {
          Release();
      }

      public void SetInteractable(bool value) {
          interactable = value;
          if (!value)
          {
              Release();
          }
          Refresh();
      }

      public void OnPointerDown(PointerEventData eventData) {
          if (!interactable || Pressed)
          {
              return;
          }

          activePointerId = eventData.pointerId;
          Pressed = true;
          Refresh();
          onDown?.Invoke();
      }

      public void OnPointerUp(PointerEventData eventData) {
          if (eventData.pointerId == activePointerId)
          {
              Release();
          }
      }

      public void OnPointerExit(PointerEventData eventData) {
          if (eventData.pointerId == activePointerId)
          {
              Release();
          }
      }

      public void Release() {
          if (!Pressed)
          {
              return;
          }

          Pressed = false;
          activePointerId = int.MinValue;
          Refresh();
          onUp?.Invoke();
      }

      private void Refresh() {
          if (background == null)
          {
              return;
          }

          background.color = !interactable ? disabledColor : (Pressed ? pressedColor : normalColor);
      }
  }
  ```

- [ ] **Step 4: SafeAreaFitter 작성**

  `Assets/Scripts/MobileUI/SafeAreaFitter.cs`:

  ```csharp
  using UnityEngine;

  // 노치·펀치홀·둥근 모서리를 피하도록 UI 컨테이너를 Screen.safeArea 범위로 맞춘다(월드 렌더링 영역은 줄이지 않는다)
  [RequireComponent(typeof(RectTransform))]
  public class SafeAreaFitter : MonoBehaviour {
      private RectTransform rect;
      private Rect lastSafeArea;
      private Vector2Int lastScreenSize;

      private void Awake() {
          rect = (RectTransform)transform;
          Apply();
      }

      private void Update() {
          if (Screen.safeArea != lastSafeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
          {
              Apply();
          }
      }

      private void Apply() {
          Rect safe = Screen.safeArea;
          lastSafeArea = safe;
          lastScreenSize = new Vector2Int(Screen.width, Screen.height);
          if (Screen.width <= 0 || Screen.height <= 0)
          {
              return;
          }

          Vector2 min = safe.position;
          Vector2 max = safe.position + safe.size;
          min.x /= Screen.width;
          min.y /= Screen.height;
          max.x /= Screen.width;
          max.y /= Screen.height;
          rect.anchorMin = min;
          rect.anchorMax = max;
          rect.offsetMin = Vector2.zero;
          rect.offsetMax = Vector2.zero;
      }
  }
  ```

- [ ] **Step 5: 컴파일 확인**

  `refresh_unity` 후 `read_console`(에러만). Expected: 에러 없음. (동작 확인은 Task 9에서 오버레이와 함께 한다)

- [ ] **Step 6: Commit**

  ```bash
  git add Assets/Scripts/MobileUI Assets/Scripts/MobileUI.meta
  git commit -m "feat: 모바일 터치 UI 위젯(스틱·버튼·세이프 에리어) 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 9: MobileTouchOverlay (오버레이 조립·상태 갱신·부트스트랩)

**Files:**
- Create: `Assets/Scripts/MobileUI/MobileTouchOverlay.cs`

**Interfaces:**
- Consumes: Task 8의 위젯·`MobileUIFactory`, `MobileInputState`, `MobileAimSettings`, `GameManager.instance`(`isPaused`, `isGameover`, `awaitingGoalChoice`, `TogglePause()`), `PlayerShooter`(`CurrentSlotIndex`, `IsSlotUnlocked`), `PlayerInput.RequireFireRelease()`
- Produces: `MobileTouchOverlay`(씬에 `PlayerInput`이 있고 `MobilePlatform.IsMobile`이면 런타임에 자동 생성). 게임 상태에 따라 컨트롤을 숨기고, 조준 모드에 따라 발사 버튼/조준 스틱을 전환한다.

- [ ] **Step 1: 오버레이 작성**

  `Assets/Scripts/MobileUI/MobileTouchOverlay.cs`:

  ```csharp
  using UnityEngine;
  using UnityEngine.SceneManagement;
  using UnityEngine.UI;

  // 모바일 터치 오버레이: 이동 스틱, 발사 버튼(오토 에임) 또는 조준 스틱(쌍둥이 스틱), 재장전, 무기 슬롯 4개, 일시정지 버튼
  // 게임 시스템과 분리되어 있고 MobileInputState에 값만 쓴다. 씬에 PlayerInput이 있을 때만 모바일에서 자동 생성된다
  public class MobileTouchOverlay : MonoBehaviour {
      private const string ObjectName = "Mobile Touch Overlay";
      private const float MoveDeadZone = 0.2f;
      private const float AimDeadZone = 0.25f;

      private RectTransform controlsRoot;     // 정지·게임오버 때 숨길 컨트롤 묶음(일시정지 버튼 제외)
      private GameObject fireButtonObject;
      private GameObject aimZoneObject;
      private TouchJoystick moveStick;
      private TouchJoystick aimStick;
      private TouchButton fireButton;
      private TouchButton reloadButton;
      private TouchButton pauseButton;
      private readonly TouchButton[] slotButtons = new TouchButton[4];
      private readonly Text[] slotLabels = new Text[4];
      private PlayerShooter playerShooter;
      private PlayerInput playerInput;
      private GameObject pauseButtonObject;

      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
      private static void Bootstrap() {
          SceneManager.sceneLoaded += (scene, mode) => TryCreate();
          TryCreate();
      }

      private static void TryCreate() {
          if (!MobilePlatform.IsMobile || FindFirstObjectByType<MobileTouchOverlay>() != null)
          {
              return;
          }

          if (FindFirstObjectByType<PlayerInput>() == null)
          {
              return; // 인트로 등 플레이어가 없는 씬에는 만들지 않는다
          }

          new GameObject(ObjectName).AddComponent<MobileTouchOverlay>();
      }

      private void Awake() {
          playerShooter = FindFirstObjectByType<PlayerShooter>();
          playerInput = FindFirstObjectByType<PlayerInput>();
          BuildCanvas();
          ApplyAimMode(MobileAimSettings.Mode, false);
          MobileAimSettings.Changed += OnAimModeChanged;
      }

      private void OnDestroy() {
          MobileAimSettings.Changed -= OnAimModeChanged;
          MobileInputState.ResetAll();
      }

      private void OnAimModeChanged(MobileAimMode mode) {
          ApplyAimMode(mode, true);
      }

      // 조준 모드에 맞게 발사 버튼/조준 스틱을 전환하고, 남은 발사·조준 입력은 초기화한다
      private void ApplyAimMode(MobileAimMode mode, bool resetInput) {
          bool twin = mode == MobileAimMode.TwinStick;
          fireButtonObject.SetActive(!twin);
          aimZoneObject.SetActive(twin);

          if (resetInput)
          {
              fireButton.Release();
              aimStick.ResetStick();
              if (playerInput != null)
              {
                  playerInput.RequireFireRelease();
              }
              else
              {
                  MobileInputState.ResetAll();
              }
          }
      }

      private void BuildCanvas() {
          Canvas canvas = gameObject.AddComponent<Canvas>();
          canvas.renderMode = RenderMode.ScreenSpaceOverlay;
          canvas.sortingOrder = 50;
          CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
          scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
          scaler.referenceResolution = new Vector2(1920f, 1080f);
          scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
          scaler.matchWidthOrHeight = 0.5f;
          gameObject.AddComponent<GraphicRaycaster>();

          RectTransform safe = MobileUIFactory.NewRect("Safe Area", transform);
          MobileUIFactory.Stretch(safe);
          safe.gameObject.AddComponent<SafeAreaFitter>();

          controlsRoot = MobileUIFactory.NewRect("Controls", safe);
          MobileUIFactory.Stretch(controlsRoot);

          BuildMoveZone();
          BuildAimZone();
          BuildFireButton();
          BuildReloadButton();
          BuildWeaponSlots();
          BuildPauseButton(safe);
      }

      // 왼쪽 아래 이동 스틱 영역(화면 왼쪽 45%, 아래 80%). 위쪽은 HUD를 가리지 않도록 비운다
      private void BuildMoveZone() {
          RectTransform zone = NewZone("Move Zone", new Vector2(0f, 0f), new Vector2(0.45f, 0.8f));
          moveStick = CreateStick(zone, MoveDeadZone);
          moveStick.onValue = value => MobileInputState.Move = value;
      }

      // 오른쪽 아래 조준 스틱 영역(쌍둥이 스틱 모드). 화면 오른쪽 45%, 아래 80%
      private void BuildAimZone() {
          RectTransform zone = NewZone("Aim Zone", new Vector2(0.55f, 0f), new Vector2(1f, 0.8f));
          aimZoneObject = zone.gameObject;
          aimStick = CreateStick(zone, AimDeadZone);
          aimStick.onValue = value => MobileInputState.AimStick = value;
      }

      private RectTransform NewZone(string name, Vector2 anchorMin, Vector2 anchorMax) {
          RectTransform zone = MobileUIFactory.NewRect(name, controlsRoot);
          zone.anchorMin = anchorMin;
          zone.anchorMax = anchorMax;
          zone.offsetMin = Vector2.zero;
          zone.offsetMax = Vector2.zero;
          zone.pivot = new Vector2(0.5f, 0.5f);
          Image hit = zone.gameObject.AddComponent<Image>();
          hit.color = new Color(0f, 0f, 0f, 0f); // 보이지 않지만 터치는 받는다
          return zone;
      }

      private TouchJoystick CreateStick(RectTransform zone, float deadZone) {
          Image baseImage = MobileUIFactory.NewImage("Base", zone, new Color(1f, 1f, 1f, 0.16f), MobileUIFactory.Circle);
          baseImage.raycastTarget = false;
          baseImage.rectTransform.sizeDelta = new Vector2(240f, 240f);
          Image knob = MobileUIFactory.NewImage("Knob", baseImage.transform, new Color(MobileUIFactory.Amber.r, MobileUIFactory.Amber.g, MobileUIFactory.Amber.b, 0.85f), MobileUIFactory.Circle);
          knob.raycastTarget = false;
          knob.rectTransform.sizeDelta = new Vector2(110f, 110f);

          TouchJoystick stick = zone.gameObject.AddComponent<TouchJoystick>();
          stick.radius = 110f;
          stick.deadZone = deadZone;
          stick.baseImage = baseImage.rectTransform;
          stick.knob = knob.rectTransform;
          return stick;
      }

      // 오른쪽 아래 발사 버튼(오토 에임 모드)
      private void BuildFireButton() {
          fireButton = CreateRoundButton("Fire Button", controlsRoot, new Vector2(-260f, 250f), 220f, "FIRE", 40);
          fireButtonObject = fireButton.gameObject;
          fireButton.onDown = () =>
          {
              MobileInputState.FireHeld = true;
              MobileInputState.RequestFireDown();
          };
          fireButton.onUp = () => MobileInputState.FireHeld = false;
      }

      // 발사 버튼 위쪽 재장전 버튼
      private void BuildReloadButton() {
          reloadButton = CreateRoundButton("Reload Button", controlsRoot, new Vector2(-500f, 470f), 130f, "R", 44);
          reloadButton.onDown = MobileInputState.RequestReload;
      }

      private TouchButton CreateRoundButton(string name, Transform parent, Vector2 anchoredFromBottomRight, float size, string label, int fontSize) {
          Image background = MobileUIFactory.NewImage(name, parent, MobileUIFactory.Panel, MobileUIFactory.Circle);
          RectTransform rect = background.rectTransform;
          rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
          rect.pivot = new Vector2(0.5f, 0.5f);
          rect.anchoredPosition = anchoredFromBottomRight;
          rect.sizeDelta = new Vector2(size, size);

          Image ring = MobileUIFactory.NewImage("Ring", rect, MobileUIFactory.Amber, MobileUIFactory.Circle);
          ring.raycastTarget = false;
          MobileUIFactory.Stretch(ring.rectTransform);
          ring.rectTransform.offsetMin = new Vector2(-4f, -4f);
          ring.rectTransform.offsetMax = new Vector2(4f, 4f);
          ring.transform.SetAsFirstSibling();

          Text text = MobileUIFactory.NewText("Label", rect, label, fontSize, MobileUIFactory.Light, TextAnchor.MiddleCenter);
          MobileUIFactory.Stretch(text.rectTransform);

          TouchButton button = background.gameObject.AddComponent<TouchButton>();
          button.background = background;
          return button;
      }

      // 오른쪽 가장자리 세로 배치 무기 슬롯 4개(1=권총, 2=소총, 3=SMG, 4=산탄총)
      private void BuildWeaponSlots() {
          string[] names = { "1 PISTOL", "2 RIFLE", "3 SMG", "4 SHOTGUN" };
          for (int i = 0; i < 4; i++)
          {
              int slotIndex = i;
              Image background = MobileUIFactory.NewImage("Slot " + (i + 1), controlsRoot, MobileUIFactory.Panel);
              RectTransform rect = background.rectTransform;
              rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
              rect.pivot = new Vector2(1f, 0.5f);
              rect.anchoredPosition = new Vector2(-24f, 820f - i * 100f);
              rect.sizeDelta = new Vector2(230f, 88f);

              Text text = MobileUIFactory.NewText("Label", rect, names[i], 30, MobileUIFactory.Light, TextAnchor.MiddleCenter);
              MobileUIFactory.Stretch(text.rectTransform);
              slotLabels[i] = text;

              TouchButton button = background.gameObject.AddComponent<TouchButton>();
              button.background = background;
              button.onDown = () =>
              {
                  if (playerShooter != null && playerShooter.IsSlotUnlocked(slotIndex))
                  {
                      MobileInputState.RequestSwap(slotIndex);
                  }
              };
              slotButtons[i] = button;
          }
      }

      // 오른쪽 위 일시정지 버튼(안전 영역 안). 정지 중에는 기존 메뉴가 계속하기를 담당하므로 숨긴다
      private void BuildPauseButton(RectTransform safe) {
          Image background = MobileUIFactory.NewImage("Pause Button", safe, MobileUIFactory.Panel, MobileUIFactory.Circle);
          RectTransform rect = background.rectTransform;
          rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
          rect.pivot = new Vector2(1f, 1f);
          rect.anchoredPosition = new Vector2(-24f, -24f);
          rect.sizeDelta = new Vector2(110f, 110f);
          Text text = MobileUIFactory.NewText("Label", rect, "II", 44, MobileUIFactory.Light, TextAnchor.MiddleCenter);
          MobileUIFactory.Stretch(text.rectTransform);

          pauseButton = background.gameObject.AddComponent<TouchButton>();
          pauseButton.background = background;
          pauseButtonObject = background.gameObject;
          pauseButton.onDown = () =>
          {
              GameManager manager = GameManager.instance;
              if (manager != null && !manager.isGameover && !manager.isPaused && !manager.awaitingGoalChoice)
              {
                  manager.TogglePause();
              }
          };
      }

      private void Update() {
          GameManager manager = GameManager.instance;
          bool blocked = manager != null && (manager.isPaused || manager.isGameover || manager.awaitingGoalChoice);
          if (controlsRoot.gameObject.activeSelf == blocked)
          {
              controlsRoot.gameObject.SetActive(!blocked);
          }
          if (pauseButtonObject.activeSelf == blocked)
          {
              pauseButtonObject.SetActive(!blocked);
          }

          if (blocked || playerShooter == null)
          {
              return;
          }

          reloadButton.SetInteractable(true);
          for (int i = 0; i < 4; i++)
          {
              bool unlocked = playerShooter.IsSlotUnlocked(i);
              bool selected = playerShooter.CurrentSlotIndex == i;
              slotButtons[i].SetInteractable(unlocked);
              // 선택·보유 상태는 배경색을 직접 갱신한다(누르고 있는 동안의 색은 TouchButton이 담당)
              if (!slotButtons[i].Pressed)
              {
                  slotButtons[i].background.color = !unlocked ? MobileUIFactory.Dim : (selected ? MobileUIFactory.Amber : MobileUIFactory.Panel);
              }
              slotLabels[i].color = !unlocked ? MobileUIFactory.Dim : (selected ? Color.black : MobileUIFactory.Light);
          }
      }
  }
  ```

  슬롯 위치(`820 - i*100`, 오른쪽 가장자리, 하단 기준 위로)는 일시정지 버튼(오른쪽 위)과 발사 버튼(오른쪽 아래)과 겹치지 않는 초기값이다. 실제 위치는 Step 3·4의 화면 검수와 Task 12 실기기 검수에서 조정한다.

- [ ] **Step 2: 컴파일 확인**

  `refresh_unity` 후 `read_console`(에러만). Expected: 에러 없음.

- [ ] **Step 3: 에디터 시뮬레이션(강제 모바일)**

  Game 뷰를 1920×1080으로 두고, `Force Mobile Input In Editor`를 켠 뒤 `UrbanSurvival.unity` 플레이. Unity MCP `manage_scene`/스크린샷 또는 Game 뷰 캡처로 확인:
  - 왼쪽 화면을 클릭 드래그하면 스틱이 나타나고 캐릭터가 이동한다(마우스 드래그는 포인터 이벤트로 동작).
  - FIRE 버튼을 누르는 동안 가장 가까운 좀비 방향을 보고 발사한다. 좀비가 없으면 이동 방향.
  - R 버튼, 슬롯 버튼(권총만 보유 시 2~4 비활성 표시, 터치해도 교체 없음), 우상단 일시정지 버튼.
  - 일시정지 시 컨트롤이 사라지고 기존 일시정지 메뉴만 보이며 재개 후 FIRE를 누르고 있어도 즉시 발사하지 않는다.
  - `MobileAimSettings.Mode = MobileAimMode.TwinStick`(`execute_code`) 후 FIRE가 사라지고 오른쪽 영역 드래그가 조준·발사가 된다.
  - HUD(체력, 점수, 웨이브, 무기 패널, 미니맵)와 겹치는 위치를 스크린샷으로 확인하고 겹치면 버튼 위치를 조정한다. 확인 결과를 작업 로그에 기록한다.

  에디터에서 마우스 한 개로는 멀티터치를 검증할 수 없다. 멀티터치(이동하면서 발사)는 Task 12 실기기 검수 항목으로 남기고 "미검증"으로 표시한다.

- [ ] **Step 4: 화면비 확인**

  Game 뷰 해상도를 1920×1080(16:9), 2340×1080(약 19.5:9), 1920×1200(16:10)로 바꿔 스크린샷을 찍어 버튼이 화면 밖으로 나가거나 HUD와 겹치지 않는지 확인한다. 노치는 `Screen.safeArea`를 에디터에서 흉내 내기 어려우므로 Simulator 뷰(Window > General > Device Simulator)에서 노치가 있는 기기를 골라 확인한다. 가능하지 않으면 "노치 미검증, 실기기로 확인"으로 기록한다.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/MobileUI/MobileTouchOverlay.cs Assets/Scripts/MobileUI/MobileTouchOverlay.cs.meta
  git commit -m "feat: 모바일 터치 오버레이(스틱·발사·재장전·무기 슬롯·일시정지) 추가" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 10: 설정 창 조준 모드 토글과 모바일 설정 정리

**Files:**
- Create: `Assets/Scripts/MobileUI/MobileAimModeSettingsBinder.cs`
- Modify: `Assets/Modules/GameSettingsKit/Runtime/SettingsStore.cs:156-172`(`ApplyDisplay`)

**Interfaces:**
- Consumes: `GameSettingsKit.SettingsPanel`(`vSyncToggle`, `fullscreenToggle`, `resolutionDropdown`, `onOpened`, `window`), `MobileAimSettings`, `MobilePlatform`
- Produces: 모바일에서 설정 창에 "AIM MODE" 토글 행(켜짐 = 쌍둥이 스틱), 전체 화면·해상도·VSync 행 숨김

- [ ] **Step 1: SettingsStore의 PC 전용 적용을 모바일에서 건너뛰기**

  `SettingsStore.cs`의 `ApplyDisplay(bool force)` 첫 줄에 다음을 추가한다(모듈 안에는 게임 전용 타입을 넣지 않는다. 플랫폼 일반 가드).

  ```csharp
              // 모바일은 화면 크기·전체 화면을 운영체제가 정하므로 적용하지 않는다
              if (Application.isMobilePlatform)
              {
                  return;
              }
  ```

  또한 `ApplyQuality()`의 `QualitySettings.vSyncCount = Current.vSync ? 1 : 0;` 줄을 모바일에서는 건너뛰게 한다(`Application.targetFrameRate`가 무시되는 것을 막고 프레임 제어를 `MobilePerformance`에 맡긴다).

  ```csharp
              if (!Application.isMobilePlatform)
              {
                  QualitySettings.vSyncCount = Current.vSync ? 1 : 0;
              }
  ```

  수정 위치의 실제 줄 번호와 변수 이름은 파일을 열어 확인한다. `Application`을 쓰려면 파일 상단에 `using UnityEngine;`이 이미 있다(`Screen.SetResolution` 사용 중).

- [ ] **Step 2: 바인더 작성**

  `Assets/Scripts/MobileUI/MobileAimModeSettingsBinder.cs`:

  ```csharp
  using GameSettingsKit;
  using UnityEngine;
  using UnityEngine.SceneManagement;
  using UnityEngine.UI;

  // 설정 창(GameSettingsKit)에 모바일 조준 모드 토글을 붙이는 연결 계층. 모듈은 이 클래스를 모른다
  // VSync 행을 복제해 "AIM MODE" 행을 만들고(씬 파일을 수정하지 않음), 모바일에서는 PC 전용 행(전체 화면·해상도·VSync)을 숨긴다
  public static class MobileAimModeSettingsBinder {
      private const string RowName = "Mobile Aim Mode Row";

      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
      private static void Bootstrap() {
          SceneManager.sceneLoaded += (scene, mode) => BindAll();
          BindAll();
      }

      private static void BindAll() {
          if (!MobilePlatform.IsMobile)
          {
              return;
          }

          SettingsPanel[] panels = Object.FindObjectsByType<SettingsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
          foreach (SettingsPanel panel in panels)
          {
              Bind(panel);
          }
      }

      private static void Bind(SettingsPanel panel) {
          if (panel.vSyncToggle == null)
          {
              return;
          }

          Transform vSyncRow = FindRow(panel.vSyncToggle.transform);
          if (vSyncRow == null || vSyncRow.parent.Find(RowName) != null)
          {
              return;
          }

          // VSync 행을 복제해 조준 모드 행으로 사용한다(원본을 숨기기 전에 복제해야 복제본이 켜진 상태로 만들어진다)
          GameObject clone = Object.Instantiate(vSyncRow.gameObject, vSyncRow.parent);
          clone.name = RowName;
          clone.SetActive(true);
          clone.transform.SetSiblingIndex(vSyncRow.GetSiblingIndex() + 1);

          // 모바일에서 의미 없는 PC 전용 행 숨김
          HideRow(panel.fullscreenToggle != null ? panel.fullscreenToggle.transform : null);
          HideRow(panel.resolutionDropdown != null ? panel.resolutionDropdown.transform : null);
          vSyncRow.gameObject.SetActive(false);

          Text label = clone.GetComponentInChildren<Text>(true);
          if (label != null)
          {
              label.text = "TWIN-STICK AIM";
          }

          Toggle toggle = clone.GetComponentInChildren<Toggle>(true);
          if (toggle == null)
          {
              Object.Destroy(clone);
              return;
          }

          toggle.onValueChanged.RemoveAllListeners();
          toggle.SetIsOnWithoutNotify(MobileAimSettings.Mode == MobileAimMode.TwinStick);
          toggle.onValueChanged.AddListener(isOn => MobileAimSettings.Mode = isOn ? MobileAimMode.TwinStick : MobileAimMode.AutoAim);
          panel.onOpened.AddListener(() => toggle.SetIsOnWithoutNotify(MobileAimSettings.Mode == MobileAimMode.TwinStick));
      }

      // 컨트롤 → 행(이름이 "... Row"인 조상)
      private static Transform FindRow(Transform control) {
          Transform current = control;
          while (current != null && !current.name.EndsWith(" Row"))
          {
              current = current.parent;
          }
          return current;
      }

      private static void HideRow(Transform control) {
          if (control == null)
          {
              return;
          }

          Transform row = FindRow(control);
          if (row != null)
          {
              row.gameObject.SetActive(false);
          }
      }
  }
  ```

  `GameSettingsKit.SettingsPanel`은 `GameManager.cs`가 이미 쓰고 있어 접근 가능하다. 설정 창이 `Dim`·`Frame` 등 하위 구조를 `window` 아래에 두고 행이 `Frame`의 수직 레이아웃 자식이므로, 복제본은 같은 부모에 들어가 자동으로 배치된다. 행 이름이 `"... Row"`로 끝난다는 가정(빌더의 `Row()`가 만드는 이름 `VSync Row`, `Fullscreen Row`, `Resolution Row`)에 의존하므로, Step 4 화면 확인에서 행이 실제로 보이고 숨겨지는지 반드시 확인한다.

- [ ] **Step 3: 컴파일 확인**

  `refresh_unity` 후 `read_console`(에러만). Expected: 에러 없음.

- [ ] **Step 4: 설정 창에서 토글 확인(강제 모바일)**

  강제 모바일을 켜고 `UrbanSurvival.unity` 플레이 → Esc로 일시정지 → SETTINGS 열기. 스크린샷으로 확인: 전체 화면·해상도·VSync 행이 없고 "TWIN-STICK AIM" 토글 행이 있다. 토글을 켜면 닫은 뒤 FIRE 버튼이 사라지고 오른쪽 조준 영역이 켜진다. 에디터를 정지 후 다시 플레이하면 마지막 값이 유지된다(F12). 창이 화면을 넘치지 않는지(행 수 감소로 문제없음) 확인한다. 같은 확인을 `Assets/Game/Scenes/Intro.unity`에서 한다.

  강제 모바일을 끄고 PC에서 설정 창이 기존과 같은지(전체 화면·해상도·VSync 표시, 조준 모드 행 없음) 확인한다.

- [ ] **Step 5: Commit**

  ```bash
  git add Assets/Scripts/MobileUI/MobileAimModeSettingsBinder.cs Assets/Scripts/MobileUI/MobileAimModeSettingsBinder.cs.meta Assets/Modules/GameSettingsKit/Runtime/SettingsStore.cs
  git commit -m "feat: 설정 창 모바일 조준 모드 토글과 모바일 설정 정리" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 11: 모바일 문구·메뉴 대응

**Files:**
- Modify: `Assets/Scripts/ReloadIndicator.cs:67`
- Modify: `Assets/Scripts/IntroMenu.cs`(QUIT 숨김), `Assets/Scripts/UIManager.cs`(QUIT 숨김)
- Modify: 튜토리얼 문구(씬 직렬화 값 `TutorialPopup.body`) — 코드에서 모바일일 때 본문을 교체

**Interfaces:**
- Consumes: `MobilePlatform.IsMobile`, `MobileAimSettings.Mode`

- [ ] **Step 1: ReloadIndicator 문구 분기**

  `ReloadIndicator.cs`의 메시지 결정 줄을 다음으로 바꾼다.

  ```csharp
                  bool mobile = MobilePlatform.IsMobile;
                  message = gun.ammoRemain == 0 ? (mobile ? "NO AMMO" : "NO AMMO  [1]") : (mobile ? "RELOAD" : "RELOAD  [R]");
  ```

  모바일은 무기 슬롯 버튼으로 권총 교체가 가능하므로 `[1]`은 숨긴다.

- [ ] **Step 2: QUIT 버튼 숨김**

  QUIT 버튼은 씬에 배치된 버튼이라 코드에서 이름으로 찾는다. 먼저 어떤 오브젝트가 `QuitGame`에 연결됐는지 확인한다: Unity MCP `find_gameobjects`로 "Quit" 이름 검색(Intro 씬과 게임 씬 모두), 또는 `execute_code`로 `Object.FindObjectsByType<Button>(FindObjectsInactive.Include, ...)`에서 `onClick` 영속 대상 메서드가 `QuitGame`인 버튼을 찾는다. 찾은 버튼 오브젝트 이름을 `QuitButton` 후보로 기록한다.

  `Assets/Scripts/MobileUI/MobileQuitHider.cs`를 새로 만들어 씬 로드 때 모바일이면 해당 버튼을 숨긴다(씬 파일 수정 없음).

  ```csharp
  using UnityEngine;
  using UnityEngine.Events;
  using UnityEngine.SceneManagement;
  using UnityEngine.UI;

  // 모바일에서는 게임 종료(QUIT) 버튼을 숨긴다(Android는 앱 종료 버튼을 두지 않는 것이 관례)
  public static class MobileQuitHider {
      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
      private static void Bootstrap() {
          SceneManager.sceneLoaded += (scene, mode) => HideAll();
          HideAll();
      }

      private static void HideAll() {
          if (!MobilePlatform.IsMobile)
          {
              return;
          }

          foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
          {
              for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
              {
                  if (button.onClick.GetPersistentMethodName(i) == "QuitGame")
                  {
                      button.gameObject.SetActive(false);
                      break;
                  }
              }
          }
      }
  }
  ```

  게임 오버 화면의 QUIT 버튼은 비활성 상태에서 시작해 `SetActive(true)`로 켜지는 구조일 수 있다(UI 패널이 켜질 때 자식도 같이 켜짐). 이 경우 버튼이 비활성인 채 숨겨진 상태가 유지되도록 버튼을 `SetActive(false)`로 직접 끈 것이 패널이 켜질 때 영향을 받지 않는지 에디터에서 확인한다(자식 오브젝트를 직접 끄면 패널 활성과 무관하게 꺼진 채 유지된다). 레이아웃 그룹 때문에 남은 버튼의 간격이 어색하면 모바일에서 확인 후 조정한다.

- [ ] **Step 3: 튜토리얼 문구 교체**

  `TutorialPopup.ShowThen`이 본문을 `body` 필드에서 읽으므로, 모바일에서 조준 모드에 맞는 본문으로 바꾼다. `TutorialPopup.cs`의 `bodyText.text = body;` 줄을 다음으로 바꾼다.

  ```csharp
          bodyText.text = MobilePlatform.IsMobile ? GetMobileBody() : body;
  ```

  같은 파일에 메서드를 추가한다.

  ```csharp
      // 모바일 튜토리얼: 현재 조준 모드에 맞는 조작 안내(기존 PC 본문은 키보드·마우스 설명이라 그대로 쓸 수 없다)
      private static string GetMobileBody() {
          if (MobileAimSettings.Mode == MobileAimMode.TwinStick)
          {
              return "<b>조작 방법</b>\n\n- 왼쪽 화면을 누르고 끌어 이동합니다.\n- 오른쪽 화면을 누르고 끌면 그 방향으로 조준하며 발사합니다. 놓으면 멈춥니다.\n- R 버튼으로 재장전합니다.\n- 오른쪽의 무기 칸을 눌러 무기를 바꿉니다.\n- 오른쪽 위 II 버튼으로 일시정지합니다.";
          }

          return "<b>조작 방법</b>\n\n- 왼쪽 화면을 누르고 끌어 이동합니다.\n- FIRE 버튼을 누르면 가장 가까운 적을 자동으로 조준하며 발사합니다.\n- R 버튼으로 재장전합니다.\n- 오른쪽의 무기 칸을 눌러 무기를 바꿉니다.\n- 오른쪽 위 II 버튼으로 일시정지합니다.";
      }
  ```

  주의: 인트로 씬의 튜토리얼 본문에 인트로 단계 전용 내용(캐릭터 선택 안내 등)이 더 들어 있다면 PC 본문과 비교해 모바일 본문에 필요한 부분을 합친다. 먼저 `Grep "body" Assets/Scripts/TutorialPopup.cs`와 에디터에서 인트로의 TutorialPopup 본문을 확인한 뒤 위 문구를 그 구성에 맞춘다. 한글이 안 깨지도록 기존 `ApplyFont()` 흐름은 그대로 유지한다.

- [ ] **Step 4: 컴파일·화면 확인(강제 모바일)**

  `refresh_unity` 후 에러 확인. 강제 모바일로 인트로를 플레이해 QUIT 버튼 숨김, 튜토리얼 모바일 문구를 확인하고, 게임 씬에서 탄창 0 상태의 머리 위 알림이 "RELOAD"/"NO AMMO"로 나오는지 확인한다. 강제 모바일을 끄고 PC에서 기존 문구(`RELOAD  [R]`, QUIT 표시, 기존 튜토리얼)가 그대로인지 확인한다.

- [ ] **Step 5: 터치 메뉴 검수(강제 모바일, 마우스 클릭으로 대신)**

  인트로 → START → 캐릭터 선택 → 게임 → 일시정지 → 설정 → 게임 오버 → 목표 결과 화면(결과 테스트 씬 `UrbanSurvival_ResultTest.unity`)에서 모든 버튼이 클릭 가능하고 서로 겹치지 않는지 확인한다. 버튼이 너무 작으면(약 48dp ≈ 1920×1080 기준 대략 70px 이하) 모바일에서만 크기·간격을 키우는 코드를 `MobileQuitHider`와 같은 방식(별도 정적 부트스트랩)으로 추가하고, 어떤 버튼을 얼마나 키웠는지 로그에 적는다. 실제 터치 감도는 Task 12 실기기에서 최종 확인한다.

- [ ] **Step 6: Commit**

  ```bash
  git add Assets/Scripts/ReloadIndicator.cs Assets/Scripts/TutorialPopup.cs Assets/Scripts/MobileUI/MobileQuitHider.cs Assets/Scripts/MobileUI/MobileQuitHider.cs.meta
  git commit -m "feat: 모바일 문구 분기, QUIT 숨김, 모바일 튜토리얼 문구" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

  (메뉴 버튼 크기 조정 코드를 추가했다면 해당 파일도 `git add`에 포함한다.)

---

## Task 12: Android 빌드 설정과 첫 개발 빌드(P3)

**Files:**
- Modify: `Assets/Editor/MobileEditorMenu.cs`(Android 설정 적용 메뉴 추가)
- Modify: `ProjectSettings/ProjectSettings.asset`
- Create(로컬 전용, 커밋하지 않음): `Keystore/` 폴더의 업로드 키(Task 14)

**Interfaces:**
- Produces: 메뉴 `Urban Survival/Mobile/Apply Android Settings`(재현 가능한 Android Player Settings 적용)

- [ ] **Step 1: Android 설정 적용 메뉴 추가**

  `MobileEditorMenu.cs`에 다음을 추가한다(맨 위 `using`에 `UnityEngine`, `UnityEditor.Build`를 추가).

  ```csharp
      // 모바일 포팅의 Android Player Settings를 한 번에 적용한다(재현 가능하도록 코드로 둔다)
      [MenuItem("Urban Survival/Mobile/Apply Android Settings")]
      private static void ApplyAndroidSettings() {
          PlayerSettings.companyName = "YWS";
          PlayerSettings.productName = "Urban Survival";
          PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yws.urbansurvival");

          PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
          PlayerSettings.allowedAutorotateToPortrait = false;
          PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
          PlayerSettings.allowedAutorotateToLandscapeLeft = true;
          PlayerSettings.allowedAutorotateToLandscapeRight = true;

          PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
          PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
          PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
          PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
          EditorUserBuildSettings.buildAppBundle = true;

          // 프로젝트가 URP 호환 모드이므로 Android 빌드에도 같은 정의가 있어야 빌드가 거부되지 않는다
          string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
          if (!defines.Contains("URP_COMPATIBILITY_MODE"))
          {
              PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, defines + (defines.Length > 0 ? ";" : "") + "URP_COMPATIBILITY_MODE");
          }

          Debug.Log("[Mobile] Android 설정 적용 완료: " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
      }
  ```

  `targetSdkVersion = (AndroidSdkVersions)36`은 Unity 6000.3의 `AndroidSdkVersions` 열거형에 36이 정의돼 있어야 컴파일된다. 컴파일 에러가 나면 `AndroidApiLevel36` 같은 이름이 있는지 `unity_reflect`로 `AndroidSdkVersions` 값을 확인해 올바른 값을 쓰고, 열거형에 없으면 `AndroidSdkVersions.AndroidApiLevelAuto`로 두고 Unity가 설치한 SDK 최고 레벨을 쓰는지 확인한다(타깃 36 이상 충족 여부는 Step 5의 AAB 매니페스트로 검증한다).

- [ ] **Step 2: 설정 적용과 확인**

  `refresh_unity` 후 `execute_menu_item`으로 `Urban Survival/Mobile/Apply Android Settings` 실행. `read_console`에서 로그 확인. `ProjectSettings/ProjectSettings.asset`에서 `applicationIdentifier`의 Android 항목이 `com.yws.urbansurvival`, `AndroidTargetArchitectures: 2`(ARM64), `AndroidMinSdkVersion: 25`인지 grep으로 확인한다.

  Run: `git diff --stat ProjectSettings/ProjectSettings.asset`
  Expected: 이 Task에서 의도한 항목 외에 URP·재저장성 변경이 섞였는지 확인한다. 섞여 있으면 `git add -p`로 의도한 줄(회사명, 제품명, 패키지, 방향, 아키텍처, API, 스크립팅 정의)만 스테이징한다.

- [ ] **Step 3: 빌드 대상과 씬 목록 확인**

  Unity MCP `manage_build`로 활성 빌드 대상을 Android로 전환한다(첫 전환은 시간이 걸린다). 빌드 씬 목록(`EditorBuildSettings`)이 `Intro` → `UrbanSurvival` → `Main`(비교용) 외 연습 씬(`BossTestScene`, `FinalBossTestScene`)을 포함하는지 확인하고 변경하지 않는다.

- [ ] **Step 4: 개발용 서명으로 개발 빌드(APK) 생성**

  개발 중에는 Unity의 debug keystore로 서명한 APK로 두 기기에 설치해 확인한다. `manage_build`(또는 `execute_code`의 `BuildPipeline.BuildPlayer`)로 다음 조건으로 빌드한다: 대상 Android, `buildAppBundle = false`, `BuildOptions.Development | BuildOptions.AllowDebugging`, 출력 `Builds/Android/UrbanSurvival-dev.apk`. `Builds/`는 `.gitignore`에 이미 있다.

  Expected: 오류 없이 APK 생성. 실패하면 `read_console`의 오류(Android SDK/NDK/JDK 경로, Gradle, API 레벨)를 해결한다. SDK 36 플랫폼이 설치되어 있지 않으면 Unity가 내려받거나 안내하므로 그 지시를 따르고, 승인이 필요한 다운로드는 사용자에게 먼저 확인한다.

- [ ] **Step 5: 기기 설치와 실행(사용자 협조)**

  사용자에게 요청: S26 Ultra와 XiaoxinPad 2025에서 개발자 옵션과 USB 디버깅을 켜고 PC에 연결해 달라고 한다. `adb`는 Unity가 설치한 `.../AndroidPlayer/SDK/platform-tools/adb.exe`를 쓴다.

  ```bash
  ADB="C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe"
  "$ADB" devices
  "$ADB" install -r Builds/Android/UrbanSurvival-dev.apk
  "$ADB" shell monkey -p com.yws.urbansurvival -c android.intent.category.LAUNCHER 1
  ```

  기기에 연결되지 않으면 실기기 검증 항목을 "미검증"으로 남기고 사용자가 APK를 직접 설치해 확인하도록 안내한다.

- [ ] **Step 6: 실기기 검수 체크리스트 실행(양쪽 기기)**

  각 항목에 통과·실패·메모를 작업 로그에 적는다.

  - 인트로 → 캐릭터 선택 → 게임 → 결과(또는 게임 오버)까지 터치만으로 진행 가능(I03)
  - F01/F02: 이동 스틱 방향과 속도(대각선 포함)
  - F03/F04: 발사 버튼 단발·연사(권총 단발, 소총·SMG 연사, 산탄총 1회)
  - F05/F16: R 버튼 재장전, 재장전 중 발사 터치 유지 후 자동 사격 없음
  - F06~F08: 오토 에임(적 2마리, 대상 없음 이동, 정지)
  - F09~F11: 설정에서 쌍둥이 스틱으로 전환, 조준·연사·재발사
  - F12: 앱 재시작 후 조준 모드 유지
  - F13: 발사 유지 중 홈 버튼으로 나갔다 복귀·일시정지 후 재개 시 즉시 발사하지 않음
  - F14/F15: 미획득 슬롯 터치 무반응, 획득 슬롯 터치 교체(드랍 획득 후)
  - **멀티터치:** 이동 스틱을 잡은 채 발사 버튼·R·슬롯을 눌러도 이동이 끊기지 않고 동시에 동작
  - 화면비·노치: 두 기기에서 HUD와 터치 버튼 겹침·잘림 없음(I02), 필요 시 버튼 위치·크기 조정(조정한 값은 `MobileTouchOverlay` 상수에 반영하고 커밋)
  - 터치 영역 크기가 손가락으로 누르기에 충분한지(48dp 수준), 메뉴 버튼 포함
  - 안드로이드 뒤로 가기 키가 일시정지로 동작하는지(Esc 매핑), 앱 전환 후 복귀 시 입력 고착 없음

- [ ] **Step 7: PC 빌드 회귀 확인**

  Windows 에디터에서 강제 모바일을 끄고 Task 0 Step 4 항목을 다시 확인한다(I01). 시간이 있으면 Windows 독립 빌드로도 확인한다(빌드는 약 11분, 사용자가 작업 중이면 먼저 묻는다).

- [ ] **Step 8: Commit**

  ```bash
  git add Assets/Editor/MobileEditorMenu.cs ProjectSettings/ProjectSettings.asset
  git commit -m "feat: Android Player Settings 적용 메뉴와 첫 Android 빌드 설정" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

  (실기기 검수에서 오버레이 상수를 조정했다면 `MobileTouchOverlay.cs`도 함께 `git add`한다. 검수 결과는 작업 로그 파일로 남긴다.)

---

## Task 13: 모바일 성능 — 프레임 설정, 모바일 품질 단계, 스트레스 측정(P3~P4)

**Files:**
- Create: `Assets/Scripts/MobileUI/MobilePerformance.cs`
- Modify: `ProjectSettings/QualitySettings.asset`(모바일 전용 단계 추가, 필요한 경우에만)

**Interfaces:**
- Produces: 모바일 시작 시 `Application.targetFrameRate`와 VSync 설정, 개발 빌드 전용 스트레스 도구(500마리+보스), FPS 표시

- [ ] **Step 1: 프레임 설정 부트스트랩과 개발 빌드 스트레스 도구 작성**

  `Assets/Scripts/MobileUI/MobilePerformance.cs`:

  ```csharp
  using UnityEngine;
  using UnityEngine.SceneManagement;
  using UnityEngine.UI;

  // 모바일 프레임 설정과 개발 빌드 전용 성능 측정 도구
  // Android는 기본 30 FPS로 제한되므로 60 FPS를 명시한다(VSync가 켜져 있으면 targetFrameRate가 무시되므로 끈다)
  public static class MobilePerformance {
      public const int TargetFrameRate = 60;

      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
      private static void ApplyFrameRate() {
          if (!Application.isMobilePlatform)
          {
              return;
          }

          QualitySettings.vSyncCount = 0;
          Application.targetFrameRate = TargetFrameRate;
      }

  #if DEVELOPMENT_BUILD || UNITY_EDITOR
      [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
      private static void CreateProbe() {
          SceneManager.sceneLoaded += (scene, mode) => EnsureProbe();
          EnsureProbe();
      }

      private static void EnsureProbe() {
          if (!MobilePlatform.IsMobile || Object.FindFirstObjectByType<MobilePerformanceProbe>() != null)
          {
              return;
          }

          if (Object.FindFirstObjectByType<ZombieSpawner>() == null)
          {
              return;
          }

          new GameObject("Mobile Performance Probe").AddComponent<MobilePerformanceProbe>();
      }
  #endif
  }

  #if DEVELOPMENT_BUILD || UNITY_EDITOR
  // 좌상단에 FPS(평균·최소 1% 구간 최저)와 적 수를 표시하고, STRESS 버튼으로 "500마리 + 보스" 상태를 만든다(개발 빌드 전용)
  public class MobilePerformanceProbe : MonoBehaviour {
      private Text label;
      private float elapsed;
      private int frames;
      private float worstFrame;
      private string text = "";

      private void Awake() {
          Canvas canvas = gameObject.AddComponent<Canvas>();
          canvas.renderMode = RenderMode.ScreenSpaceOverlay;
          canvas.sortingOrder = 200;
          CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
          scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
          scaler.referenceResolution = new Vector2(1920f, 1080f);
          scaler.matchWidthOrHeight = 0.5f;
          gameObject.AddComponent<GraphicRaycaster>();

          RectTransform safe = MobileUIFactory.NewRect("Safe Area", transform);
          MobileUIFactory.Stretch(safe);
          safe.gameObject.AddComponent<SafeAreaFitter>();

          label = MobileUIFactory.NewText("FPS", safe, "", 30, Color.yellow, TextAnchor.UpperLeft);
          label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, 1f);
          label.rectTransform.pivot = new Vector2(0f, 1f);
          label.rectTransform.anchoredPosition = new Vector2(24f, -220f);
          label.rectTransform.sizeDelta = new Vector2(700f, 160f);

          Image background = MobileUIFactory.NewImage("Stress Button", safe, MobileUIFactory.Panel);
          RectTransform rect = background.rectTransform;
          rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
          rect.pivot = new Vector2(0f, 1f);
          rect.anchoredPosition = new Vector2(24f, -380f);
          rect.sizeDelta = new Vector2(240f, 80f);
          Text buttonText = MobileUIFactory.NewText("Label", rect, "STRESS", 32, MobileUIFactory.Light, TextAnchor.MiddleCenter);
          MobileUIFactory.Stretch(buttonText.rectTransform);
          TouchButton button = background.gameObject.AddComponent<TouchButton>();
          button.background = background;
          button.onDown = StartStress;
      }

      private void Update() {
          float dt = Time.unscaledDeltaTime;
          elapsed += dt;
          frames++;
          worstFrame = Mathf.Max(worstFrame, dt);

          if (elapsed >= 1f)
          {
              text = "FPS " + (frames / elapsed).ToString("F1") + "  worst " + (1f / Mathf.Max(worstFrame, 0.0001f)).ToString("F0")
                  + "\nzombies " + Zombie.alive.Count + "  bosses " + Zombie.bosses.Count;
              elapsed = 0f;
              frames = 0;
              worstFrame = 0f;
          }

          label.text = text;
      }

      // 몬스터를 빠르게 늘리고 보스를 곧 등장시키며 플레이어가 죽지 않게 해 부하 상태를 재현한다
      private void StartStress() {
          ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
          if (spawner != null)
          {
              spawner.waveIntervalMin = 0.3f;
              spawner.waveIntervalMax = 0.6f;
              spawner.baseWaveZombieCount = 60;
              spawner.zombieCountIncreasePerWave = 10;
              spawner.maxConcurrentZombies = 500;
          }

          GameManager manager = GameManager.instance;
          if (manager != null)
          {
              manager.startElapsedMinutes = Mathf.Max(manager.startElapsedMinutes, 9.95f); // 곧 10분 보스가 등장
          }

          PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
          if (health != null)
          {
              health.startingHealth = 1000000f;
              health.RestoreHealth(1000000f);
          }
      }
  }
  #endif
  ```

  주의: `PlayerHealth.healthSlider.maxValue`가 갱신되지 않아 HUD 체력바가 이상해 보일 수 있다(측정 도구라 허용). `ZombieSpawner`가 `startElapsedMinutes` 변경을 즉시 반영하는지는 보스 등장으로 Step 3에서 확인한다.

- [ ] **Step 2: 에디터 시험**

  강제 모바일로 플레이해 FPS 라벨과 STRESS 버튼이 보이고, STRESS를 누르면 수십 초 안에 좀비가 500마리 근처까지 늘어나고 약 6초 뒤 보스가 소환되는지 확인한다(`zombies`, `bosses` 값). 에디터 FPS는 참고용이다(실기기 값만 기준으로 쓴다).

- [ ] **Step 3: 실기기 측정(개발 빌드, 두 기기)**

  Task 12 Step 4와 같은 방식으로 개발 빌드 APK를 다시 만들어 설치한다. 각 기기에서: 게임 시작 → STRESS 누름 → `zombies`가 500 근처이고 `bosses`가 1일 때 60초간 FPS(평균, worst)와 기기 발열을 기록한다. `adb shell dumpsys gfxinfo com.yws.urbansurvival`, Unity Profiler(Development Build 연결 시) 또는 화면의 FPS 라벨로 CPU/GPU 병목을 구분한다. 결과를 작업 로그의 표로 남긴다: 기기, 평균 FPS, worst, 병목(CPU·GPU), 발열 메모.

  판정: 평균이 60 FPS 이상이면 통과, 30~60이면 Step 4로 최적화, 30 미만이면 목표 미달로 사용자에게 보고하고 최적화 후보를 제안한다(동시 적 수 변경은 밸런스 결정이므로 사용자 승인 없이 하지 않는다).

- [ ] **Step 4: 병목이 확인된 항목만 모바일 전용 품질 단계로 조정**

  병목이 확인된 항목만 `QualitySettings`에 `Mobile` 단계를 추가해 적용한다(예: Shadow Distance·Shadow Resolution 축소, 렌더 스케일 0.8~0.9, 후처리 일부 끄기, 파티클 최대 수). Android의 기본 품질 단계를 `Mobile`로 지정한다(`QualitySettings.asset`의 플랫폼별 기본 레벨). PC 단계(Very Low~Ultra)의 값은 수정하지 않는다. 변경 전후 FPS를 표로 기록한다. 변경 후 PC 에디터에서 Quality 설정을 비교해 PC 단계 값이 그대로인지 확인한다(I05). 설정 창의 Graphics 드롭다운에 `Mobile` 단계가 보이는 것이 정상인지, 모바일에서 다른 단계로 바꿀 수 있어도 괜찮은지를 로그에 적고 사용자에게 한 줄로 확인한다.

  `QualitySettings.asset`은 Unity가 재저장하는 파일이므로 의도한 `Mobile` 단계 추가 줄만 커밋한다(`git add -p`).

- [ ] **Step 5: 30분 연속 플레이(I07)**

  두 기기 중 최소 한 기기에서 개발 빌드가 아닌 일반 빌드로 30분 이상 실제 플레이(또는 무적 스트레스 상태)를 진행한다. 입력 고착, 메모리 급증(`adb shell dumpsys meminfo com.yws.urbansurvival`로 시작·종료 값 비교), 크래시, 발열을 기록한다. 이 항목은 실기기 사용자 협조가 필요하므로 불가능하면 "미검증"으로 보고한다.

- [ ] **Step 6: Commit**

  ```bash
  git add Assets/Scripts/MobileUI/MobilePerformance.cs Assets/Scripts/MobileUI/MobilePerformance.cs.meta
  git add -p ProjectSettings/QualitySettings.asset
  git commit -m "feat: 모바일 프레임 설정과 개발 빌드 성능 측정 도구, 모바일 품질 단계" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  ```

---

## Task 14: 스토어 준비 — 아이콘·버전·서명·AAB·체크리스트(P4)

**Files:**
- Create: `docs/superpowers/plans/mobile-store-checklist.md`
- Modify: `ProjectSettings/ProjectSettings.asset`(아이콘, 버전, 서명 설정 일부)
- 로컬 전용(커밋 금지): `Keystore/urbansurvival-upload.keystore`

- [ ] **Step 1: 버전 정책 확정**

  `bundleVersion`(사용자에게 보이는 버전)을 `1.0.0`, `AndroidBundleVersionCode`를 `1`로 한다. 출시(업로드)마다 `AndroidBundleVersionCode`를 1씩 올린다는 규칙을 체크리스트 문서에 적는다. 사용자가 다른 정책을 원하면 그 값을 따른다(Task 시작 시 한 줄로 확인).

- [ ] **Step 2: 앱 아이콘·스플래시**

  사용자에게 앱 아이콘 원본(권장 1024×1024 PNG, 적응형 아이콘용 전경·배경 분리 가능 시 각각 432×432)을 요청한다. 제공되지 않으면 임시 아이콘을 만들지 않고 Unity 기본 아이콘 상태임을 체크리스트에 "미완료: 사용자 제공 필요"로 적는다. 제공받으면 `PlayerSettings.SetIcons`(Android 레거시·적응형·라운드 종류)로 적용하는 Editor 메뉴를 `MobileEditorMenu`에 추가하고 `Apply Android Icons` 메뉴로 실행한다. 스플래시는 Unity 기본 스플래시 정책(개인 사용자에게 로고 필수 여부는 Unity 라이선스에 따름)을 확인하고 사용자에게 알린다.

- [ ] **Step 3: 업로드 키 생성 방법 안내(사용자 직접 보관)**

  키스토어 비밀번호는 사용자가 정한다. 사용자가 직접 Unity `Project Settings > Player > Publishing Settings > Keystore Manager`에서 생성하거나 다음 명령을 쓰도록 안내하고, 비밀번호·키 파일은 대화에 붙여 넣지 않게 요청한다. 키 파일 경로는 `D:\work\Zombie\Keystore\`(`.gitignore`에 `/Keystore/` 있음)로 한다.

  ```bash
  "C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe" -genkeypair -v -keystore Keystore/urbansurvival-upload.keystore -alias urbansurvival-upload -keyalg RSA -keysize 2048 -validity 10000
  ```

  Unity에 키스토어 경로·별칭·비밀번호를 입력하는 작업은 비밀번호가 필요하므로 사용자가 직접 한다(비밀번호를 대화·파일에 기록하지 않는다). 사용자가 키를 백업(예: 안전한 외부 저장소)했는지 체크리스트에서 확인한다. Play App Signing 사용을 기준으로 이 키는 "업로드 키"다.

- [ ] **Step 4: 서명된 릴리스 AAB 빌드**

  사용자가 Unity에 키스토어 정보를 설정한 뒤: `buildAppBundle = true`, 개발 빌드 옵션 없음, IL2CPP, ARM64로 `Builds/Android/UrbanSurvival-1.0.0-1.aab`를 빌드한다(`manage_build`). 오류 시 `read_console`로 해결한다.

- [ ] **Step 5: AAB 조건 검증**

  `bundletool`이 없으면 사용자 승인 후 공식 릴리스를 내려받거나(다운로드 전에 파일명·출처·크기를 알리고 승인받는다), 대신 AAB(zip)에서 매니페스트를 확인한다. 최소한 다음을 확인하고 로그에 기록한다: 패키지명 `com.yws.urbansurvival`, `targetSdkVersion` ≥ 36, `minSdkVersion` 25, 네이티브 라이브러리가 `arm64-v8a`만 있는지, 서명 여부(`jarsigner -verify`로 확인 가능). 확인하지 못한 항목은 "미검증"으로 둔다.

- [ ] **Step 6: Play Console 준비 체크리스트 문서 작성**

  `docs/superpowers/plans/mobile-store-checklist.md`에 다음을 항목별 `- [ ]`로 작성한다(상태는 실제 확인 결과로 채운다): 사용자 직접 처리 항목(개발자 계정·등록비·신원 확인, 앱 생성, 개인정보처리방침 URL, Data Safety, 콘텐츠 등급, 카테고리·대상 연령, 스토어 설명, 아이콘 512×512, 스크린샷, 피처 그래픽 1024×500, 테스트 트랙, 최종 Production 제출), 업로드 키 백업 확인, 개인정보·데이터 수집 점검(이 게임은 `PlayerPrefs`만 저장하고 외부 전송·광고·계정이 없는지 코드로 확인해 기록: `Grep "UnityWebRequest|Analytics|Advertisement"`와 `Packages/manifest.json`의 `com.unity.analytics` 사용 여부를 확인하고, Analytics 패키지가 활성이라면 Data Safety에 영향이 있을 수 있음을 적는다), 신규 개인 계정 비공개 테스트 요건(등록 직전에 Google Play 공식 문서로 최신 요건 재확인, 현재 모바일 기획서 기준 12명·14일), 등록용 이미지 목록, 버전 코드 증가 규칙, 타깃 API·AAB·64비트 요건 확인 결과.

- [ ] **Step 7: 작업 로그와 최종 보고**

  `D:\Codex\Log\20261006_(모바일포팅)_Log.md`(날짜가 바뀌면 새 파일)에 P0~P4 결과, 실기기 검수 표, 성능 표, 미검증 항목(실기기·노치·멀티터치·30분 플레이 중 확인하지 못한 것)을 정리한다. 사용자에게 완료·미완료·미검증을 구분해 보고한다. `D:\Codex\Log` 쓰기 권한이 없으면 필요한 권한을 요청한다.

- [ ] **Step 8: Commit**

  ```bash
  git add docs/superpowers/plans/mobile-store-checklist.md ProjectSettings/ProjectSettings.asset Assets/Editor/MobileEditorMenu.cs
  git status --short   # 키스토어·AAB·Builds가 목록에 없는지 확인
  git commit -m "chore: 모바일 스토어 준비(버전·아이콘 설정, Play Console 체크리스트)" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
  git push
  ```

  푸시는 `SideProject-Mobile` 브랜치만 대상이며 사용자가 이미 승인했다. 푸시 전에 `git branch --show-current`가 `SideProject-Mobile`인지 한 번 더 확인한다.

---

## Self-Review 기록

**스펙 커버리지**
- §1 목표·§11 단계(P0~P4): Task 0, Task 1~11(P1·P2), Task 12(P3), Task 13·14(성능·P4).
- §3 D1(발사 차단 우회): Task 7(`!mobile && ... IsPointerBlockedForFire`). D2: Task 6. D3: Task 9 `BuildPauseButton`. D4: Task 7 오토 에임 분기. D5: Task 11 Step 1.
- §4 아키텍처: MobileInputState(1), PlayerInput/PlayerMovement(7), AutoAimTargeting(4), TouchJoystick·TouchButton·SafeAreaFitter(8), MobileTouchOverlay·슬롯 터치(9), 바인더(10), MobilePlatform(1). 슬롯 터치는 기존 `WeaponHUD`에 아이콘이 없고 현재 무기 이름만 있어 오버레이 안에 독립 슬롯 버튼으로 만든다(스펙 §4.1 "WeaponHUD 슬롯 터치"를 이 방식으로 대체. 기존 HUD 무기 아이콘 재사용은 YAGNI로 제외).
- §5 입력 규칙·래치: Task 7(Step 7 검증), Task 9(정지 시 숨김), Task 10·9(모드 전환 `RequireFireRelease`).
- §6 조준 모드 + 설정: Task 4·5·7·9·10.
- §7 UI·문구·QUIT: Task 9·11.
- §8 플랫폼 정책: Task 1.
- §9 Android 빌드·성능: Task 12·13.
- §10 Git: Global Constraints, 각 Task 커밋.
- §12 테스트: Task 1~5 자동 테스트, Task 7·9·12 수동·실기기.
- §14 사용자 직접 처리: Task 14 체크리스트.

**알려진 격차(의도적)**
- 멀티터치와 노치는 에디터에서 검증할 수 없어 Task 12 실기기에서 확인한다.
- 오토 에임이 벽 뒤 적을 고르는 문제는 스펙대로 시야 검사를 넣지 않았다(실플레이 불편 시 별도 작업).
- 메뉴 버튼 크기 조정은 Task 11 Step 5의 확인 결과에 따라 필요할 때만 추가한다.

**타입 일관성 확인**: `AimCandidate(Vector3, bool)`, `AimResult{valid,hasTarget,direction}`, `AutoAimTargeting.Select(origin, candidates, range, moveDirection, lastDirection)`, `JoystickMath.Evaluate/ToWorldDirection`, `TwinStickFireTracker.Update/Held/Down/Reset`, `MobileInputState.*`, `MobileAimSettings.Mode/Changed/PrefsKey`, `PlayerInput.aimWorldDirection/hasAimWorldDirection/RequireFireRelease`, `PlayerShooter.CurrentSlotIndex/IsSlotUnlocked`은 정의한 Task와 사용하는 Task에서 같다.
