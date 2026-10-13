# 전체 코드리뷰 후속 작업 로드맵

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans. 이 문서는 로드맵이다. 각 Phase를 시작하기 직전에 해당 Phase의 상세 단계(테스트 코드 포함)를 별도 계획 파일로 쓴 뒤 실행한다.

**Goal:** 2026-10-12 전체 코드리뷰(핵심 전투, 스포너·UI, 모바일, 저장소 4개 영역)에서 나온 문제를 위험도 순으로 해결한다.

**Architecture:** 변경은 `SideProject-Mobile`에서 먼저 하고, 검증 후 PC `SideProject`로 옮긴다. 순수 로직으로 뺄 수 있는 것은 `Assets/Scripts/MobileCore/`(EditMode 테스트 가능)에 두고, 씬 연결이 필요한 것은 에디터 플레이로 검증한다.

**Tech Stack:** Unity 6000.3.19f1, URP 17.3.0(호환 모드), C#, EditMode 테스트(현재 98건).

**Spec:** `docs/superpowers/specs/2026-10-13-code-review-remediation-design.md`(요구사항 R1~R5, 결정 D1~D7). 게임 규칙의 기준은 `AGENTS.md`, 리뷰 항목 번호의 출처는 2026-10-12 리뷰 보고(인수인계 문서 4장에 요약).

## Global Constraints
- 브랜치: 모바일 변경은 `SideProject-Mobile`, PC 반영은 `SideProject`. `main`은 수정·커밋하지 않는다.
- 기존 파일 수정 전에 사용자에게 백업 여부를 확인한다(파일명 `원본_YYYYMMDD_HHMM_백업.확장자`). 새 파일만 만드는 작업은 제외.
- 빌드(APK·AAB·exe)는 사용자가 요청할 때만 한다. 2026-10-12 결정: 1.0.4 빌드는 만들지 않고 다음 업데이트에 묶는다.
- Unity 내부 작업(씬·프리팹·컴포넌트)은 Unity CLI/MCP로 한다. 씬이 바이너리라 diff가 안 되므로 씬 수정 후 `git diff --stat`로 범위를 확인한다.
- Windows에서 `.cs`를 Edit하면 Rider 훅이 재포맷할 수 있다. 수정 후 `git diff --stat`으로 의도하지 않은 변경이 없는지 확인한다.
- Unity 자동 재저장 파일(TMP, URP 설정, ProjectSettings 등)은 커밋하지 않는다.
- 규칙은 `AGENTS.md`가 우선한다. 규칙을 바꾸는 작업은 먼저 사용자 결정을 받고, 결정 뒤 `AGENTS.md`와 기획서 갱신을 사용자에게 안내한다.
- 검증 없이 완료를 선언하지 않는다. 기기·에디터 플레이로 확인하지 못한 항목은 "미검증"으로 남긴다.

## Review Focus
- 같은 프레임에 보스 처치와 플레이어 사망이 겹친다: 패배가 우선하고 결과 화면은 뜨지 않아야 한다.
- 앱이 백그라운드로 갔다 돌아온다: 일시정지 메뉴가 떠 있어야 하고 전투가 재개되면 안 된다.
- 무한 모드를 30분 이상 돌린다: 재질 인스턴스·GameObject·대기열이 무한히 늘지 않아야 한다.
- 소환 대기열이 수백 마리일 때 웨이브가 나온다: 프레임 스파이크가 없어야 한다.
- 사망 핸들러 중 하나가 예외를 던진다: 시체가 총알을 막거나 에이전트가 남으면 안 된다.

---

## Phase 0. 사용자 결정 (2026-10-12 수신, 아래 표는 질문 원문, 결정 반영은 이 표 아래)

