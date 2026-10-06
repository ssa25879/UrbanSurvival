# Urban Survival Android 모바일 포팅 설계

- 작성일: 2026-10-06
- 브랜치: **모바일 포팅은 `SideProject-Mobile` 브랜치를 사용한다**(`SideProject`에서 분기, 2026-10-06 사용자 지시). 모바일 관련 코드·UI·설정·빌드 변경은 모두 이 브랜치에만 커밋하며 `SideProject`·`main`에는 커밋하지 않는다.
- 기준 문서: `Urban_Survival_Android_모바일_포팅_상세_기획서_20261006_v1.0.docx` (이하 **모바일 기획서**), PC 상세 기획서 v1.19, 사용자 결정(조준 모드: 오토 에임 기본 + 쌍둥이 스틱 옵션)
- 앱 정보(2026-10-06 사용자 확정): 회사명 `YWS`, 패키지명 `com.yws.urbansurvival`
- 테스트 실기기(사용자 보유): Samsung Galaxy S26 Ultra, Lenovo XiaoxinPad 2025
- 상태: 설계 초안, 사용자 검토 대기(D2·D3 승인 완료)

이 문서는 모바일 기획서를 구현 가능한 단위로 옮긴 설계다. 모바일 기획서와 충돌하면 모바일 기획서가 우선한다. 아래 "코드 대조에서 추가된 결정"은 기획서에 없던 항목으로, 사용자 확인이 필요하다.

## 1. 목표와 성공 기준

- 목표: 기존 PC 게임 규칙을 유지한 채 Android 스마트폰에서 이동·조준·사격·재장전·무기 교체·메뉴를 터치로 수행하고, Google Play 테스트 트랙에 올릴 수 있는 AAB를 만든다.
- 성공 기준(모바일 기획서 P3·P4 완료 판단):
  - 실기기에서 인트로부터 결과 화면까지 한 판을 끝낼 수 있다.
  - PC 빌드는 기존과 동일하게 동작한다(키보드·마우스 회귀 없음).
  - 서명된 테스트 AAB와 Play Console 준비 체크리스트가 있다.
- 비목표: iOS, 게임패드, 세로 화면, 모바일 전용 밸런스·캐릭터·무기, 카메라 제스처, 스토어 마케팅, Play Console 계정 생성·결제.

## 2. 현재 코드에서 확인한 구조

| 대상 | 확인 내용 | 모바일 영향 |
|---|---|---|
| `PlayerInput` | 입력의 단일 진입점. `move`, `rotate`, `fire`, `fireDown`, `reload`, `aimPosition`, `swapToSlot1~4` 제공. 일시정지·게임오버 중 입력 차단과 `suppressFireUntilRelease`(포커스·정지 복귀 오발 방지)가 이미 있다. | 입력 소스만 플랫폼별로 분기하면 된다. |
| `PlayerInput.IsPointerBlockedForFire` | `Input.mousePosition`이 Selectable UI 위이면 발사를 막는다. | 터치 발사 버튼도 Selectable이면 자기 자신이 발사를 막는다. 모바일 입력 경로는 이 검사를 거치지 않는다(결정 D1). |
| `PlayerMovement.GetAimDirection` | `aimPosition`을 카메라 Ray로 플레이어 높이 평면에 투영해 방향을 구한다. 무효하면 `lastAimDirection` 유지. | `aimWorldDirection`이 유효하면 이 계산을 대체한다. |
| `PlayerMovement.GetMoveDirection` | 카메라 기준 평면 이동, `ClampMagnitude(…, 1)`로 대각선 속도 제한. | 스틱 벡터를 `move/rotate`에 넣으면 그대로 동작한다. |
| `PlayerShooter` | `playerInput.fire/fireDown/reload/swapToSlot*`만 읽는다. 재장전 우선·빈 탄창 규칙·`blockFireUntilRelease` 포함. | 변경 없음(기획서 원칙 유지). |
| `GunData.range` | 무기별 사거리(권총 15, 소총 30, SMG 20, 산탄총 12). | 오토 에임 사거리 기준. |
| `Zombie` | `LivingEntity.dead` 존재. 살아있는 전체 목록은 없고 `bosses`(보스 목록)만 정적 리스트로 관리. | 오토 에임 대상 탐색용 목록이 필요하다(결정 D2). |
| `GameManager` | 일시정지는 `pauseKey`(Escape) 입력과 `TogglePause()`. | 모바일은 Escape가 없다(결정 D3). |
| 문구 | `ReloadIndicator`에 `"RELOAD  [R]"`, `"NO AMMO  [1]"` 하드코딩. `IntroMenu`·`UIManager`에 `Application.Quit()`. 튜토리얼 문구는 씬에 있다. | 모바일 문구 분기 필요. |
| 환경 | Unity 6000.3.19f1에 `AndroidPlayer` 모듈 설치됨(로컬 확인). Android 설정은 `applicationIdentifier` 비어 있음, 회사명 DefaultCompany, 최소 SDK 25. 프로젝트가 URP 호환 모드(Render Graph 끔)이며 PC 빌드에서 Render Graph를 켜면 렌더 예외가 났다. | Android 빌드도 호환 모드를 유지한다. |
| 원격 | `origin`에 `SideProject`, `main`만 있다. `SideProject-Mobile`은 원격에 없다. | 원격 등록은 사용자 승인 후 push(§10). |

