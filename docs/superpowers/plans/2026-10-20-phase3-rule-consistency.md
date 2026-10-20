# Phase 3. 규칙 일관성 — 상세 계획

- 작성일: 2026-10-20, 브랜치 `SideProject-Mobile`
- 상위 문서: 로드맵 `2026-10-12-code-review-roadmap.md` Phase 3, 스펙 `../specs/2026-10-13-code-review-remediation-design.md`(R3.x, D1~D4)
- 규칙: 코드 수정 전 사용자에게 백업 여부 확인. 빌드·실행·캡처는 요청 시에만. 씬은 건드리지 않는다(스크립트만).

## 범위 정리
| Task | 상태 | 비고 |
|---|---|---|
| T3.1 | 코드 변경 없음 | D1은 현재 동작 그대로(`RestoreHealth` 상한, 팩은 항상 `Destroy`). 순수 로직 분리 없이는 테스트 불가 → 테스트 추가 생략, 문서 안내만 |
| T3.2 | 구현 | 중복 무기 픽업 → 그 슬롯 예비탄 가산 |
| T3.3 | 구현 | 보스 사망 시 경고 중 레드존 취소 |
| T3.4 | **취소(대체됨)** | 2026-10-13 `a8082c8`(D1)로 자석이 가득 찬 상자도 끌어오게 되돌렸으므로 "필요량 이상 끌어오지 않기"는 규칙과 충돌 |
| T3.5 | 구현 | PC에서 재장전 중 버튼을 놓았다 다시 눌러도 재장전 직후 즉발하는 틈 막기 |
| T3.6 | 구현 | PC 발사 방향을 같은 프레임 조준값으로 |
| T3.7 | 구현 | 연습 씬 생존 시간 = HUD 시계, 난이도 상수 중복 제거 |
| (R3.3) | 구현 | UI 위에서 시작한 클릭이 필드로 끌려 나와도 발사 안 됨 |

## T3.2 중복 무기 픽업 예비탄 (D2)
- 새 순수 함수 `ReserveAmmo.Add(int current, int amount, int cap)` (`Assets/Scripts/MobileCore/ReserveAmmo.cs`): `cap < 0`(무제한)이면 `current` 그대로, 아니면 `Min(current + amount, cap)`; `amount <= 0`이면 변경 없음.
- `PlayerShooter.AddAmmoToSlot(WeaponSlot slot, int amount)` 추가: 해당 슬롯 `Gun.ammoRemain`에 `ReserveAmmo.Add` 적용. 기존 `AddAmmoToCurrentWeapon`도 같은 함수를 쓰게 정리(동작 동일).
- `WeaponPickup.Use`: 이미 보유한 슬롯(`IsSlotUnlocked`)이면 `AddAmmoToSlot(slot, gunData.startAmmoRemain)`, 처음이면 기존 `UnlockWeapon`. 항상 `Destroy`(상한이어도 습득·소모).
- **가정(확인 요청):** "회복량"은 그 무기의 초기 예비탄(`startAmmoRemain`: 소총 150 / SMG 200 / 산탄총 40)만큼 가산하고 상한에서 자른다. 완전 충전이 아니다.
- 테스트(EditMode, `ReserveAmmoTests`): 일반 가산, 상한 클램프, 이미 상한, 무제한(-1) 불변, amount 0/음수.

## T3.3 보스 사망 시 레드존 취소 (D4)
- `RedZone.Spawn(..., Zombie owner = null)`, 필드 `owner`/`hasOwner`.
- `Update`: 폭발 전(`!exploded`)에 `hasOwner && (owner == null || owner.dead)`이면 `Destroy(gameObject)`로 취소(피해 없음, 번쩍임 없음). 이미 폭발해 번쩍이는 중이면 그대로 둔다.
- 대기 시간(`startTime` 이전)에도 같은 검사를 먼저 수행(지금은 `delaySeconds`가 0이지만 방어).
- `BossRedZoneAttack.Cast`의 두 `RedZone.Spawn` 호출에 `zombie` 전달.
- 검증: 에디터 플레이(연습 씬 BossTestScene). 경고 중 보스 처치 → 레드존 사라지고 플레이어 체력 불변. 미검증 항목은 기록.