### 결정 결과 (2026-10-12 사용자 답변)
| ID | 결정 | 로드맵 반영 |
|---|---|---|
| D1 | 각 수치(체력)를 최대치로 회복하고 최대치를 넘길 수 없다. **(2026-10-13 확정)** 가득 찬 상태여도 아이템은 습득(소모)하되 실제 회복량만 최대치를 넘지 못한다 | **T3.1 코드 변경 없음.** 현재 동작과 같다: `LivingEntity.RestoreHealth`가 `startingHealth`로 제한, `HealthPack`·`AmmoPack`은 사용 후 항상 `Destroy`(2026-10-13 코드 확인). 남은 일은 기획서 13장 문구("가득 차면 소비하지 않음")를 "습득은 되고 회복량은 상한까지"로 고치는 문서 갱신(사용자 몫)과, 자석 버프가 가득 찬 상자를 끌어오지 않는 규칙(`MagnetPullRules`)을 그대로 둘지 확인하는 것 |
| D2 | 이미 가진 무기를 주우면 예비탄을 회복(상한 초과 불가). 이미 상한이면 D1과 같은 판정(습득되고 초과분은 버려짐) | T3.2 진행: `WeaponPickup.Use`가 이미 보유한 슬롯이면 `AddAmmo`를 해당 슬롯의 `reserveAmmoCap`까지 가산하고 항상 `Destroy` |
| D3 | 엄폐물과 상관없이 현재 동작 유지, 무타깃 FIRE도 억제하지 않음 | **T4.3 취소, T4.4 중 무타깃 FIRE 정책 취소.** 히스테리시스(조준선 튐 완화)는 결정 대상이 아니라 미정 상태로 보류 |
| D4 | 보스 사망 시 경고 중인 레드존 취소 | T3.3 진행 |
| D5 | 보류 | T5.1 보류(키스토어 경로 처리 안 함) |
| D6 | 포트폴리오 용도라 상관없음 | T5.2 중 협업자 정책 취소. 이메일 오타 변형 가림 확장은 공개본 푸시 시에만 필요 |
| D7 | 질문: "GUI는 이미 빠진 게 아니었나?" | 답변: 모바일 브랜치에서만 빠졌다. 아래 참고 |

**D7 확인 결과(2026-10-12 `git ls-tree`):** GUI PRO Kit 추적 파일 수는 `SideProject-Mobile` 0개, `main` 0개, **`SideProject`(PC) 4,961개**다. 모바일 빌드에서는 빠졌지만 PC 브랜치에는 비공개 origin 이력에 그대로 있다. 공개본은 `publish-clean-repo.sh`가 이력에서 지운다. 따라서 "공개 시 GUI 포함 여부"는 이미 정해진 방침(공개본 제외)이고, 남은 문제는 PC 브랜치를 공개로 전환하거나 협업자를 추가하는 경우뿐이다. T5.5의 경고 문구만 유지한다.

### 질문 원문 (참고)
아래는 코드만으로 정답이 정해지지 않는 규칙이다.

| ID | 질문 | 영향 Task | 리뷰 출처 |
|---|---|---|---|
| D1 | 일반 습득(3 m 트리거)에도 "가득 차면 소비하지 않음"을 적용할까? (지금은 자석 버프만 적용) | T3.1 | 핵심 #2 |
| D2 | 이미 가진 무기를 다시 주우면 예비탄을 줄까? (드랍 40%라 체감 큼) | T3.2 | 핵심 #18 |
| D3 | 자동 조준: 엄폐물 뒤의 적을 후순위로 하고, 적이 없을 때 FIRE는 발사를 억제할까? | T4.3, T4.4 | 모바일 #4, #7 |
| D4 | 보스를 먼저 처치하면 이미 경고 중인 레드존을 취소할까? | T3.3 | 핵심 #17 |
| D5 | 업로드 키스토어 경로를 어떻게 숨길까? (상대 경로 / 환경변수 / 빌드 스크립트로 주입) | T5.1 | 모바일 #6, 저장소 #1 |
| D6 | 공개본에서 협업자(`contributor@noreply.invalid`) 이름·주소를 어떻게 처리할까? | T5.2 | 저장소 #2 |
| D7 | 공개 시점과 `README`/`LICENSE` 종류, GUI PRO 포함 여부 최종 확인 | Phase 5 전체 | 저장소 #3, #5 |

---

## Phase 1. 안정성 버그 (최우선, `SideProject-Mobile`)