## 3. 코드 대조에서 추가된 결정 (사용자 확인 필요)

- **D1. 모바일 발사는 마우스 기반 차단 검사를 거치지 않는다.** `IsPointerBlockedForFire`는 마우스 좌표와 UI 겹침을 보는데, 터치에서는 발사 버튼 자체가 UI이므로 항상 막힌다. 모바일 경로는 `MobileInputState`의 발사 값을 직접 쓰고, 오발 방지는 `suppressFireUntilRelease`와 같은 래치(§5)로 처리한다. PC 경로는 그대로다.
- **D2. 살아있는 적 목록을 `Zombie`에 추가한다.** 모바일 기획서는 `Zombie` 변경을 적지 않았지만, 오토 에임이 가장 가까운 적을 찾으려면 목록이 필요하다. `Zombie`에 정적 `alive` 리스트(OnEnable/OnDisable 등록, 기존 `bosses` 패턴 재사용)를 추가한다. 최대 500마리를 매 프레임 순회하지 않도록 탐색은 0.1초 간격으로 하고 제곱거리로 비교한다. 대안인 `Physics.OverlapSphere`는 적 레이어 설정과 물리 부하에 의존해 채택하지 않는다.
- **D3. 모바일 일시정지 버튼을 HUD에 추가한다.** 기획서에는 일시정지 진입 수단이 없다. 우상단 안전 영역 안에 작은 버튼을 두고 `GameManager.TogglePause()`를 호출한다. Android 뒤로 가기는 Escape로 들어오므로 기존 `pauseKey` 경로도 함께 동작한다.
- **D4. 오토 에임 발사 버튼의 의미:** 버튼을 누르는 동안 조준과 발사를 함께 한다. 연사 무기는 누르는 동안 연사, 단발·산탄총은 누를 때마다 1회(`fireDown`)이다. 대상이 없으면 이동 방향 → 마지막 방향 순으로 조준하며 발사는 그대로 가능하다(탄을 낭비하는 것은 사용자 책임).
- **D5. 재장전 알림 문구는 플랫폼별로 바꾼다.** `ReloadIndicator`의 `[R]`/`[1]` 표기를 모바일에서는 "RELOAD"/"NO AMMO"로 쓰고, 모바일은 무기 아이콘 터치로 권총 전환이 가능하다고 안내하는 문구를 따로 정한다.

## 4. 아키텍처

```
[Touch UI]  ──writes──▶  MobileInputState (정적, 프레임 단위)  ──read──▶  PlayerInput  ──▶ PlayerMovement / PlayerShooter (변경 최소)
 MoveStick / FireButton / AimStick /                                         ▲
 ReloadButton / WeaponSlotTouch / PauseButton                                │ aimWorldDirection
                                                              AutoAimTargeting ──reads── Zombie.alive, GunData.range
 SettingsPanel (GameSettingsKit) ─toggle─▶ MobileAimModeSettingsBinder ─▶ PlayerPrefs ─▶ MobileTouchOverlay(모드 전환)
```

### 4.1 신규·변경 단위

