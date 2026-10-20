# Phase 1. 안정성 버그 — 상세 계획 (준비, 미착수)

- 작성일: 2026-10-20, 브랜치 `SideProject-Mobile` (Phase 3 완료 `a526421`·`4940583` 푸시 후)
- 상위 문서: 로드맵 `2026-10-12-code-review-roadmap.md` Phase 1, 스펙 R1.1~R1.6
- 규칙: 기존 파일 수정 전 백업 여부 확인(Phase 3에서는 "백업 없이" 선택, Phase 1은 다시 확인). 빌드·exe 실행·캡처는 요청 시에만. 에디터는 열어도 된다는 사용자 허락(2026-10-20) 있음.
- 종료 조건: EditMode 전체 통과(현재 102건, 증가), 검증 기록 `docs/verification/2026-10-20-phase1.md`, 커밋 6개 이내.

## 착수 전 확인 결과 (코드 재확인)
| Task | 현재 코드에서 확인한 사실 | 판단 |
|---|---|---|
| T1.1 | `Zombie.cs` 약 97~108행 `bodyRenderer.material`이 좀비마다 재질 복제. 색 곱셈·`_EMISSION` 키워드·`_EmissionColor` 설정 | 유효. 아래 위험 참고 |
| T1.2 | `ReachPrimaryGoal`은 `isGameover`이면 무시(사망 먼저면 패배 우선 OK). **반대 순서**(목표 먼저 → 같은 프레임 사망)는 `EndGame`이 `awaitingGoalChoice`·목표 결과 UI를 정리하지 않고 `timeScale`을 1로 되돌려 결과 화면과 게임오버가 겹칠 수 있음 | 유효 |
| T1.3 | `LivingEntity.Die`가 `onDeath`를 먼저 호출. 핸들러가 예외를 던지면 `Zombie.Die`의 콜라이더·NavMeshAgent 정리까지 도달 못 함(`base.Die()` 뒤에 있음) | 유효 |
| T1.4 | `MobileTouchOverlay.OnApplicationPause`는 컨트롤만 놓고 일시정지는 하지 않음. 일시정지 버튼 콜백은 `!isGameover && !isPaused && !awaitingGoalChoice` 조건 | 유효 (기기 필요) |
| T1.5 | `TouchButton.OnPointerExit`가 항상 `Release()` — FIRE 포함 모든 버튼이 손가락이 밖으로 나가면 해제 | 유효 |
| T1.6 | `SpawnPending`이 한 프레임에 `while`로 대기열을 끝까지 소환(상한은 동시 500뿐). `TryGetSpawnPosition` 링 풀이 비면 소환 1마리마다 `RefreshSpawnRingPool` 재시도. `spawnFailureStartTime`은 보스·일반 소환 성공 시에만 초기화 | 유효 |

## 실행 순서
- `Zombie.cs`를 건드리는 T1.1·T1.3은 순차(같은 파일). 그 뒤 T1.2 → T1.5 → T1.6 → T1.4(기기 필요라 마지막, 미검증으로 기록 가능).
- 서브에이전트: 로드맵은 T1.1~T1.3 병렬 가능이라 했지만 `Zombie.cs` 충돌 때문에 T1.1·T1.3은 하나로 묶는다. T1.5·T1.6은 독립이라 병렬 가능.

## T1.1 좀비 재질 인스턴스 누수 제거
- 방법: `bodyRenderer.material`을 쓰지 않고 `MaterialPropertyBlock`(`SetColor("_BaseColor")`, `_EmissionColor`)을 `SetPropertyBlock`으로 적용. 렌더러별 블록 재사용.
- **위험:** 발광(`_EMISSION` 키워드)은 재질 키워드라 블록만으로 켜지지 않는다. 현재 좀비 재질(Zombie Apocalypse Kit)이 URP Lit이면 키워드가 꺼진 재질에서 `_EmissionColor` 값은 무시된다. 방법 후보: (a) Final Boss/glow가 필요한 데이터용 공유 재질 1벌을 미리 만들어 `sharedMaterial` 교체(재질 인스턴스는 데이터 종류별 1개), (b) 키워드가 이미 켜진 재질 변형을 에셋으로 추가. 구현 전에 에디터에서 해당 재질의 셰이더·키워드와 `ZombieData.glowColor` 사용처를 확인하고 후보를 고른다.
- 색 곱셈: 기존은 재질 원래 색 × `skinColor`. 블록에서는 원래 `sharedMaterial.color` × `skinColor`를 계산해 `_BaseColor`로 넣는다(`_Color`/`_BaseColor` 속성명 확인).
- 검증: 소환·사망 반복 후 `Resources.FindObjectsOfTypeAll<Material>().Length` 불변(에디터 플레이, `execute_code`). 일반/강화/보스/Final Boss 색이 이전과 같음(스크린샷 비교는 사용자 허락 필요).
- 커밋: `fix: 좀비 재질 복제 제거(MaterialPropertyBlock)`