목표: 장시간 플레이·앱 전환에서 깨지거나 규칙에 어긋나는 문제를 먼저 제거한다. 완료 기준: EditMode 전체 통과 + 아래 각 검증 + 에디터 플레이 샘플링.

### T1.1 좀비 재질 인스턴스 누수 제거 (L)
- 출처: 핵심 #1, 스포너 #6
- Files: Modify `Assets/Scripts/Zombie.cs:97-108`
- 방법: `bodyRenderer.material` 복제를 없애고 `MaterialPropertyBlock`으로 색(Final Boss 붉은 기운 등)을 넣는다. 렌더러별로 블록을 재사용한다.
- 검증: 에디터 플레이에서 좀비 100마리 소환·사망 반복 후 `Resources.FindObjectsOfTypeAll<Material>().Length`가 증가하지 않는다. 보스/강화/일반 색이 이전과 같다.
- 커밋: `fix: 좀비 재질 복제 제거(MaterialPropertyBlock)`

### T1.2 목표 달성·사망 같은 프레임 처리 (M)
- 출처: 핵심 #16, 스포너 #5
- Files: Modify `Assets/Scripts/GameManager.cs:184-193`(`EndGame`), `Assets/Scripts/PracticeBossMode.cs:108-135`, 필요 시 `UIManager`(`HideGoalResult`)
- 방법: `EndGame`이 먼저 `awaitingGoalChoice=false`로 하고 열려 있는 목표 결과 UI를 닫은 뒤 게임오버를 띄운다. `isGameover`이면 중복 처리하지 않는다. 연습 모드는 결과 확정 순서를 같은 규칙으로 맞춘다.
- 검증: 결과 테스트 씬에서 목표 결과가 뜬 프레임에 플레이어를 사망시키는 에디터 스크립트로 재현 → 게임오버만 남고 ENDLESS 버튼이 없다. 반대 순서(사망 후 보스 처치)도 확인.
- 테스트: 상태 전이를 순수 클래스(`MobileCore`)로 분리할 수 있으면 EditMode 테스트 추가(`GameEndState`: 목표 선택 대기 중 EndGame 호출 → 대기 해제).
- 커밋: `fix: 같은 프레임 목표 달성·사망 시 패배 우선`

### T1.3 사망 처리 순서 견고화 (S)
- 출처: 핵심 #4
- Files: Modify `Assets/Scripts/LivingEntity.cs:57-63`, `Assets/Scripts/Zombie.cs:259-275`
- 방법: 콜라이더·NavMeshAgent·애니메이션 정리를 `onDeath` 호출 앞으로 옮기거나, 핸들러를 개별 try/catch로 호출하고 오류는 `Debug.LogException`.
- 검증: 임시로 `onDeath` 핸들러에서 예외를 던지게 해 시체의 콜라이더가 꺼지는지 확인(임시 코드는 되돌린다).