| 단위 | 책임 | 의존 |
|---|---|---|
| `MobileInputState` (신규, 정적) | Move(Vector2), Aim(방향+유효 플래그), FireHeld, FireDown(1회 요청), Reload(1회 요청), SwapSlot(0~4, 1회 요청). `ConsumeXxx`로 소비하면 초기화. `ResetAll()` 제공. | 없음 |
| `PlayerInput` (변경) | 모바일 활성 시 `MobileInputState`를 읽어 기존 출력 값을 채우고 `aimWorldDirection`·`hasAimWorldDirection` 제공. PC 경로 불변. | `MobileInputState` |
| `PlayerMovement` (변경, 최소) | `hasAimWorldDirection`이면 그 방향을 `lastAimDirection`에 반영하고 반환, 아니면 기존 Ray 투영. | `PlayerInput` |
| `AutoAimTargeting` (신규, 순수 로직+얇은 MonoBehaviour) | 대상 결정(§6). 순수 정적 함수로 분리해 EditMode 테스트 가능. | `Zombie.alive`, `GunData.range` |
| `TouchJoystick` (신규 UI) | 플로팅 스틱, 데드존·정규화 변환(순수 함수 분리). 이동·조준 스틱이 재사용. | `MobileInputState` |
| `FireButton`, `ReloadButton`, `PauseButton` (신규 UI) | 눌림·해제 이벤트를 상태로 변환. | `MobileInputState`, `GameManager` |
| `WeaponHUD` 슬롯 터치 (변경) | 슬롯 아이콘에 터치 입력 추가. 미획득 슬롯은 비활성 표시·무시. 직접 교체하지 않고 `SwapSlot` 요청만 보낸다. | `MobileInputState` |
| `MobileTouchOverlay` (신규) | Canvas(1920×1080 Scaler)·Safe Area 컨테이너 생성, 플랫폼·조준 모드에 따라 UI 표시. | 위 UI들 |
| `MobileAimModeSettingsBinder` (신규, 모듈 밖) | 설정 창 토글 ↔ `PlayerPrefs`(키 `MobileAimMode`) ↔ 오버레이. | `GameSettingsKit` 공개 API |
| `SafeAreaFitter` (신규) | HUD 최상위 컨테이너에 `Screen.safeArea` 적용. | 없음 |
| `ReloadIndicator`·튜토리얼·`IntroMenu`/`UIManager` (변경, 최소) | 모바일 문구 분기, QUIT 숨김. | `MobilePlatform` |
| `MobilePlatform` (신규, 정적) | `IsMobile` = `Application.isMobilePlatform` 또는 에디터 전용 강제 플래그. 빌드에서는 강제 플래그 무시. | 없음 |
| `Zombie` (변경, 최소) | 정적 `alive` 리스트 추가(D2). | 없음 |

### 4.2 변경하지 않는 것
`PlayerShooter`, `Gun`, 총기·탄약·재장전·피해·Raycast·총구 막힘·산탄·드랍·웨이브 로직. `GameSettingsKit` 모듈 내부에는 Urban Survival 전용 타입을 추가하지 않는다(바인더가 모듈 공개 API만 사용).

## 5. 입력 규칙

- **프레임 규약:** 터치 UI는 이벤트에서 `MobileInputState`에 값을 쓰고, `PlayerInput.Update`가 프레임당 한 번 읽고 소비한다. 일회성 요청(FireDown·Reload·SwapSlot)은 소비 즉시 초기화한다.
- **이동:** 스틱 벡터를 `rotate`(x)·`move`(y)에 넣는다. 크기는 1 이하로 정규화되어 대각선 속도가 증가하지 않는다(F02).
- **래치(오발 방지):** 모바일에도 `suppressFireUntilRelease` 개념을 적용한다. 다음 상황에서 `MobileInputState.ResetAll()`과 래치 설정: 앱 일시정지·포커스 변경, 일시정지 진입·해제, 게임 오버, 씬 재시작, 조준 모드 전환. 래치 중에는 발사 입력이 해제(FireHeld=false, 스틱이 데드존 안)될 때까지 `fire`·`fireDown`을 내보내지 않는다.
- **재장전과 발사 동시:** 기존 `PlayerShooter` 규칙(재장전 우선, 재장전 후 한 번 떼야 발사)을 그대로 쓴다. 터치도 `fireDown`과 `fire`를 구분해 전달한다.
- **마우스 조준값:** 모바일에서 `aimPosition`은 사용하지 않는다. 유효한 `aimWorldDirection`이 없으면(예: 오토 에임 대상·이동·마지막 방향 모두 없음) 기존 PC 계산으로 되돌린다.

## 6. 조준 모드