## T1.3 사망 처리 순서 견고화
- 방법: `LivingEntity.Die`가 `onDeath`를 개별 `try/catch`로 호출(`GetInvocationList`)하고 오류는 `Debug.LogException`. 또는 `Zombie.Die`의 콜라이더·NavMesh 정리를 `base.Die()` 앞으로 옮긴다. 후자는 `dead`가 먼저 true가 되는 순서를 지켜야 하므로 전자를 우선.
- 검증: 임시로 `onDeath` 핸들러가 예외를 던지게 해 시체 콜라이더가 꺼지는지 확인(임시 코드 되돌림).
- 커밋: `fix: 사망 이벤트 예외가 시체 정리를 막지 않게`

## T1.2 목표 달성·사망 같은 프레임
- 방법: `EndGame`에서 `awaitingGoalChoice=false`로 두고 열려 있는 목표 결과 UI를 닫은 뒤(`UIManager.HideGoalResult`) 게임오버 표시. 이미 `isGameover`이면 중복 처리하지 않는다. 연습 모드: `PracticeBossMode.OnBossDied`가 이미 `isGameover`를 확인하므로 반대 순서(보스 처치 결과 후 같은 프레임 사망)만 `EndGame`이 결과 UI를 정리하는지 확인.
- 테스트: 상태 전이를 `MobileCore`의 순수 클래스로 빼기 어렵다면(UIManager 의존) 에디터 플레이 재현으로 대체. 가능하면 `GameEndState` 순수 클래스 분리.
- 검증: 결과 테스트 씬(`UrbanSurvival_ResultTest`)에서 목표 결과가 뜬 직후 `PlayerHealth.OnDamage`로 사망 → 게임오버만 남고 ENDLESS 버튼 없음. 반대 순서도 확인.
- 커밋: `fix: 같은 프레임 목표 달성·사망 시 패배 우선`

## T1.5 FIRE 버튼 이탈 시 발사 유지
- 방법: `TouchButton`에 `releaseOnExit`(기본 true) 필드 추가, FIRE·재장전이 아닌 조준 버튼만 false. `MobileTouchOverlay`가 FIRE 생성 시 false 지정. `OnPointerUp`/`OnDisable`에서만 해제.
- 테스트: `TouchButtonTests` — `PointerEventData`로 Down→Exit→Up 순서에서 `Pressed` 확인(EditMode에서 `EventSystem` 없이 가능한지 확인, 불가하면 포인터 ID 판정 로직을 순수 함수로 분리).
- 커밋: `fix: FIRE 버튼은 손가락이 밖으로 나가도 유지`

## T1.6 소환 부하와 실패 타이머
- 방법: (a) `SpawnBudget.Take(pending, alive, cap, perFrame)` 순수 함수(`MobileCore`)로 프레임당 소환 수 계산, 인스펙터 `maxSpawnsPerFrame`(기본 12). (b) 링 후보 0개 직후 `spawnRingRefreshInterval` 안에서는 재생성하지 않음. (c) 대기가 없거나 소환 성공 시 `spawnFailureStartTime` 초기화(연습 씬의 오래된 타이머가 종료를 일으키지 않게).
- 규칙 불변: 웨이브 간격 3~8초, 마릿수 4+2n, 동시 500 상한, 보스는 상한 무시.
- 테스트: `SpawnBudgetTests` (대기 0, 상한 도달, perFrame 제한, 음수/0 입력).
- 검증: 대기열 300마리에서 프레임 시간 전·후(프로파일러 또는 `Time.deltaTime` 샘플링). 연습 씬에서 10초 이상 소환 없음 후 1회 실패해도 게임 종료 안 됨.
- 커밋: `fix: 프레임당 소환 수 제한과 실패 타이머 초기화`

## T1.4 앱 백그라운드 자동 일시정지 (마지막)
- 방법: `MobileTouchOverlay.OnApplicationPause(true)`에서 `!isGameover && !awaitingGoalChoice && !isPaused`이면 `TogglePause`. 복귀 시 자동 재개 없음. 스틱 재획득(#9)은 일시정지 메뉴에서 재개하면 새 터치로 시작하는 흐름을 허용(별도 포인터 재획득 코드 없음) — 확정 필요.
- 검증: 에디터에서는 `OnApplicationPause(true)` 수동 호출로 대체, 기기 검증 전에는 "미검증".
- 커밋: `fix: 앱이 백그라운드로 가면 일시정지`

## 사용자 확인이 필요한 점
1. T1.1: 발광(붉은 기운) 구현 방식 — 공유 재질 변형(에셋 추가) vs 현재 재질 키워드 확인 후 결정. 에디터에서 재질을 보고 제안하겠다.
2. T1.4: 복귀 시 스틱은 새 터치로 시작해도 되는지.
3. 기존 파일 백업 여부(Phase 3와 같게 "백업 없이"로 진행해도 되는지).
