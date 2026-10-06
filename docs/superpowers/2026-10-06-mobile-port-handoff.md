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
