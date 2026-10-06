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