### 6.1 오토 에임 (기본값)
- 화면: 이동 스틱 + 발사 버튼 + 재장전 버튼 + 무기 슬롯 + 일시정지 버튼.
- 발사 버튼을 누르는 동안 `AutoAimTargeting`이 방향을 갱신한다. 결정 순서(모바일 기획서 05장):
  1. 현재 무기 사거리(`GunData.range`) 안에서 가장 가까운 살아있는 적(수평 거리, 제곱 비교)
  2. 없으면 현재 이동 방향
  3. 이동 입력도 없으면 마지막 유효 조준 방향
- 사거리 밖 적은 대상이 아니다. 벽 뒤 적도 후보가 되며(기획서 결정) 실제 피해는 기존 총구·Raycast 판정이 막는다.
- 버튼을 떼고 있을 때의 캐릭터 방향은 마지막 유효 방향을 유지한다.
- 오토 에임은 방향만 제공하고 명중·산포·사거리·탄약·발사 속도·재장전 상태는 바꾸지 않는다.

### 6.2 쌍둥이 스틱
- 오른쪽 스틱이 조준·발사를 맡는다. 데드존(초기값 0.25)을 넘으면 그 방향으로 조준하며 `FireHeld=true`, 데드존 밖으로 처음 나가는 프레임에 `FireDown`.
- 중앙 복귀 시 발사 중지. 연사 무기는 유지 중 연사, 단발 무기는 중앙 복귀 후 다시 당겨야 다음 발사(F11).
- 재장전 후 계속 당기고 있었다면 래치로 인해 한 번 중앙으로 복귀해야 발사한다.
- 이 모드에서는 발사 버튼을 숨긴다.

### 6.3 설정과 전환
- 값: `AutoAim`(기본) / `TwinStick`. `PlayerPrefs` 키 `MobileAimMode`.
- 설정 창 토글은 PC에서는 숨기고 모바일(`MobilePlatform.IsMobile`)에서만 표시한다.
- 전환은 다음 입력 프레임부터 적용하며, 이때 상태를 초기화하고 래치를 건다.

## 7. 모바일 UI

- Canvas Scaler: Scale With Screen Size, 기준 1920×1080, Match 0.5(초기값, 화면비 검수 후 조정).
- 배치(1920×1080 기준 초기 치수, 실기기 확인 후 조정):
  - 이동 스틱: 왼쪽 아래 영역(화면 왼쪽 40% 하단)에서 최초 터치 위치를 중심으로 생성, 손을 떼면 숨김. 반지름 약 110.
  - 발사 버튼: 오른쪽 아래, 지름 약 190 (오토 에임 모드).
  - 조준 스틱: 오른쪽 아래 영역(쌍둥이 스틱 모드), 반지름 약 110.
  - 재장전 버튼: 발사 영역 위, 지름 약 120.
  - 일시정지 버튼: 오른쪽 위 안전 영역 안, 약 100.
  - 무기 슬롯 4칸: 기존 무기 패널의 아이콘을 터치 가능한 크기(약 140)로 확대. 위치는 발사 버튼과 겹치지 않게 오른쪽 가장자리 세로 배치를 우선 검토한다(화면비 16:9·20:9 검수에서 확정).
- 터치 영역은 최소 약 48dp. 픽셀과 dp는 다르므로 실기기에서 확인해 조정한다.
- 터치 UI는 최상위 `Safe Area` 컨테이너 아래에 둔다. 월드 렌더링 영역은 줄이지 않는다.
- 멀티터치: 이동 스틱과 발사/조준 입력은 각각 독립된 `pointerId`로 추적한다.
- 터치 오버레이는 모바일에서만 활성, PC에서는 비활성이며 기존 UI를 유지한다.
- HUD 안전 영역 우선 확인 대상: 체력, 점수·시간, 웨이브, 보스 체력, 미니맵, 무기·탄약 패널.

### 문구
| PC | 모바일 |
|---|---|
| `RELOAD [R]` | `RELOAD` + 재장전 버튼 |
| `NO AMMO [1]` | `NO AMMO` + 권총 슬롯 안내 |
| WASD / 마우스 / 좌클릭 / 1~4 안내 | 이동 스틱 / 자동 조준 또는 오른쪽 스틱 / 발사 버튼 / 무기 아이콘 터치 |

튜토리얼 팝업도 플랫폼과 선택한 조준 모드에 맞는 문구를 쓴다. 메뉴(인트로·캐릭터 선택·일시정지·설정·게임 오버·결과·무한 모드 선택)는 터치로 검수하고 필요한 경우에만 모바일에서 버튼 크기·간격을 키운다. QUIT 버튼은 모바일에서 숨긴다.