### T1.4 앱 백그라운드 자동 일시정지 (M)
- 출처: 모바일 #1, #9
- Files: Modify `Assets/Scripts/MobileUI/MobileTouchOverlay.cs:303-311`, `Assets/Scripts/GameManager.cs`(`TogglePause` 호출 지점)
- 방법: `OnApplicationPause(true)`에서 `!isGameover && !awaitingGoalChoice && !isPaused`이면 일시정지. 복귀 시 스틱이 끊기는 문제(#9)는 일시정지 메뉴 복귀 흐름에서 재누름이 필요하다는 점을 허용할지 결정하고, 아니면 활성 포인터 재획득을 추가한다.
- 검증: 기기에서 홈 버튼 후 복귀 시 일시정지 메뉴(Android 기기 필요, 에디터에서는 `OnApplicationPause` 수동 호출로 대체). 기기 검증 전에는 "미검증"으로 기록.

### T1.5 FIRE 버튼 이탈 시 발사 유지 (S)
- 출처: 모바일 #5
- Files: Modify `Assets/Scripts/MobileUI/TouchButton.cs:61-67`
- 방법: FIRE와 이동·조준 버튼은 `OnPointerExit`에서 해제하지 않고 `OnPointerUp`/`OnDisable`에서만 해제. 슬롯·일반 버튼은 기존 유지.
- 테스트: `PointerEventData`로 Enter→Exit→Up 순서에서 `Held` 상태를 확인하는 EditMode 테스트 추가(`TouchButtonTests`).

### T1.6 소환 부하와 실패 타이머 (M)
- 출처: 스포너 #1, #2, #7
- Files: Modify `Assets/Scripts/ZombieSpawner.cs:245-278`(`SpawnPending`), `:324`(링 풀 갱신), `:467-512`(실패 타이머)
- 방법: (a) 프레임당 소환 수 상한(기본 12, 인스펙터 필드). (b) 링 후보 0개여도 `spawnRingRefreshInterval` 안에서는 재생성하지 않음. (c) 소환 성공 또는 대기 없음일 때 `spawnFailureStartTime` 초기화.
- 영향 규칙: AGENTS.md의 "3~8초 간격 웨이브, 마릿수 4+2n, 500마리 상한"은 바뀌지 않는다. 한 웨이브가 여러 프레임에 걸쳐 나온다는 점만 달라진다.
- 검증: 대기열 300마리 상태에서 프로파일러로 프레임 시간 비교(전·후). 연습 씬에서 10초 이상 소환이 없다가 한 번 실패해도 게임이 종료되지 않는다.
- 테스트: 프레임당 소환 수 계산을 순수 함수로 빼서 EditMode 테스트(`SpawnBudget.Take(pending, alive, cap, perFrame)`).

**Phase 1 종료 조건:** EditMode 전체 통과, 위 검증 기록을 `docs/verification/`에 남김, 커밋 6개 이내.

---

## Phase 2. GC·성능 정리 (`SideProject-Mobile`)

목표: 모바일 GC 스파이크와 불필요한 반복 호출 제거. 동작 변경이 없어야 한다(규칙·수치 동일).

| Task | 내용 | Files | 출처 |
|---|---|---|---|
| T2.1 | HUD·웨이브·준비 텍스트를 마지막 값 캐시 후 변경 시에만 갱신 | `ZombieSpawner.cs:137`, `UIManager.cs:75-82`, `HealthHUD.cs:35`, `BossHealthUI.cs:31`, `PracticeBossMode.cs:104`, `WeaponHUD.cs:38` | 스포너 #3 |
| T2.2 | 미니맵이 `FindObjectsByType<Zombie>` 대신 `Zombie.alive` 사용 | `ThreatMinimap.cs:66` | 스포너 #4 |
| T2.3 | `Zombie` 경로 갱신: `WaitForSeconds` 정적 캐시, 시작 시 0~0.25초 무작위 지연, 사망 후 `OverlapSphereNonAlloc`+정적 버퍼(감지 범위는 그대로), `Animator.StringToHash` 캐시와 값 변경 시에만 `SetBool`, `isOnNavMesh` 가드 | `Zombie.cs:135,154,160,163,183,270` | 핵심 #3, #12, #13 |
| T2.4 | 발사 할당 제거: 궤적 배열 재사용, `WaitForSeconds` 정적 캐시, `OnDisable`에서 궤적 라인 끄기와 재장전 상태 복구 | `Gun.cs:149,187,213-244,305-336` | 핵심 #8~#10 |
| T2.5 | PC 연사 중 UI 판정 할당 제거(`PointerEventData` 재사용) + UI 위에서 막을 때 `fireLatch.RequireRelease()` | `PlayerInput.cs:89-98,147-151` | 핵심 #6, #11 |
| T2.6 | 모바일 조준 모드 캐시(`Enum.IsDefined` 제거, `PlayerPrefs`는 set/ResetOnPlay에서만 읽기) | `MobileCore/MobileAimMode.cs:17-21`, `PlayerInput.cs:194` | 모바일 #2 |
| T2.7 | 지형·무기 아이콘 카메라를 2~3프레임마다 수동 `Render()`, 일시정지·가림 중 중지 | `MinimapTerrainView.cs:60-91`, `WeaponIconRig.cs:114-137` | 스포너 #13 |
| T2.8 | 소소한 정리: `ItemMagnet` 평소 `enabled=false`, `RemoveAll` 델리게이트 캐시, `BossHeadBar` 사망 시 `Destroy`, `RedZone`의 `FindFirstObjectByType` 캐시 | `ItemMagnet.cs:61`, `ZombieSpawner.cs:103`, `BossHeadBar.cs:69-72`, `RedZone.cs:41` | 스포너 #9,#10, 핵심 #17,#20 |

- 각 Task 검증: EditMode 통과 + 에디터 플레이에서 동작 동일 + 프로파일러 GC Alloc 전·후 수치를 `docs/verification/`에 기록.
- 커밋은 Task별 1개씩.
- 순서: T2.3 → T2.1 → T2.2 → T2.6 → 나머지(독립적이라 병렬 가능).

---

## Phase 3. 규칙 일관성 (Phase 0 결정 후)

| Task | 내용 | 선행 | Files |
|---|---|---|---|
| T3.1 | **(변경됨, 2026-10-13)** D1 확정에 따라 코드 변경 없음. 일반 습득은 가득 차도 소모되고 회복량만 상한으로 제한되며 이미 그렇게 동작한다. 할 일: (a) 이 동작을 고정하는 EditMode 테스트 추가 여부 결정(체력은 `LivingEntity`가 MonoBehaviour라 순수 로직 분리 필요), (b) 기획서 13장 문구 갱신 안내 | D1 | 없음(문서만) |
| T3.2 | D2에 따라 중복 무기 획득 시 예비탄 가산(상한까지, 가득 차도 습득·소모). `PlayerShooter`에 슬롯 지정 탄약 가산 메서드 추가 | D2 | `WeaponPickup.cs:38-39`, `PlayerShooter.cs` |
| T3.3 | D4에 따라 레드존이 소유 보스 사망 시 취소 | D4 | `RedZone.cs:79-103`, `BossRedZoneAttack.cs:35-63` |
| T3.4 | 자석이 필요량 이상 상자를 끌어오지 않게 비행 중 수량 반영 | 없음 | `MagnetBuff.cs:130-141` |
| T3.5 | 재장전 중 놓았다 다시 누르면 연사 무기가 즉발하는 틈 막기(PC 경로만) | 없음 | `PlayerShooter.cs:102-105` |
| T3.6 | PC 발사 방향을 같은 프레임 `aimWorldDirection`으로 | 없음 | `PlayerShooter.cs:129`, `PlayerMovement.cs:119` |
| T3.7 | 연습 씬 생존 시간 표기와 난이도 상수 정리 | 없음 | `UIManager.cs:94,119,154`, `PracticeSelectMenu.cs:18` |

- 규칙을 바꾸는 Task(T3.1~T3.3)는 완료 후 `AGENTS.md`와 기획서 문구 갱신 목록을 사용자에게 알려 준다(기획서 버전 번호는 사용자 업로드 기준으로 갱신).

---

## Phase 4. 모바일 호환성과 테스트 보강

| Task | 내용 | Files |
|---|---|---|
| T4.1 | 모든 HUD 캔버스(자석 HUD 포함)에 `SafeAreaFitter` 적용. 노치 폰 에뮬레이터 또는 시뮬레이터 `Device Simulator`로 확인 | `MagnetBuff.cs:225`, `MobileHudLayout.cs`, 기존 HUD 캔버스 |
| T4.2 | 설정 RESET이 TWIN-STICK도 초기화(`AutoAim`) | `MobileAimModeSettingsBinder.cs:67-71`, `SettingsPanel.cs:148` |
| T4.3 | D3에 따라 자동 조준에 가시성(0.45 m 수평 Raycast) 필터. 가시성 콜백을 순수 함수 시그니처에 주입해 EditMode 테스트 | `MobileCore/AutoAimTargeting.cs`, `PlayerInput.cs:268-283` |
| T4.4 | 자동 조준 히스테리시스(새 대상이 20% 이상 가까울 때만 교체), 무타깃 FIRE 정책(D3) | `AutoAimTargeting.cs`, `PlayerInput.cs:224` |
| T4.5 | 무기 슬롯과 조준 영역 겹침 해소 | `MobileTouchOverlay.cs:140,234` |
| T4.6 | `MobileFireLogic`: 재장전 재개를 "Reloading→Ready 전이"로 제한, 무기 교체 시 `Reset()` | `MobileCore/MobileFireLogic.cs:46-60` |
| T4.7 | 테스트 보강: `MobileFireLogic`(무기 교체·None 전이), `TouchJoystick/TouchButton` 포인터 ID, `SafeAreaFitter` 앵커, `MagnetPullRules` 경계값(`reserveCap==0`, 음수), `PlayerInput` 입력 로직 순수 클래스 분리 | `Assets/Game/Tests/EditMode/` |
| T4.8 | `Apply Android Settings`에 버전·Analytics·TV 호환 설정을 코드로 담기(재현성). 적응형 아이콘 판별 확인 | `Editor/MobileEditorMenu.cs` |
| T4.9 | 120 Hz 기기에서 프레임 제한과 vSync 확인(기기 필요) | `MobilePerformance.cs`, `SettingsStore.cs` |

- Phase 4 종료 조건: EditMode 건수가 98건에서 늘어나고 전체 통과. 기기가 필요한 T1.4, T4.1, T4.9는 기기 검증 결과를 `docs/verification/`에 기록하거나 "미검증"으로 명시.

---

## Phase 5. 저장소·공개 준비 (공개 전 필수, 비공개 유지 중에는 낮은 우선순위)

선행: D5, D6, D7. 이 Phase의 작업은 `SideProject-Mobile`과 `tools/`, `docs/`에 한정한다. 공개본 푸시는 사용자가 요청할 때만 한다.

| Task | 내용 | Files |
|---|---|---|
| T5.1 | 키스토어 경로 제거(D5 방식). 이력에 남은 실명 경로는 `publish-clean-repo.sh` SCRUB에 `(author)`과 키스토어 경로를 추가. `FbxExportSettings.asset:12`의 타인 경로, `docs/superpowers/specs/2026-09-21-...`의 로컬 경로도 정리 | `ProjectSettings/ProjectSettings.asset:274`, `tools/publish-clean-repo.sh`, 문서 |
| T5.2 | 이메일 가림 확장: 배열 또는 로컬 파트(`ssaqwe123`) 부분 일치, 오타 변형 `ssaqwe123.gmail.com` 포함, 협업자 정책(D6). 검증 단계에 `git log --format='%an %ae %cn %ce' | sort -u` 출력 확인 추가 | `tools/publish-clean-repo.sh` |
| T5.3 | 스크립트 `--push`에서 `--all` 제거 또는 가드(AGENTS.md의 병합 커밋 절차와 일치), 푸시 전 로컬·origin 동기화 점검 추가(`SideProject`가 origin보다 2커밋 뒤처져 있음) | `tools/publish-clean-repo.sh` |
| T5.4 | `.gitignore` 보강(`*.zip`, `/Update-Build/`, `AGENTS_*_백업.md`, `.aiassistant/`, `Assets/_Recovery*`), `.gitattributes`에 Unity YAML 병합 설정 추가 | `.gitignore`, `.gitattributes` |
| T5.5 | 라이선스 문서 현행화: GUI PRO 상태(PC `SideProject` 비공개 origin에 4,960~4,961개 추적), Toon Shooter `License.txt` 부재, 예제 저장소 출처 효과음·모델의 허락 증빙, PC에서 `Gun Shoot.wav` 교체 반영 여부 확인. 비공개를 공개로 전환·협업자 추가 금지 경고 문구 | `docs/store/asset-license-audit.md`, `docs/store/third-party-not-in-repo.md` |
| T5.6 | `README.md`와 `LICENSE` 추가(종류는 D7), 크레딧 화면 필요 여부 결정 | 루트 |
| T5.7 | 대용량 음원 `singularity_calm.wav`(51.4 MB) ogg 전환 검토, `git gc`로 임시 객체 정리 | `Assets/Game/Audio/Music/` |
| T5.8 | 비공개 `main`(`74cc39a`에서 멈춘 옛 상태) 정리 방침 결정(삭제하지 않고 기록만 할지) | 사용자 결정 |

- 공개 전 최종 점검: `publish-clean-repo.sh`로 만든 사본에서 `git grep -I "(author)"`, `git log` 작성자 목록, GUI PRO 폴더·GUID 참조, 100 MB 초과 파일을 확인한 뒤 사용자 확인을 받고 푸시한다.

---

## Phase 6. PC `SideProject` 반영과 릴리스 정리

- T6.1 Phase 1~4의 커밋을 `SideProject`로 옮긴다(임시 워크트리 사용, Unity 배치 모드로 컴파일·EditMode 확인). PC는 로컬이 origin보다 2커밋 뒤처져 있으므로 먼저 동기화한다. 모바일 전용 파일(`MobileCore`, `MobileUI`)은 옮기지 않는다. 순수 로직은 PC 위치 규칙(`Assets/Scripts/`)에 맞춘다.
- T6.2 PC에서 Unity를 열어 씬 4개에 자석 프리팹 연결, `ZombieSpawner`의 `Resources.Load` 대체 정리, 컴파일 오류 확인(AGENTS.md "PC 브랜치 동기화" 항목의 남은 일).
- T6.3 다음 업데이트(1.0.4 이후)에 묶을 변경 목록을 `docs/store/release-notes.md`와 `docs/store/patch-plan.md`에 반영(빌드는 사용자가 요청할 때만).
- T6.4 `AGENTS.md` 갱신 제안 목록(규칙 변경 T3.x, 테스트 건수, 기기 검증 현황)을 사용자에게 전달. `AGENTS.md`는 Git에 올리지 않고 사용자가 직접 옮긴다.

---

## 실행 순서와 의존성

```
Phase 0 (결정) ─────────────────────────┐
Phase 1 (안정성) ─→ Phase 2 (GC) ──┐     │
                                   ├→ Phase 6 (PC 반영, 릴리스 정리)
Phase 4 (모바일·테스트) ───────────┤     │
Phase 3 (규칙, D1~D4 후) ←─────────┘─────┘
Phase 5 (저장소, D5~D7 후, 공개 전) ─ 독립 진행 가능
```

- Phase 1이 최우선이다. T1.1~T1.3은 서로 독립이라 병렬 서브에이전트로 나눌 수 있다.
- Phase 2의 Task는 독립적이라 병렬 가능하나 `Zombie.cs`를 건드리는 T1.1, T1.3, T2.3은 같은 파일이므로 순차로 한다.
- Phase 5는 코드와 무관해 Phase 1과 병렬로 진행할 수 있다. 단, 공개 푸시는 사용자 요청 전까지 하지 않는다.
- 각 Phase 시작 시 현재 브랜치와 작업 트리 상태를 확인하고, 사용자의 기존 변경을 덮어쓰지 않는다.

## 규모 추정
| Phase | Task 수 | 규모 | 기기 필요 |
|---|---|---|---|
| 0 | 결정 7건 | 대화 1회 | 없음 |
| 1 | 6 | 중 | T1.4 |
| 2 | 8 | 중 | 없음(프로파일러) |
| 3 | 7 | 중 (결정 의존) | 없음 |
| 4 | 9 | 중~대 | T4.1, T4.9 |
| 5 | 8 | 중 | 없음 |
| 6 | 4 | 소~중 | PC Unity 열기 |

## 완료 정의
- EditMode 전체 통과(현재 98건 기준, 증가).
- 각 Task의 검증 결과를 `docs/verification/`에 날짜별로 남기고, 확인하지 못한 항목은 "미검증"으로 표시.
- 작업 로그를 `D:\Codex\Log\YYYYMMDD_(작업이름)_Log.md`에 기록.
- 규칙·수치가 바뀐 경우 `AGENTS.md`·기획서 갱신 안내를 사용자에게 전달.
