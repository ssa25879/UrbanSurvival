# Urban Survival 모바일 포팅 인수인계

- 작업 브랜치: `SideProject-Mobile` (원격 등록 완료)
- 기준 문서: 설계 `docs/superpowers/specs/2026-10-06-mobile-port-design.md`, 계획 `docs/superpowers/plans/2026-10-06-mobile-port.md`
- 실행 방식: 병행(직접 구현 + 작업량이 큰 Task는 서브에이전트). Unity 에디터가 하나라 서브에이전트는 순차 실행한다.
- 이 문서는 Task가 하나 끝날 때마다 항목을 추가한다. 확인하지 못한 항목은 "미검증"으로 적는다.
- 백업: 사용자가 Git 브랜치로 갈음하기로 확인했다(2026-10-06). 파일 백업본은 만들지 않는다.

## 에디터 사용 메모
- `mcp__UnityMCP__execute_code`(CodeDom, C# 6)에서는 `Object`가 모호하므로 `UnityEngine.Object.FindFirstObjectByType<T>()`처럼 완전한 이름을 쓴다.
- 플레이 모드 진입 직후 첫 호출은 도메인 리로드로 타임아웃이 날 수 있다. 몇 초 뒤 다시 호출한다.

- **Rider 후처리 훅 주의:** 기존 `.cs` 파일을 Edit하면 Rider가 파일 전체를 재포맷(중괄호 줄바꿈 등)해 diff가 부풀 수 있다. 기존 파일을 고친 뒤에는 반드시 `git diff --stat`으로 의도한 줄만 바뀌었는지 확인한다. 이 저장소 `.cs`는 CRLF+BOM이라 MSYS `sed -i`도 줄바꿈을 망가뜨린다. 정확한 치환은 PowerShell `[IO.File]::ReadAllText/WriteAllText`(UTF8 BOM, `r`n 사용)로 한다.

- **Unity CLI 병행(사용자 요청, 2026-10-06):** `unity`(1.0.0-beta.11)가 설치되어 있고 실행 중인 에디터(포트 7800)에 연결된다. 접두는 `unity command --caller plugin --skill unity-cli --no-banner --result-only <명령> --<인자> <값>`이다. 쓸 수 있는 명령: `editor_play`/`editor_stop`, `open_scene --path`, `eval '<C#>'`(Roslyn), `run_tests --mode EditMode`(동기, 결과 즉시 반환), `console`, `get/set_player_settings`, `get/set_quality_settings`, `switch_build_target`, `build`, `build_status`, `screenshot`, `get_performance_stats`. Task 12(Android 설정·빌드)와 Task 13(품질·성능)에서 활용한다. `unity build`/`unity test`는 별도 에디터를 띄우므로 이미 열린 프로젝트에서는 `unity command build`(열린 에디터 사용)를 쓴다.

## Task 0 — 브랜치·기준 확보 (완료)

- 변경: `.gitignore`에 `*.keystore`, `*.jks`, `keystore.properties`, `/Keystore/` 추가.
- 브랜치 `SideProject-Mobile`, 원격 등록 확인.
- Android 빌드 모듈 확인: Unity 6000.3.19f1 `PlaybackEngines/AndroidPlayer`에 SDK, NDK, OpenJDK가 있다.
- PC 기준(에디터 플레이, `UrbanSurvival.unity`): 플레이 진입 정상, 권총 탄창 10발·Ready, 5초 안에 좀비 소환, 메인 카메라 존재.
- **미검증(사용자 수동 확인 필요):** WASD 이동, 마우스 조준, 좌클릭 발사, R 재장전, 1~4 교체, Esc 일시정지. 키보드·마우스 입력은 자동으로 재현하지 못했다. 이 항목은 Task 7 이후 PC 회귀 확인의 비교 기준이다.
- 활성 빌드 대상은 아직 StandaloneWindows64이다(Task 12에서 Android로 전환).

## Task 1 — MobileCore 어셈블리, MobileInputState, MobilePlatform (완료)

- 신규 어셈블리 `UrbanSurvival.MobileCore`(`Assets/Scripts/MobileCore/`, 자동 참조). 게임 코드에 의존하지 않는 순수 로직을 담는다. `Assembly-CSharp`이 자동으로 참조하고, 테스트 어셈블리 `UrbanSurvival.EditMode.Tests`에 참조를 추가했다.
- `MobileInputState`: 터치 UI → `PlayerInput` 정적 브리지(`Move`, `AimStick`, `FireHeld`, 요청/소비 방식의 `FireDown`·`Reload`·`Swap`, `ResetAll`).
- `MobilePlatform.IsMobile`: 빌드에서는 `Application.isMobilePlatform`, 에디터에서는 `ForceMobileInEditor`(EditorPrefs)로도 켠다. 에디터 메뉴는 Task 7에서 추가한다.
- 테스트: `MobileInputStateTests` 8건. 구현 전에 컴파일 실패(`MobileInputState` 없음)를 확인한 뒤 구현했다. EditMode 전체 22/22 통과(기존 `SpecDataTests` 14건 포함).
- 다음 Task가 쓰는 것: `MobileInputState`, `MobilePlatform`.

## Task 2 — JoystickMath (완료)

- `JoystickMath.Evaluate(offset, radius, deadZone)`: 크기 1 이하, 데드존 이하 0, 데드존 위는 0~1로 다시 편다. 반지름 0 이하와 데드존 1 이상(0.95로 제한)도 NaN 없이 처리한다.
- `JoystickMath.ToWorldDirection(stick, camForward, camUp)`: `PlayerMovement.GetMoveDirection`과 같은 카메라 기준 평면 변환. 수직 탑뷰는 `camera.up`을 위쪽으로 쓴다.
- 테스트 11건(`JoystickMathTests`). 구현 전 컴파일 실패 확인 후 통과.

## Task 3 — TwinStickFireTracker (완료)

- 조준 스틱 출력이 0이 아니면 눌림. 데드존 밖으로 처음 나간 프레임에만 `Down`, 유지 중에는 `Held`만 true. 중앙 복귀 후 다시 당기면 새 `Down`. `Reset()`은 상태를 비운다.
- 테스트 5건(`TwinStickFireTrackerTests`).

## Task 4 — AutoAimTargeting (완료)

- `AutoAimTargeting.Select(origin, candidates, range, moveDirection, lastDirection)`: 사거리 안 가장 가까운 살아 있는 적 → 이동 방향 → 마지막 방향 순. 수평(XZ) 거리, 사거리와 같은 거리는 대상, range 0 이하는 대상 없음, 겹친 적(거리 0)은 건너뛴다. 결과는 `AimResult{valid, hasTarget, direction}`.
- 테스트 11건(`AutoAimTargetingTests`, 500마리 후보 포함).
- 알려진 한계(설계 결정): 벽 뒤 적도 후보다. 실제 피해는 기존 총구·Raycast 판정이 막는다.

## Task 5 — MobileAimSettings (완료)

- `MobileAimMode { AutoAim = 0, TwinStick = 1 }`, `MobileAimSettings.Mode`(PlayerPrefs 키 `MobileAimMode`, 기본 AutoAim, 잘못된 저장값은 AutoAim, 같은 값이면 `Changed` 미발생).
- 테스트 4건(`MobileAimSettingsTests`, PlayerPrefs 원상 복구).
- 설정 창 토글 연결은 Task 10.

**여기까지 순수 로직(MobileCore) 완료. EditMode 53/53 통과.**

## Task 6 — Zombie.alive 목록과 PlayerShooter 읽기 접근자 (완료)

- `Zombie.alive`(정적 `List<Zombie>`): `Setup`에서 등록, `Die()`와 `OnDestroy()`에서 제거. 보스 포함, 시체는 제외. `ZombieSpawner.CreateZombie`가 항상 `Setup`을 호출하므로 모든 소환 경로가 등록된다.
- `PlayerShooter.CurrentSlotIndex`(읽기 전용)와 `IsSlotUnlocked(int)`: 슬롯 UI 표시용. 전투 로직은 바꾸지 않았다.
- 확인: 구현 전에 `Zombie.alive` 컴파일 실패를 확인했다. 에디터 플레이에서 살아 있는 좀비 13마리가 `alive` 13개와 일치했고 시체가 목록에 없었다. 한 마리를 `Die()` 처리하자 13 → 12로 줄고 목록에서 빠졌다. 권총 슬롯 보유 true, 소총 false, 범위 밖(9) false. EditMode 53/53 통과, 콘솔 오류 없음.
- 자동 테스트는 만들지 않았다(MonoBehaviour와 NavMesh 필요). 계획서대로 플레이 모드 확인으로 대신했다.
- 서식 사고: Rider 훅이 `Zombie.cs`를 재포맷해 diff가 80줄로 늘었다. 원복하고 PowerShell 정확 치환으로 의도한 8줄만 반영했다(위 "Rider 후처리 훅 주의" 참고).

## Task 7 — PlayerInput 모바일 경로와 PlayerMovement 조준 방향 (완료)

- `PlayerInput`: PC 입력은 `ReadDesktopInput()`으로 옮겼고 동작은 그대로다. 모바일은 `ReadMobileInput()`이 `MobileInputState`를 읽어 같은 출력(`move`, `rotate`, `fire`, `fireDown`, `reload`, `swapToSlot1~4`)과 새 `aimWorldDirection`·`hasAimWorldDirection`을 낸다. 마우스 포인터 차단(`IsPointerBlockedForFire`)은 PC에서만 거친다(터치 발사 버튼은 UI 자체라 막히기 때문).
- 오토 에임은 발사를 누르는 동안 0.1초마다(눌린 첫 프레임은 즉시) 재탐색한다. 쌍둥이 스틱은 `TwinStickFireTracker`와 `JoystickMath.ToWorldDirection`을 쓴다. `RequireFireRelease()`로 조준 모드 전환 직후 오발을 막는다(Task 9, 10에서 호출).
- `PlayerMovement.GetAimDirection`: `hasAimWorldDirection`이면 마우스 Ray 투영 대신 그 방향을 쓴다. 전투 로직(`PlayerShooter`, `Gun`)은 수정하지 않았다.
- `Assets/Editor/MobileEditorMenu.cs`: 메뉴 `Urban Survival/Mobile/Force Mobile Input In Editor`로 에디터에서 모바일 입력을 강제한다(EditorPrefs, 빌드에는 영향 없음). **현재 꺼져 있다.** 켠 채로 두면 에디터 PC 입력이 막힌다.
- 확인(에디터 플레이, `execute_code`로 입력 주입, 플레이어 무적 처리):
  - PC 경로: 강제 모바일 끔 → `hasAimWorldDirection=false`, `aimPosition`은 마우스 좌표.
  - 오토 에임: 조준 방향이 사거리 안 가장 가까운 좀비 방향과 일치(`(-0.93, 0, -0.36)`), 캐릭터가 그 방향을 봤고 탄창 10→9. 발사를 떼면 `fire=false`.
  - 쌍둥이 스틱: 스틱 +X → 카메라 기준 오른쪽(`(0.71, 0, -0.71)`), `fire` 유지 중 `fireDown=false`.
  - 일시정지: 입력이 모두 초기화되고(`fire=false`, `FireHeld=false`) 탄창 변화 없음. 재개 후 래치가 해제되고 새로 누르면 발사(9→8).
  - 무기 교체: `RequestSwap(1)`로 권총 → AK(소총) 교체 확인.
  - 재장전 후 계속 누름: 소총 연사 중 `RequestReload` → 재장전 완료 후 `fire=true`(계속 누름)인데 탄창이 30으로 유지됨(자동 발사 없음). 뗐다가 다시 누르니 탄창 감소.
  - EditMode 53/53 통과, 콘솔 오류 없음.
- **미검증:** PC 실제 키보드·마우스 조작 회귀(WASD, 마우스 조준, 좌클릭, R, 1~4, Esc). 코드 경로는 그대로이고 입력 값 읽기 줄이 같은 순서로 옮겨졌다. 사용자가 직접 한 판 확인해 주면 좋다. 포커스 상실·복귀(`OnApplicationFocus/Pause`) 경로도 에디터에서는 재현하지 못했다.
- 시험 중 한 번 플레이어가 서 있다가 사망해(게임 오버) 입력이 전부 0이 된 것을 래치 문제로 오해했다. 게임 오버/일시정지 때 `MobileInputState.ResetAll()`이 호출되는 것은 설계 의도다.
- 서식: `PlayerInput.cs`, `PlayerMovement.cs`는 Rider 훅을 피해 PowerShell로 직접 써서 diff가 의도한 줄(+147/-9)로만 나왔다.

## Task 8 — 터치 UI 위젯(스틱·버튼·세이프 에리어) (완료)

- `Assets/Scripts/MobileUI/`에 `MobileUIFactory`(원형 스프라이트 캐시, NewRect/NewImage/NewText/Stretch, 색 상수), `TouchJoystick`(플로팅 스틱, 포인터 ID 하나만 추적), `TouchButton`(누름/뗌, 자기 포인터만 해제, 밖으로 나가면 뗌), `SafeAreaFitter`를 추가했다. 계획서 코드와 동일하며 변경 없음.
- 확인: 구현 전 `MobileTouchOverlay` 참조가 컴파일 실패하는 RED 확인. 위젯 작성 후 강제 새로고침·컴파일, 콘솔 오류·경고 0건, `execute_code`로 세 위젯 타입 로드 확인.
- 동작(포인터 독립성 등) 검증은 Task 9 오버레이와 함께 한다.

## Task 9 — MobileTouchOverlay (완료)

- `MobileTouchOverlay`: 모바일이고 씬에 `PlayerInput`이 있을 때만 런타임에 자동 생성되는 오버레이(이동 스틱, 발사 버튼/조준 스틱 전환, 재장전, 무기 슬롯 4개, 일시정지, 세이프 에리어). `MobileInputState`에만 값을 쓴다.
- **계획서 대비 변경(실제 문제 수정)**
  1. 스틱 바탕이 처음부터 보임: `AddComponent`의 `Awake` 시점에는 `baseImage`가 아직 연결 전이라 숨김이 무시됐다. `CreateStick`에서 연결 직후 `ResetStick()`을 호출해 숨긴다.
  2. 원형 버튼이 단색 앰버로 보임: 링을 버튼 배경의 자식으로 만들면 배경을 덮는다. 링을 형제(`<이름> Ring`)로 먼저 만들고 `PlaceBottomRight` 헬퍼로 같은 위치에 8px 크게 둔다. 이에 맞춰 조준 모드 전환 때 `fireRingObject`도 같이 숨긴다.
  3. HUD 겹침(스크린샷 확인): 일시정지 버튼은 미니맵 위라 `(-380,-24)`로 왼쪽으로, 무기 슬롯은 `620 - i*100`으로 내려 미니맵 아래에 둔다. 발사 버튼 `(-640,170)`, 재장전 `(-430,330)`으로 옮겨 무기 패널(우하단)과 겹치지 않게 했다.
- 확인(에디터, 강제 모바일, 플레이어 무적, 합성 `PointerEventData`):
  - 컴파일 오류·경고 0건, EditMode 53/53 통과(최종 코드 기준 재실행).
  - 오버레이 1개, 오토 에임: 발사 버튼 표시·조준 영역 숨김. 쌍둥이 스틱: 발사 버튼 숨김·조준 영역 표시, 전환 시 누르고 있던 `FireHeld` true → false, `AimStick` 0으로 초기화. 다시 오토 에임 복귀 확인.
  - 이동 스틱: 60px 드래그 `(0.37, 0)`, 멀리 드래그 크기 1.000, 놓으면 `(0,0)`.
  - 발사 버튼: 누르면 `FireHeld` true와 `FireDown` 요청, 떼면 false. R 버튼: `RequestReload` 큐잉 확인.
  - 슬롯: 잠긴 슬롯 2는 누름 `ConsumeSwap = -1`(색 Dim), `UnlockWeapon(Rifle)` 후 `ConsumeSwap = 1`.
  - 일시정지: 버튼으로 `isPaused` true, 다음 프레임에 컨트롤·일시정지 버튼 숨김, 기존 일시정지 메뉴(CONTINUE 버튼 클릭)로 재개 후 컨트롤 복귀.
  - 멀티 포인터: 이동 스틱 id1을 잡은 채 id2가 이동 영역을 눌러 드래그해도 값이 바뀌지 않음, id2를 뗀 뒤에도 스틱 유지. 이동 스틱 유지 중 발사 버튼(id3)·재장전(id4) 동시에 눌러도 이동값·`FireHeld` 유지. 다른 포인터(id9)의 누름/뗌은 발사 상태에 영향 없음, 자기 포인터로 떼야 `FireHeld` false.
  - 화면비: Game 뷰 1920x1080, 2340x1080, 1920x1200 스크린샷으로 확인. 버튼이 화면 밖으로 나가지 않고 미니맵·무기 패널·점수 패널과 겹치지 않음(처음 배치는 겹쳐서 위 3번대로 조정).
- 환경 복구: `ForceMobileInEditor` false, 플레이 종료, 저장한 조준 모드 PlayerPrefs 키 삭제, 시험용 Game 뷰 사이즈 제거, 씬 저장 없음.
- **미검증:** 실제 멀티터치(동시에 두 손가락)와 터치 감도, 노치·펀치홀(`Screen.safeArea`는 에디터에서 전체 화면), 실기기 터치 대상 크기(48dp 이상인지), 일시정지 중 손가락 누른 채 재개했을 때 오발 방지(Task 7 `RequireFireRelease`는 확인했으나 오버레이 경유 실손 확인은 못 함), 포커스 상실 경로.
- 시험 중 알게 된 점: 시작 위치 주변에 무기 픽업이 있어 슬롯이 금방 해금될 수 있다(잠긴 슬롯 시험은 `unlocked` 배열을 리플렉션으로 조정).

## Task 10 — 설정 창 조준 모드 토글과 모바일 설정 정리 (완료)

- `MobileAimModeSettingsBinder`(모듈 밖): 모바일에서만 씬 로드 때 `SettingsPanel`을 찾아 VSync 행을 복제해 "TWIN-STICK AIM" 토글 행을 만든다. 모바일에서 의미 없는 전체 화면·해상도·VSync 행은 숨긴다. 씬 파일은 수정하지 않았다. `GameSettingsKit` 모듈에는 게임 전용 타입을 넣지 않았다.
- `SettingsStore`(모듈, 플랫폼 일반 가드만): `ApplyDisplay`는 `Application.isMobilePlatform`이면 건너뛰고, `ApplyQuality`의 `vSyncCount` 적용도 모바일에서는 건너뛴다(vSync가 켜지면 `Application.targetFrameRate`가 무시되기 때문). diff는 +11/-1.
- 확인(에디터 강제 모바일, `UrbanSurvival.unity`):
  - 행 구성: Fullscreen·Resolution·VSync 꺼짐, Mobile Aim Mode 켜짐(스크린샷으로 레이아웃 정상, 창이 넘치지 않음).
  - 토글을 켜면 `MobileAimSettings.Mode=TwinStick`, PlayerPrefs `MobileAimMode=1`, 오버레이의 FIRE 버튼이 숨고 조준 영역이 켜진다.
  - 창을 닫았다 다시 열면 토글이 켜진 채로 유지되고, 플레이를 껐다 켜도 `TwinStick`이 유지된다(F12).
  - 인트로(`Intro.unity`)에서도 같은 행 구성이 나오고 오버레이는 만들어지지 않는다.
  - EditMode 53/53 통과.
- 시험 뒤 PlayerPrefs 키 삭제, 강제 모바일 끔, 씬 저장 안 함.
- **미검증:** `Application.isMobilePlatform` 가드(에디터에서는 항상 false)는 실기기에서 확인해야 한다. 해상도·전체 화면이 건드려지지 않는지, vSync가 0으로 유지되는지는 Task 12·13에서 본다. 설정 창의 Graphics 드롭다운에는 아직 모바일 전용 품질 단계가 없다(Task 13).
- 알아둘 점: 설정 창을 일시정지 메뉴 밖에서 직접 열면 오버레이 버튼이 창 위에 겹쳐 보인다(시험용으로만 가능한 경로). 실제 흐름은 일시정지 메뉴에서 열기 때문에 컨트롤이 숨는다.

## Task 11 — 모바일 문구·메뉴 대응 (완료)

- `ReloadIndicator`: 모바일에서 "RELOAD" / "NO AMMO"(키 안내 `[R]`·`[1]` 제거). PC 문구는 그대로.
- `MobileTutorialText.Convert(body, mode)`(MobileCore, 순수 함수): 인트로 튜토리얼의 PC 전용 조작 두 줄과 재장전 팁 한 문장만 모바일 문구로 바꾼다. 서식 태그와 나머지 가이드는 유지하고, 해당 문장이 없는 연습 튜토리얼은 그대로 둔다. `TutorialPopup`이 모바일에서만 이 함수를 거친다. 조준 모드(오토 에임/쌍둥이 스틱)에 따라 문구가 다르다.
- `MobileQuitHider`: 모바일에서 `onClick`에 `QuitGame`이 연결된 버튼을 씬 로드 때 끈다(인트로·게임 오버 화면의 QUIT).
- **안드로이드 한글 폰트 대비:** `TutorialPopup.ApplyFont`가 Windows 폰트 이름(`Malgun Gothic` 등)만 찾아서 Android에서는 한글이 깨질 수 있다. 후보 이름 뒤에 `Noto Sans CJK KR`, `Noto Sans CJK`, `sans-serif`를 추가했다(PC는 앞의 이름이 먼저 걸려 동작이 같다).
- 테스트: `MobileTutorialTextTests` 5건. 구현 전에 컴파일 실패 확인 후 통과. EditMode 58/58(MCP와 CLI `run_tests` 모두 확인).
- 확인(에디터 강제 모바일):
  - 인트로: QUIT 버튼 꺼짐. 실제 튜토리얼 본문(`TutorialSeen_Game`)을 변환하면 PC 전용 단어(WASD, 마우스, 좌클릭, ESC, R 키)가 남지 않는다.
  - 게임 씬: 탄창 0 + 예비탄 20 → "RELOAD", 예비탄 0 → "NO AMMO". 강제 모바일을 끄면 "RELOAD  [R]", "NO AMMO  [1]"로 PC 문구가 유지된다.
- **미검증:**
  - **안드로이드 한글 렌더링:** `CreateDynamicFontFromOSFont`가 실기기에서 한글 글꼴을 찾는지 확인하지 못했다. 튜토리얼이 네모(□)로 보이면 한글이 포함된 폰트 에셋을 프로젝트에 넣는 별도 작업이 필요하다(Task 12 실기기 검수 항목).
  - 게임 오버·결과 화면의 QUIT 숨김은 코드 경로가 인트로와 같지만 해당 화면에서 직접 보지는 못했다.
  - 터치 메뉴 버튼 크기(약 48dp)와 게임 오버·일시정지·결과 화면의 터치 조작은 실기기에서 확인해야 한다. 버튼 크기 조정 코드는 필요성이 확인되지 않아 추가하지 않았다.

## Task 12 — Android 빌드 설정과 첫 개발 빌드 (진행 중: 빌드·설치·실행 성공, 사용자 실기기 검수 대기)

- `Urban Survival/Mobile/Apply Android Settings`(`MobileEditorMenu.ApplyAndroidSettings`, 커밋 `d593480`)로 설정을 코드로 적용했다: 회사명 `YWS`, 제품명 `Urban Survival`, 패키지명 `com.yws.urbansurvival`, 가로 화면만(세로 자동 회전 끔), IL2CPP, ARM64, 최소 API 25, 타깃 API 36(`AndroidApiLevel36`), AAB 출력, Android 스크립팅 정의 `URP_COMPATIBILITY_MODE` 추가. `ProjectSettings.asset`에는 이 변경만 커밋했고 원래 있던 `runInBackground: 1` 변경은 제외했다.
- **주의(사용자 결정 반영):** 회사명·제품명을 바꾸면 Windows 빌드와 에디터의 PlayerPrefs 저장 위치(레지스트리 `YWS/Urban Survival`)가 달라진다. 이전 `DefaultCompany/Zombie`에 저장된 설정·튜토리얼 확인 여부·연습 최고 기록은 새 위치에서 보이지 않는다(삭제된 것은 아니다).
- 에디터 활성 빌드 대상을 사용자 승인 후 **Android로 전환**했다(재임포트 약 11분). 지금도 Android 상태다. PC(Windows) 빌드를 하려면 `unity command switch_build_target --target StandaloneWindows64 --confirm true`로 되돌려야 한다.
- 개발용 APK: Android SDK 36 플랫폼과 NDK가 설치되어 있어 추가 다운로드 없이 빌드됐다. `Builds/Android/UrbanSurvival-dev.apk`(179 MB, 빌드 시간 약 22분, Development + AllowDebugging, `buildAppBundle=false`로 임시 변경). **`buildAppBundle`은 현재 false다. Task 14에서 AAB를 만들 때 다시 true로 켠다.**
- 기기 설치·실행: `adb`로 연결된 **Lenovo `TB373FU`**(XiaoxinPad 2025로 추정, Android 15 / API 35, Mali-G615, OpenGL ES 3.2, 가로 2944×1840 = 16:10)에 설치하고 실행했다. 로그에서 `Company Name: YWS`, `Product Name: Urban Survival` 확인. 크래시 없음.
- 화면 확인(앱 실행 캡처 1장, 확인 후 삭제): 이동 스틱, FIRE, R, 무기 슬롯 4칸(미획득 슬롯은 흐리게), 일시정지, 미니맵, 점수·HP·웨이브 패널이 겹치지 않고 보인다. 개발 빌드 FPS 표시 `FPS 60.0 worst 60 / zombies 9 bosses 0`(좀비 9마리일 때).
- **발견한 문제(미해결):**
  - 로그 `Failed to load native plugin: Unable to lookup library path for '_burst_0_0'`: Burst 네이티브 라이브러리를 못 찾는다는 메시지. 게임은 동작하지만 Burst 최적화가 빠졌을 수 있다. 원인 미확인(릴리스 빌드에서 재확인 필요).
  - 로그 `Hidden/InternalErrorShader`가 `lanternDouble` 오브젝트(`Assets/Materials/light.mat`, 셰이더 `Unlit/Color`)에 적용됨: Android 빌드에서 이 셰이더가 오류 셰이더로 대체되어 가로등 전구가 분홍색일 수 있다. 외관 문제로 보이며 `light.mat`을 URP Unlit으로 바꾸는 것을 검토한다(기존 프로젝트 자체 에셋 변경이라 사용자 확인 필요).
- 한 대(TB373FU)만 연결됐다. **S26 Ultra는 연결되지 않아 미검증이다.**
- **미검증(사용자 실기기 조작 필요):** 이동 스틱·발사·재장전·무기 교체·일시정지 동작, 멀티터치, 오토 에임/쌍둥이 스틱 전환, 설정 창 토글, 한글 튜토리얼 렌더링, 노치/펀치홀(S26 Ultra), 메뉴 터치 크기, 게임 오버·결과 화면의 QUIT 숨김, 앱 전환·뒤로 가기 동작. 플레이 중 입력은 adb로 재현할 수 없다.

## Task 13 — 모바일 성능 (진행 중: 도구 작성·기기 동작 확인, 스트레스 측정 대기)

- `MobilePerformance.cs`: 모바일에서 `QualitySettings.vSyncCount=0`, `Application.targetFrameRate=60`(Android 기본 30 FPS 제한 해제). 개발 빌드와 에디터 전용 `MobilePerformanceProbe`가 좌상단에 FPS(평균/최저), 좀비·보스 수를 표시하고 `STRESS` 버튼으로 "좀비 500마리 + 보스(곧 10분 보스 등장) + 플레이어 무적" 상태를 만든다. 릴리스(비개발) 빌드에는 포함되지 않는다.
- 기기 확인: 태블릿 개발 빌드에서 FPS 표시가 동작하고 평균 60.0이 나왔다(좀비 9마리).
- **미완료:** `STRESS` 상태(500마리+보스)에서의 FPS 측정, 모바일 전용 품질 단계 추가 여부 결정(병목이 확인될 때만), 30분 연속 플레이.

## Task 9b — 터치 버튼 스타일 개선 (완료)

- 사용자 피드백(실기기 태블릿): 스틱은 좋지만 FIRE·R·무기 슬롯·일시정지가 기존 HUD와 따로 논다. 이동·조준 스틱은 그대로 두고 나머지를 HUD와 같은 시각 언어로 바꿨다.
- 변경 파일: `MobileUIFactory.cs`, `TouchButton.cs`, `MobileTouchOverlay.cs`(`MobilePerformance.cs`는 수정하지 않음, 단 `NewText` 기본 글꼴이 바뀌어 FPS 라벨·STRESS 버튼 글꼴도 HUD 글꼴로 나온다).
- 스타일:
  - `MobileUIFactory`가 씬의 `WeaponHUD`(무기 패널)에서 패널 스프라이트·Image.Type(Sliced)·pixelsPerUnitMultiplier·글꼴(Kenney Future Narrow)을 읽어 캐시한다(못 찾으면 단색 사각형·기본 글꼴로 대체, 다음 호출에서 재시도). `Panel` 색을 HUD 값(0.07, 0.08, 0.09, 0.82)으로 맞췄고, `NewPanel`(HUD 패널)과 `NewTopLine`/`SetTopLineThickness`(상단 앰버 라인)를 추가했다.
  - FIRE(220)·R(130)·일시정지(110)는 정사각 HUD 패널 + 상단 앰버 라인(8/6/5px). 누르는 동안 앰버 채움과 어두운 글자. 일시정지 "II"는 글리프 대신 세로 막대 2개 Image로 그렸다.
  - 무기 슬롯 4칸(230x88): 왼쪽에 번호 배지(앰버), 가운데 무기 이름(`playerShooter.guns[i].gameObject.name`, HUD와 같은 이름: Pistol/AK/SMG/Shotgun을 대문자로). 3상태: 선택 = 굵은 앰버 라인(7px) + 앰버 글자 + 따뜻한 어두운 배경, 보유 = 밝은 글자 + 얇은 앰버 라인(알파 .45), 미보유 = 어둡고 흐린 배경·글자·라인(터치 무반응 기존 동작 유지). 앰버 채움 안은 눌림 상태와 혼동돼서 채택하지 않았다.
  - 이전 원형 링(형제 오브젝트)과 `fireRingObject`는 제거했다. 위치는 기존 값 유지(HUD와 겹치지 않음).
  - `TouchButton`에 선택 필드 `label`, `labelNormalColor`, `labelPressedColor`를 추가했다(지정하면 눌림 때 글자색 전환). `onDown/onUp/SetInteractable/Release` 등 기존 인터페이스는 그대로다.
- 확인(에디터 강제 모바일, 플레이어 무적, Game 뷰 1920x1080·2944x1840, Android 대상 에디터): 컴파일 오류 0, EditMode 58/58. 스크린샷으로 점수·웨이브·무기 패널과 같은 둥근 패널·글꼴·상단 앰버 라인으로 어울림을 확인. 오토 에임/쌍둥이 스틱 두 모드(쌍둥이에서 FIRE 숨김·조준 스틱 표시), 슬롯 선택(권총·SMG)/보유(AK)/미보유(SMG 해금 전·산탄총) 상태, FIRE 눌림(앰버 채움, 탄창 10 → 9) 확인. 합성 포인터로 FIRE down/up(FireHeld true/false, FireDown 큐), R 큐, 잠긴 슬롯 swap -1, 열린 슬롯 swap 1, 일시정지 버튼(isPaused true, 컨트롤 숨김, CONTINUE로 재개) 재통과.
- 환경 복구: `ForceMobileInEditor` false, 플레이 종료, 조준 모드 PlayerPrefs 키 삭제, 시험용 Game 뷰 사이즈 제거(Android 그룹 19개로 복원), 씬 저장 없음.
- **미검증:** 실기기 터치감과 가독성(태블릿 실제 크기), 멀티터치 실손 조작, Kenney 글꼴에서 "AK"가 "AH"처럼 보이는 점(HUD 무기 이름과 같은 글꼴이라 그대로 둠), 노치. 에디터 스크린샷은 개발 빌드 도구(FPS 라벨·STRESS)가 같이 보인다.

## Task 9c — 모바일 UI 배치 조정과 쌍둥이 스틱 자동 재장전 (완료, 사용자 요청 2026-10-06)

사용자 요청 5가지 + "AK는 AR로 표기".
- **탄약 패널 상단 중앙:** `MobileHudLayout`(신규, 모바일에서만 오버레이가 붙임)이 기존 `WeaponHUD`(`Ammo Display`)를 상단 중앙 앵커로 옮긴다(씬 파일 수정 없음). 상단 중앙에는 보스 체력바(`Boss Health Panel`, 520x76)가 있어서 `Zombie.bosses`가 있으면 그 아래(`y=-104`)로 내린다. 보스가 없으면 `y=-20`. 확인: 보스 없음 -20, 보스 있음(더미 등록) -104. PC는 그대로(앵커 우하단, 오버레이 없음).
- **FIRE·R:** 기존 탄약 패널 자리인 오른쪽 아래로 이동(FIRE 220 모서리, R 130 왼쪽, 아래 정렬).
- **무기 슬롯:** FIRE·R 위 가로 한 줄(160x84, 간격 10). 번호 배지는 없앴고 표기는 `PISTOL / AR / SMG / SG`(`WeaponDisplayName.SlotShortLabel`). 선택/보유/미보유 상태 표현은 그대로.
- **AK → AR 표기:** `WeaponDisplayName.Get`(MobileCore)이 화면 표기만 바꾼다(`AK`→`AR`, 대소문자 무시, `AKM` 등은 그대로). 씬의 총 오브젝트 이름 `AK`는 바꾸지 않았다(`gameObject.name`을 쓰는 다른 코드 영향 없음). `WeaponHUD`가 이 함수를 거쳐 PC·모바일 HUD 모두 "AR"로 보인다(모바일 스크린샷에서 `AR 30 / 150` 확인). 씬 안 다른 텍스트에는 "AK"가 없었다.
- **쌍둥이 스틱 자동 재장전:** `PlayerInput.ReadMobileInput`의 쌍둥이 스틱 분기에서 스틱을 당긴 채(`twinStickFire.Held`) 총 상태가 `Empty`이면 `reload=true`를 낸다. 오토 에임 모드와 PC는 기존 규칙(자동 재장전 없음) 그대로. `PlayerShooter`·`Gun`은 수정하지 않았다. 확인: 구현 전에 스틱을 당긴 채 탄창을 비우면 `Empty`에 멈춤(RED), 구현 후 재장전이 시작되어 완료(탄창 10, 예비탄 20→10)(GREEN).
- 테스트: `WeaponDisplayNameTests` 14건(구현 전 컴파일 실패 확인 후 통과). EditMode 72/72.
- **미결정/알아둘 점(사용자 확인 필요):**
  - 자동 재장전이 끝난 뒤 스틱을 계속 당기고 있어도 바로 다시 발사하지 않는다(기존 "재장전 후 한 번 놓았다 다시 눌러야 발사" 규칙이 `PlayerShooter`에 있어서). 재장전 직후 계속 발사되게 하려면 `PlayerInput`에서 재장전 중 `fire=false`로 래치를 풀어 주는 방식이 있으나 이 규칙을 바꾸는 것이라 사용자 확인 후 하겠다.
  - 보스 체력바가 있을 때만 탄약 패널이 내려간다(상단 중앙 겹침 방지).
- **미검증:** 실기기 배치(터치 영역 겹침, 엄지 닿는 거리), 16:10 태블릿에서 상단 중앙 패널과 보스 바 간격.

## Task 9d — 쌍둥이 스틱 재장전 후 계속 발사 (완료, 사용자 요청 2026-10-06)

- Task 9c의 미결정 항목을 사용자가 "재장전 후에도 계속 발사되게" 로 결정했다. `PlayerInput.ReadMobileInput`의 쌍둥이 스틱 분기에서, 스틱을 당긴 채 총이 `Reloading`이면 `fire=false`로 거두어(= `PlayerShooter`의 "재장전 뒤 한 번 놓아야 발사" 대기 `blockFireUntilRelease`가 풀린다) 재장전이 끝난 첫 프레임에 `fire=true`, `fireDown=true`를 한 번 낸다(`resumeFireAfterReload`). 스틱을 놓으면 재개하지 않는다. 오토 에임 모드와 PC는 기존 규칙 그대로(재장전 후 한 번 놓았다 다시 눌러야 발사). `PlayerShooter`·`Gun` 미수정, 변경은 `PlayerInput.cs` +21줄.
- 확인(에디터 강제 모바일, 쌍둥이 스틱, 플레이어 무적):
  - 구현 전(RED): 소총으로 스틱을 계속 당기면 탄창이 비어 자동 재장전된 뒤 탄창 30에서 멈춤(발사 안 함).
  - 구현 후(GREEN): 재장전 뒤 발사가 이어져 탄창 30→9, 예비탄 150→120.
  - 재장전 도중(96%) 스틱을 놓으면 재개하지 않음: 재장전 후 탄창 30 유지.
  - 단발 무기(권총): 재장전 후 한 발만 나가고(탄창 10→9) 계속 연발되지 않음. 단발 무기의 정상 동작(한 번 당길 때 한 발)과 같다.
  - EditMode 72/72, 콘솔 오류 없음.
- 자동 테스트는 만들지 않았다(`PlayerShooter`·`Gun`·씬이 필요한 흐름). 플레이 모드 확인으로 대신했다.
- **참고:** 이번 작업 중 `Assets/TextMesh Pro/` 아래 파일이 대량으로 수정·추가된 것으로 나타났다(셰이더 등). 제가 의도한 변경이 아니라 Unity의 자동 갱신(패키지 리소스 재임포트)으로 보여 커밋하지 않았다(AGENTS.md "Unity 자동 재저장 파일" 규칙). 되돌릴지는 사용자가 정한다.

## Task 9e — 모바일에서 권총·샷건 누르고 있으면 자동 연속 발사 (완료, 사용자 요청 2026-10-06)

- `PlayerInput.ReadMobileInput` 끝에서, 발사 입력이 눌려 있고(`fire`) 총이 `Ready`이며 발사 모드가 `Manual`(권총·샷건)이면 매 프레임 `fireDown=true`를 낸다. 실제 발사 간격은 `Gun.Fire`가 `timeBetFire`(권총 0.2초, 샷건 0.75초)로 제한한다. 오토 에임(발사 버튼)과 쌍둥이 스틱 모두 적용. PC는 `ReadMobileInput`을 거치지 않으므로 그대로(단발). 소총·SMG는 원래 연사다. `PlayerInput.cs` +10줄, `PlayerShooter`·`Gun` 미수정.
- **`Ready`일 때만 내는 이유:** 탄창이 비었을 때(`Empty`) `fireDown`이 `PlayerShooter`의 "빈 탄창 + 새 발사 입력 → 재장전" 규칙을 건드려 오토 에임 모드에서 의도치 않게 자동 재장전되는 것을 막기 위해서다. 따라서 오토 에임 모드에서 권총을 계속 누르면 탄창이 비는 순간 `Empty`에서 멈추고, R 버튼 또는 FIRE를 다시 눌러야 재장전한다(기존 규칙). 쌍둥이 스틱 모드는 앞서 만든 자동 재장전이 담당한다.
- 확인(에디터 강제 모바일, 오토 에임, 플레이어 무적):
  - 구현 전(RED): 권총으로 FIRE를 3초 눌러도 탄창 10→9(한 발).
  - 구현 후(GREEN): 권총을 누르고 있으면 약 1초 안에 탄창 10→0까지 연속 발사되고 `Empty`에서 멈춤(4초 뒤에도 `Empty`, 자동 재장전 없음).
  - 샷건: 누르고 있는 동안 8발이 간격을 두고 나감(게임 시간 t=49.92에서 탄창 4, t=54.38에서 0), 한 프레임에 몰아 쏘지 않음.
  - 스틱/버튼을 놓으면 즉시 멈춤. EditMode 72/72.
- 자동 테스트는 만들지 않았다(`PlayerShooter`·`Gun` 필요). 플레이 모드 확인으로 대신했다.

## Task 14a — 앱 아이콘·스토어 이미지 적용 (완료, 사용자가 이미지 제공 2026-10-06)

- 사용자가 `Assets/Images/AppIcon-1.png`(1254x1254, 앱 아이콘용 키아트)와 `AppIcon-2.png`(1794x876, 피처 그래픽용, 비율 2.048)를 올렸다. 게임 분위기(군인 + 좀비 + 앰버 가로등 조명)와 맞는다.
- **크롭 선택(사용자 결정):** 적응형 아이콘은 가장자리가 마스크(중앙 약 66%)로 잘려 원본 그대로는 군인이 작다. 크롭 A(전체)/B(군인 중심 1000px)/C(860px)를 원형 마스크 미리보기로 보여 드렸고 사용자가 **B**를 골랐다.
- 생성 파일: `Assets/Images/Android/AppIcon_1024.png`(B 크롭 1024), `AppIcon_Foreground_Empty.png`(투명 전경), `StoreAssets/play-icon-512.png`(Play 고해상도 아이콘 512x512, 32bit PNG, 653KB), `StoreAssets/play-feature-graphic-1024x500.png`(1024x500, 24bit PNG 알파 없음, 1064KB). `StoreAssets/`는 Unity가 임포트하지 않는 폴더(Assets 밖)다.
- `MobileEditorMenu.ApplyAndroidIcons`(메뉴 `Urban Survival/Mobile/Apply Android Icons`): 적응형(배경=아이콘 그림, 전경=투명)·라운드·레거시 18개 슬롯을 채운다. 확인: 적용 전 `filled=0/18`(RED), 적용 후 `filled=18/18`(GREEN).
- `ProjectSettings.asset`에는 아이콘 슬롯과 버전 `bundleVersion 1.0.0`만 커밋(원래 있던 `runInBackground` 변경은 제외).
- 체크리스트(`mobile-store-checklist.md`)의 아이콘·이미지 항목 갱신. 남은 이미지: **휴대전화 스크린샷(최소 2장)**.
- **미검증:** 실제 런처/Play 목록에서의 모양(빌드 필요). 피처 그래픽은 원본을 1024x500으로 축소만 했다(텍스트 `URBAN SURVIVAL`이 이미지 안에 있고 가독성 확인은 Play Console 미리보기에서).
- 참고: 이미지 원본이 Assets 안에 있어 Unity가 임포트하지만 어떤 씬에서도 참조하지 않으므로 빌드에는 들어가지 않는다(아이콘 마스터는 아이콘 슬롯으로 들어감).

## Task 14b — Play 스토어 등록 준비 문서 (완료, 사용자 요청 2026-10-06)

사용자가 "현재 빌드를 앱스토어에 올릴 준비"를 요청했다. 빌드는 별도로 진행 중(개발용 APK, Input Handling 경고 창에서 사용자 응답 대기)이라 에디터 없이 가능한 준비를 했다.
- 신규 문서(`docs/store/`):
  - `asset-license-audit.md`: 외부 에셋별 출처·라이선스 점검표. CC0 확인됨(Quaternius, Kenney, 남성 음성), **미확인**(Toon Shooter, GUI PRO, 효과음 7개, `Searching.ogg`, 초기 프로젝트 자원), 사용자 확인(AI 생성 이미지 약관).
  - `store-listing-draft.md`: 한국어·영어 짧은/긴 설명(실제 기능만), Play Console 입력 제안, 콘텐츠 등급(IARC) 가이드, Data Safety 가이드(Unity Analytics 켬/끔 두 시나리오), 개인정보처리방침 초안 A/B.
- `mobile-store-checklist.md`에 6장 릴리스 런북(10단계, 담당 구분)과 7장 출시 전 문제 목록 추가.
- **중요 발견:**
  1. GitHub 저장소 `ssa25879/URP_ZombieGame`이 **공개**(`api.github.com`에서 `private: false`, HTTP 200). Asset Store 에셋 원본이 공개 저장소에 올라가 있어 라이선스 위반 가능성이 있다. 비공개 전환은 사용자가 직접(저장소 설정).
  2. `AndroidTVCompatibility: 1`(Android TV 호환 켜짐). 터치 전용 게임이라 `0`이 맞다. 에디터가 응답하지 않아(Input 경고 창) 아직 변경하지 못했다.
  3. Unity Analytics 켜짐(Data Safety 영향), Unity 스플래시 로고 표시, 출처 불명 오디오.
- 이번 작업에서 하지 않은 것: AAB 빌드(키스토어 없음, 빌드는 요청 시), 프로젝트 설정 변경, 저장소 설정 변경.

## Task 14c — 개발 빌드 재생성·설치와 릴리스 설정 정리 (완료)

- 개발용 APK 재빌드 성공(사용자 요청 2026-10-06): `Builds/Android/UrbanSurvival-dev.apk` 180 MB, 약 8분. 중간에 "Unsupported Input Handling on Android"(Active Input Handling `Both`) 창이 떠서 사용자가 Ignore로 처리. 새 UI 배치, 쌍둥이 스틱 자동 재장전·재장전 후 계속 발사, 권총·샷건 자동 연속 발사, AK→AR 표기, 새 앱 아이콘, 버전 1.0.0이 들어 있다.
- 태블릿(TB373FU)에 `adb install -r`로 설치만 했다(실행하지 않음). 기기 보고값: `targetSdk=36`, `minSdk=25`, `versionName=1.0.0`, `versionCode=1`, `primaryCpuAbi=arm64-v8a`, 권한 `INTERNET`뿐.
- **`AndroidTVCompatibility` 1→0:** 기기 매니페스트에 `LEANBACK_LAUNCHER`가 있어 TV 호환이 켜진 것이 확인됐다. 터치 전용 게임이라 껐다(`ProjectSettings.asset`의 이 한 줄만 커밋, 원래 있던 `runInBackground` 변경은 제외). 다음 빌드에서 사라지는지 확인해야 한다.
- 아이콘 텍스처(`AppIcon_1024.png`, `AppIcon_Foreground_Empty.png`) 압축을 `Uncompressed`로 변경(빌드 경고 "Compressed texture ... is used as icon" 해소). `.meta` 2개 커밋.
- 빌드 로그 참고: `totalErrors: 1`이 보고됐으나 APK는 정상 생성·설치·`dumpsys` 확인됨(콘솔의 오류 항목은 구현 전 테스트 단계에서 남은 이전 기록으로 보임). "TextMesh Pro Essential Resources are missing" 경고가 에디터에 있음(게임 HUD는 레거시 Text라 영향 없음으로 보이나 미확인). 라운드·레거시 아이콘은 Unity 향후 버전에서 제거 예정이라는 경고(정보성).
- **미검증:** 새 빌드에서의 실제 플레이(사용자가 태블릿에서 확인), S26 Ultra, 릴리스 빌드의 INTERNET 권한.

## Task 14d — GUI PRO Kit 저장소 제거, Toon Shooter 라이선스 기록 (완료, 사용자 지시 2026-10-06)

- 사용자 지시: "Toon Shooter는 CC0일 거고, GUI PRO는 저장소에서 제거".
- **GUI PRO Kit - Simple Casual:** `git rm --cached`로 Git 추적에서 제거(파일 4,960개 + `.meta`, 약 151 MB)하고 `.gitignore`에 `/Assets/GUI PRO Kit - Simple Casual/`와 `.meta`를 추가했다. **로컬 파일은 삭제하지 않았다**(게임·에디터는 그대로 동작, Unity에서 스프라이트·프리팹 로드 확인).
- 프로젝트가 실제로 쓰는 GUI PRO 자원은 6개뿐이다(Unity 의존성 조회): 스프라이트 5개(`BasicFrame_Rectangle02_s_White`, `LineFrame_White`, `BasicFrame_Circle_337_White`, `Icon_WhiteIcon_Home`, `Icon_WhiteIcon_Setting_s`)와 프리팹 `Play_Pause_common (1)`. 참조하는 곳: 씬 5개(`Intro`, `UrbanSurvival`, `UrbanSurvival_ResultTest`, `BossTestScene`, `FinalBossTestScene`)와 `UrbanSurvivalSettingsTheme.asset`. 따라서 앱(컴파일된 결과물)에는 이 6개가 들어간다.
- **한계(중요):** (1) 이 브랜치의 이전 커밋과 다른 브랜치(`SideProject`, `main`)에는 파일이 그대로 남아 있고 저장소는 공개 상태라서 공개 기록에서 사라진 것은 아니다. (2) 다른 PC에서 이 브랜치를 받거나 병합하면 해당 폴더가 삭제된다(Asset Store에서 다시 임포트해야 복구, 안내는 `docs/store/third-party-not-in-repo.md`). 완전 제거(대체, 비공개 전환, 기록 삭제)는 사용자 결정으로 남겼다. 기록 삭제(강제 푸시)는 되돌릴 수 없어 승인 없이 하지 않았다.
- **Toon Shooter Game Kit:** 사용자 진술로 CC0, 원문 미확인으로 점검표에 기록했다. 라이선스 근거(페이지 주소, 확인일)를 같은 폴더에 `License.txt`로 남기는 것을 권고했다.
- 신규 문서 `docs/store/third-party-not-in-repo.md`, 점검표·체크리스트 갱신.

## Task 14e — 공개용 깨끗한 저장소(방법 B) 준비 (진행 중) 와 Searching.ogg 출처 확인

- 사용자 지시: "과거 기록만 private 처리" 대신 방법 B(기존 저장소는 비공개 보존, GUI PRO를 기록 전체에서 지운 새 공개 저장소) 진행. 이 저장소(`D:\work\Zombie`)는 건드리지 않고 `D:\work\Zombie_clean.git`(bare 복제본, remote 제거)에서 `git filter-branch`로 정리한다. 푸시는 사용자 승인 후에만.
- 사용자가 `Searching.ogg`의 출처를 확인(OpenGameArt https://opengameart.org/content/searching, CC0). `Assets/Game/Audio/Music/SOURCE.md` 신규, 점검표·체크리스트에서 "출처 불명" 목록에서 제외. `singularity_calm.wav`는 페이지 주소가 아직 없다.
- 1차 정리 검증: 커밋 143개 유지, GUI PRO 경로가 있는 커밋 0개, 도달 가능한 GUI PRO 객체 0개, `SideProject-Mobile` 최신 트리 해시가 원본과 동일(`88256277…`), `main`은 원래 GUI PRO가 없어 동일, `SideProject`만 트리가 달라짐(GUI PRO 제거).
