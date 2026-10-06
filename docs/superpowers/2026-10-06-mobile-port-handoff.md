# Urban Survival 모바일 포팅 인수인계

- 작업 브랜치: `SideProject-Mobile` (원격 등록 완료)
- 기준 문서: 설계 `docs/superpowers/specs/2026-10-06-mobile-port-design.md`, 계획 `docs/superpowers/plans/2026-10-06-mobile-port.md`
- 실행 방식: 병행(직접 구현 + 작업량이 큰 Task는 서브에이전트). Unity 에디터가 하나라 서브에이전트는 순차 실행한다.
- 이 문서는 Task가 하나 끝날 때마다 항목을 추가한다. 확인하지 못한 항목은 "미검증"으로 적는다.
- 백업: 사용자가 Git 브랜치로 갈음하기로 확인했다(2026-10-06). 파일 백업본은 만들지 않는다.

## 에디터 사용 메모
- `mcp__UnityMCP__execute_code`(CodeDom, C# 6)에서는 `Object`가 모호하므로 `UnityEngine.Object.FindFirstObjectByType<T>()`처럼 완전한 이름을 쓴다.
- 플레이 모드 진입 직후 첫 호출은 도메인 리로드로 타임아웃이 날 수 있다. 몇 초 뒤 다시 호출한다.

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