## T3.5 재장전 중 재누름 틈 (R3.2, PC만)
- 원인: `blockFireUntilRelease`는 재장전 시작 시 켜지지만, 매 프레임 "버튼이 떼어져 있으면 해제"되므로 재장전 도중 한 번 떼면 풀리고, 재장전이 끝나는 즉시 다시 누른 상태가 연사로 이어진다.
- 수정: 해제 조건에 `gun.state != Gun.State.Reloading`을 추가(모바일은 자동 재개 규칙이 있으므로 `MobilePlatform.IsMobile`이면 기존대로).
- 결과: 재장전이 끝난 뒤에 버튼을 한 번 놓았다 다시 눌러야 발사(AGENTS.md PC 규칙과 일치).

## T3.6 PC 발사 방향 (같은 프레임)
- 원인: 발사 방향이 `transform.forward`인데 캐릭터 회전은 `FixedUpdate`의 `MoveRotation`으로 다음 물리 스텝에 적용된다.
- 수정: `PlayerMovement.GetAimDirection()`을 `public`으로 열고, `PlayerShooter`가 PC에서 `hasAimWorldDirection`이 false일 때 `playerMovement.GetAimDirection()`을 사용(없으면 `transform.forward`). `GetAimDirection`이 마지막 유효 방향을 유지하는 동작은 그대로.
- 위험: `PlayerInput.Update`가 `PlayerShooter.Update`보다 먼저 돌아야 `aimPosition`이 최신이다(발사 입력도 같은 전제라 기존과 동일).

## T3.7 연습 씬 생존 시간·난이도 상수
- `GameManager`에 `ElapsedSeconds`(`FloorToInt(elapsedMinutes * 60)`) 추가. `UIManager`의 `Time.timeSinceLevelLoad` 3곳(게임오버, 목표 스냅샷, 목표 결과)을 이것으로 교체. 메인 씬은 `startElapsedMinutes`가 0이라 동일, 연습 씬은 HUD 시계와 일치.
- `ZombieSpawner.difficultyRampMinutes`(`private readonly` 30)를 `public const float DifficultyRampMinutes`로 바꾸고 `PracticeSelectMenu`가 이를 참조. `PracticeSelectMenu.difficultyRampMinutes` 직렬화 필드 삭제(씬에는 옛 값이 남지만 무해).

## R3.3 UI에서 시작한 클릭이 발사로 이어지지 않게
- 원인: 마우스를 UI 버튼 위에서 누르면 `IsPointerBlockedForFire`가 막지만, 누른 채 필드로 끌면 `GetButton`이 계속 true여서 막힘이 풀리며 발사된다.
- 수정: `PlayerInput.Update`의 PC 차단 분기에서 `fireLatch.RequireRelease()`도 호출해 버튼을 뗄 때까지 래치 유지.

## 검증
1. EditMode 전체(현재 98건 + `ReserveAmmoTests`) 통과 — Unity MCP `run_tests` 또는 배치 모드.
2. 컴파일 오류 0건(`read_console`).
3. 에디터 플레이: T3.2(무기 픽업 중복), T3.3(연습 씬 보스), T3.5(재장전 중 놓았다 누르기), T3.6(회전 직후 발사 방향), R3.3(UI에서 끌기). `Application.runInBackground`가 켜져 있어야 함(현재 켜져 있음).
4. 확인하지 못한 것은 `docs/verification/2026-10-20-phase3.md`에 "미검증"으로 남긴다.

## 완료 후 사용자 안내(문서 갱신)
- `AGENTS.md`: 자석 규칙 서술(`ca6bee9` → `a8082c8` 되돌림), D2(중복 무기 예비탄), D4(레드존 취소), 테스트 건수.
- 기획서: 13장 D1 문구, D2·D4 추가(버전 번호는 업로드 기준).

## 커밋
- 작업 단위별로 커밋하며 자동 재저장 파일(TMP, ProjectSettings 등)은 포함하지 않는다. 푸시는 사용자 확인 후.