## 8. 플랫폼 활성화 정책
`MobilePlatform.IsMobile`은 `Application.isMobilePlatform`을 기본으로 한다. 에디터에서만 강제 플래그(`#if UNITY_EDITOR`)를 허용하고 실제 빌드의 판정을 덮어쓰지 않는다.

## 9. Android 빌드와 성능

### 9.1 빌드 설정 (모바일 기획서 11장)
Android, Landscape 고정(Portrait 불허), IL2CPP, ARM64, AAB, 최소 API 25, 타깃 API 36 이상, URP 호환 모드 유지. 회사명은 `YWS`, 패키지명은 `com.yws.urbansurvival`로 확정했다(Play에 올린 뒤에는 변경 불가). 제품명(스토어 표시 이름)·버전·아이콘·스플래시·키스토어는 배포 전에 확정한다. 키스토어는 Git에 커밋하지 않으며 `.gitignore`에 패턴을 추가한다.

### 9.2 URP 호환 모드 주의
이 프로젝트는 호환 모드를 꺼야 하는 시점에 후처리 `CheckPostProcessForDepth` 예외가 났다. Android 빌드도 호환 모드를 켜 둔 채 진행하고, 호환 모드 정의(`URP_COMPATIBILITY_MODE`)가 Android 대상에도 있는지 확인한다.

### 9.3 성능 정책 (모바일 기획서 13장)
1. 기능이 동작하는 모바일 빌드를 먼저 만든다.
2. 실기기에서 Profiler로 병목(CPU·GPU·메모리)을 구분한다.
3. 확인된 항목만 모바일 전용 Quality Level에서 조정한다(그림자, 후처리, Render Scale, 파티클 등).
4. PC 품질은 변경하지 않으며 모바일 Quality 적용 후 PC 값 불변을 확인한다.
5. 동시 적 500마리 상한과 같은 게임 규칙 변경은 밸런스 결정이므로 별도로 판단한다.
6. 목표 FPS는 목표 기기가 정해지기 전까지 고정하지 않고 30·60 FPS 유지 가능 여부를 함께 측정한다.

## 10. Git 운영
- 모든 모바일 변경은 `SideProject-Mobile`에서만 커밋한다. 작업 시작 시마다 현재 브랜치를 확인한다.
- 원격 등록은 사용자가 승인했다(2026-10-06). `git push -u origin SideProject-Mobile`로 등록한다.
- 현재 작업 트리에 있는 기존 변경(`Headlights.mat`, `ProjectSettings.asset` 자동 재저장, 핸드오프 문서, `.aiassistant/`)은 포팅 변경과 섞지 않는다. 포팅 커밋에는 의도한 파일만 담는다.
- 이 문서의 모바일 기획서와 PC AGENTS.md의 "작업 브랜치는 `SideProject`만" 규칙이 충돌한다. 모바일 기획서와 사용자 지시에 따라 모바일 포팅은 `SideProject-Mobile`로 진행한다. AGENTS.md 반영은 사용자가 결정한다.

## 11. 구현 단계 (모바일 기획서 15장)

| 단계 | 내용 | 완료 판단 |
|---|---|---|
| P0 | 브랜치 확인(완료), PC 기준 동작 확보(발사·재장전·무기 교체·HUD 기록), Android 모듈 확인(완료) | 현재 브랜치가 `SideProject-Mobile`이고 PC 회귀 비교 기준이 있다 |
| P1 입력 | `MobileInputState`, `PlayerInput` 연결, 스틱·발사·재장전·슬롯, `aimWorldDirection`, `AutoAimTargeting`, 쌍둥이 스틱, 래치, `Zombie.alive`, EditMode 테스트 | 에디터 모바일 시뮬레이션에서 두 모드 모두 동작, PC 입력 회귀 없음 |
| P2 UI | 오버레이·Safe Area·모드별 UI·문구·메뉴 터치·QUIT 숨김·일시정지 버튼·설정 토글 | 16:9·20:9·노치에서 겹침·잘림 없음 |
| P3 Android 빌드 | Player Settings, IL2CPP/ARM64/API, AAB, 개발용 서명, 실기기 설치 | 실기기에서 한 판 완료, PC 빌드 정상 |
| P4 스토어 준비 | 아이콘·버전 정책·업로드 키·최종 AAB·체크리스트 | 테스트 릴리스 업로드 가능한 AAB와 항목 목록 |

각 단계마다 PC 에디터 입력 검수를 병행한다. 단계마다 작업 로그를 `D:\Codex\Log`에 남긴다.

## 12. 테스트

### 12.1 EditMode 자동 테스트 (신규, 로직 분리 전제)
- 오토 에임 대상 선택: 사거리 안만 선택, 가장 가까운 적, 사망 제외, 대상 없음 → 이동 방향, 이동도 없음 → 마지막 방향.
- 조이스틱 변환: 입력 없음 → 0, 최대 입력 → 크기 1, 대각선 → 크기 ≤ 1, 데드존 내부 무시, 쌍둥이 스틱 데드존 진입·이탈에 따른 `FireDown`.
- 래치: 정지·포커스 복귀 후 해제 전까지 발사 차단, 해제 후 허용.
- 순수 함수로 분리하기 위해 `AutoAimTargeting`·스틱 변환은 `MonoBehaviour`에서 독립시킨다. 어셈블리는 현재 `Assembly-CSharp` 하나이므로 테스트도 그 구조를 따른다.

### 12.2 기능·통합 검수
모바일 기획서 F01~F16(이동, 발사, 재장전, 오토 에임, 쌍둥이 스틱, 설정 저장, 정지 복귀 오발 방지, 슬롯 교체)과 I00~I08(브랜치, PC 회귀, 화면비, 터치 메뉴, AAB 빌드, PC 품질 불변, 업로드 준비, 30분 플레이, 최대 적 부하)을 따른다. 에디터에서 검증할 수 없는 항목(실기기 터치, 노치, 성능, 발열, 30분 플레이)은 실기기 없이는 완료로 보고하지 않고 "미검증"으로 명시한다.

## 13. 리스크

| 리스크 | 대응 |
|---|---|
| 벽 뒤 가까운 적에게 자동 조준되어 사격이 막힘 | 우선 기획서대로 구현. 실제 플레이에서 불편하면 대상 선정 단계에 시야 검사 추가를 별도 검토 |
| 500마리 동시 적의 모바일 부하 | 실기기 계측 후 렌더·애니메이션·AI·물리 최적화 먼저. 상한 변경은 밸런스 결정 |
| 오토 에임 탐색 비용 | 0.1초 주기 + 제곱거리, 정적 목록 사용(D2) |
| 터치 영역 겹침(무기 슬롯과 발사 버튼) | 16:9·20:9 각각 검수 |
| `PlayerInput` 모바일 분기로 PC 입력 회귀 | 단계마다 PC 입력 확인, 분기는 `MobilePlatform.IsMobile`로 완전 분리 |
| 레거시 `Input` 터치 시뮬레이션 마우스와 충돌 | 모바일 경로는 `Input.mousePosition`을 쓰지 않음(D1), UI 클릭은 EventSystem 처리 |
| Render Graph 전환 문제 | 호환 모드 유지, 이번 포팅에서 전환하지 않음 |
| 실기기 부재 | 에디터 시뮬레이션으로 P1·P2 검증. P3 실기기 항목은 미검증으로 표기하고 사용자 기기 확인 요청 |

## 14. 사용자 직접 처리 항목
Play Console 개발자 계정·등록비·신원 확인, 앱 생성, 개인정보처리방침 URL, Data Safety, 콘텐츠 등급, 스토어 설명·스크린샷·피처 그래픽, 테스트 트랙 운영, 업로드 키 보관, 최종 제출. 신규 개인 계정은 Production 전 비공개 테스트 요건(모바일 기획서 부록 A 기준 12명·14일)이 있어 등록 직전에 최신 정책을 다시 확인한다.

## 15. 결정 현황
- 확정(2026-10-06): 회사명 `YWS`, 패키지명 `com.yws.urbansurvival`, 원격 등록 승인, 일시정지 버튼(D3)·`Zombie.alive` 추가(D2) 승인, 테스트 실기기 S26 Ultra·XiaoxinPad 2025.
- 미결정:
  1. 스토어 표시 제품명(현재 Player Settings의 `Zombie`를 바꿀지).
  2. 목표 FPS: 두 기기의 실측 후 확정(§9.3). 두 기기는 화면비·성능이 달라(폰 20:9급 고주사율, 태블릿 약 16:10) 레이아웃 검수 대상에 모두 포함한다.
  3. 에디터 플레이 검수 외에 두 기기에서의 실제 설치·터치 검수 절차(USB 디버깅 설정은 사용자가 준비).
