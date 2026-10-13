# 코드리뷰 후속 개선 설계 (스펙)

- 작성일: 2026-10-13 (리뷰 2026-10-12, 사용자 결정 2026-10-12~13 반영)
- 짝 문서: 계획/로드맵 `docs/superpowers/plans/2026-10-12-code-review-roadmap.md`, 인수인계 `WorkNotes/20261012_코드리뷰_인수인계.md`
- 상위 규칙: 프로젝트 루트 `AGENTS.md`(Git 제외), 기획서 v1.22(`Downloads\Urban_Survival_상세_게임_기획서_수정본_20261013_v1.22.docx`)
- 근거 수준: 리뷰 항목은 읽기 전용 리뷰어가 코드를 읽고 확인한 것이며 에디터 플레이·기기로 재현하지 않았다. 구현 전에 각 항목을 재현하거나 코드로 다시 확인한다.

## 1. 목적과 범위
2026-10-12 전체 코드리뷰에서 나온 문제를 해결해 (a) 장시간 플레이와 앱 전환에서 깨지지 않게 하고, (b) 모바일 GC·프레임 부담을 줄이며, (c) 결정된 규칙과 코드를 일치시키고, (d) 비공개 저장소를 공개할 수 있는 상태로 정리한다.

**범위 안:** `Assets/Scripts` 전투·스포너·UI·모바일, EditMode 테스트, `tools/publish-clean-repo.sh`, `.gitignore`/`.gitattributes`, `docs/store` 라이선스 문서, 기획서 문구.
**범위 밖:** 새 기능, 밸런스 수치 변경, Render Graph 전환, 입력 시스템 전환, 빌드·스토어 업로드(요청 시에만), 멀티플레이·성장 트리 등 기존 제외 기능.

## 2. 확정된 규칙 (사용자 결정)
| ID | 규칙 | 구현 의미 |
|---|---|---|
| D1 | 회복·탄약 아이템은 체력·예비탄이 가득 차 있어도 **습득(소모)** 된다. 실제 회복량만 최대치를 넘지 못한다 | 일반 습득은 이미 이렇게 동작(`LivingEntity.RestoreHealth`, `HealthPack`/`AmmoPack`의 `Destroy`). **자석 버프도 가득 찬 상자를 끌어온다** — `MagnetPullRules`·`CanBenefit` 제거(2026-10-13 구현) |
| D2 | 이미 가진 무기를 다시 주우면 그 무기 예비탄을 회복(상한 초과 불가), 상한이어도 습득·소모 | `WeaponPickup.Use`가 보유 슬롯이면 슬롯별 예비탄을 `reserveAmmoCap`까지 가산 |
| D3 | 자동 조준은 엄폐물과 무관하게 현재 동작 유지, 적이 없어도 FIRE를 억제하지 않음 | 변경 없음(리뷰 모바일 #4·#7 취소) |
| D4 | 보스가 죽으면 경고 중인 레드존을 취소 | `RedZone`이 소유 보스를 알고 폭발 전에 사망이면 취소(피해 없음) |
| D5 | 키스토어 경로 처리는 보류 | T5.1 보류 |
| D6 | 협업자 주소 가림은 하지 않음(포트폴리오 용도) | T5.2 협업자 정책 취소 |
| D7 | GUI PRO Kit는 모바일 브랜치에서만 빠졌다(`SideProject` 4,961개 추적) | 공개본은 스크립트가 지움. PC 브랜치 공개 전환·협업자 추가 시에만 조치 |

## 3. 요구사항 (검증 가능한 문장)
### R1 안정성
- R1.1 좀비를 몇 마리 소환·사망시켜도 `Material` 인스턴스 수가 늘지 않는다. 보스·강화·일반·Final Boss의 색은 이전과 같다.
- R1.2 같은 프레임에 1차 목표 달성과 플레이어 사망이 겹치면 **게임오버만** 남고 결과 선택 화면과 ENDLESS 버튼은 없다. 반대 순서도 동일. 연습 모드도 같은 규칙.
- R1.3 `onDeath` 핸들러가 예외를 던져도 시체의 콜라이더와 NavMeshAgent는 꺼진다.
- R1.4 모바일에서 앱이 백그라운드로 가면(`OnApplicationPause(true)`) 게임오버·결과 대기·이미 일시정지 상태가 아닐 때 일시정지 상태가 된다. 복귀 시 자동 재개하지 않는다.
- R1.5 FIRE 버튼은 누른 손가락이 버튼 밖으로 나가도 손가락을 뗄 때까지 눌림 상태를 유지한다.
- R1.6 대량 대기열이 있어도 한 프레임에 소환하는 수는 상한(기본 12, 인스펙터 조정)을 넘지 않는다. 웨이브 간격·마릿수·500마리 상한 규칙은 바뀌지 않는다. 링 후보가 없을 때 소환 1마리마다 후보를 다시 만들지 않는다. 일반 소환이 없는 연습 씬에서 오래된 실패 타이머로 판이 종료되지 않는다.

### R2 성능(동작 동일)
- R2.1 HUD·웨이브·준비 텍스트는 표시 값이 바뀔 때만 문자열을 만든다.
- R2.2 미니맵은 `FindObjectsByType` 대신 `Zombie.alive`를 쓴다.
- R2.3 좀비 경로 갱신 루프는 `WaitForSeconds`를 재사용하고, 같은 프레임 몰림을 시작 지연으로 분산하며, 사망 후 `OverlapSphere` 배열을 매번 할당하지 않고(감지 범위 500 m 유지), 애니메이터 파라미터는 해시를 캐시한다.
- R2.4 발사 한 번에 managed 할당이 없다(궤적 배열·`WaitForSeconds` 재사용). 총을 끄면 궤적 라인도 꺼진다.
- R2.5 모바일 조준 모드 읽기는 프레임마다 `Enum.IsDefined`·`PlayerPrefs`를 호출하지 않는다.
- R2.6 지형·무기 아이콘 카메라는 매 프레임 렌더하지 않는다(일시정지·가림 중 중지).

### R3 규칙 일관성
- R3.1 D1·D2·D4를 위 표대로 구현한다. 가득 찬 상태 습득 테스트로 고정한다.
- R3.2 재장전 중 버튼을 한 번 떼었다 다시 눌러도 PC 연사 무기가 재장전 종료 즉시 발사하지 않는다(모바일은 기존 자동 재개 유지).
- R3.3 UI 위에서 시작한 클릭은 필드로 끌어도 발사로 이어지지 않는다.
- R3.4 연습 씬의 게임오버·결과 생존 시간이 HUD 시계와 같다.

### R4 모바일 호환성·테스트
- R4.1 모든 HUD 캔버스(자석 HUD 포함)는 안전 영역 안에 놓인다.
- R4.2 설정 RESET은 TWIN-STICK 설정도 기본값(오토 에임)으로 되돌린다.
- R4.3 무기 슬롯 터치 영역과 조준 영역이 겹치지 않는다. 일시정지 복귀 시 스틱 동작은 명시적으로 결정한다.
- R4.4 `MobileFireLogic`의 자동 재개는 "재장전 → 준비" 전이에서만 발동하고 무기 교체 시 초기화된다.
- R4.5 EditMode 테스트가 98건에서 늘어난다(순수 로직으로 분리 가능한 것부터: 포인터 ID 처리, 안전 영역 앵커 계산, 발사 로직 경계, 소환 예산).

### R5 저장소·공개 준비 (공개 푸시 전)
- R5.1 공개본 사본에서 실명(`(author)`)·로컬 경로·이메일 오타 변형이 0건이다. (키스토어 경로는 D5 보류)
- R5.2 `publish-clean-repo.sh --push`가 `--all`을 쓰지 않고 `AGENTS.md` 절차(공개 `main` 분기)와 일치하며, 푸시 전 로컬·origin 동기화를 점검한다.
- R5.3 `.gitignore`/`.gitattributes`가 zip·백업·`.aiassistant/`·`_Recovery`·Unity YAML 병합을 다룬다.
- R5.4 `docs/store/asset-license-audit.md`가 현재 상태(GUI PRO는 PC 브랜치에만 있음, Toon Shooter 라이선스 파일 없음, 출처 증빙 상태)와 일치한다. `README`/`LICENSE`가 있다.

## 4. 영향 파일 지도
| 영역 | 파일 |
|---|---|
| 좀비·사망 | `Zombie.cs`, `LivingEntity.cs`, `ZombieData.cs` |
| 판 상태 | `GameManager.cs`, `PracticeBossMode.cs`, `UIManager.cs` |
| 소환 | `ZombieSpawner.cs`, `ItemSpawner.cs`, `BossHeadBar.cs` |
| 전투 | `Gun.cs`, `PlayerShooter.cs`, `PlayerInput.cs`, `PlayerMovement.cs`, `RedZone.cs`, `BossRedZoneAttack.cs` |
| 아이템 | `WeaponPickup.cs`, `ItemMagnet.cs`, `MagnetBuff.cs`(2026-10-13 수정됨) |
| 모바일 | `MobileUI/*`, `MobileCore/*`(`MagnetPullRules` 삭제됨), `SettingsPanel.cs` |
| 렌더 보조 | `MinimapTerrainView.cs`, `WeaponIconRig.cs`, `ThreatMinimap.cs` |
| 저장소 | `tools/publish-clean-repo.sh`, `.gitignore`, `.gitattributes`, `docs/store/*` |

## 5. 위험과 제약
- 씬이 바이너리라 diff가 안 된다. 씬을 건드린 뒤에는 `git diff --stat`로 범위를 확인하고, Unity CLI/MCP로 조작한다.
- `Zombie.cs`를 건드리는 작업(R1.1, R1.3, R2.3)은 순차로 한다.
- 소환 분산(R1.6)은 웨이브가 여러 프레임에 걸쳐 나오게 하므로 웨이브 체감 타이밍이 미세하게 달라질 수 있다. 에디터 플레이로 확인한다.
- 백그라운드 일시정지(R1.4), 안전 영역(R4.1), 120 Hz 프레임 제한은 기기 검증이 필요하다. 기기 확인 전에는 "미검증"으로 기록한다.
- 규칙이 바뀐 항목(D1·D2·D4)은 완료 뒤 `AGENTS.md`와 기획서 갱신을 사용자에게 안내한다. 기획서 보급 표는 v1.22에서 D1에 맞춰 고쳤고, D2·D4는 아직 반영되지 않았다.
- 빌드·exe 실행·화면 캡처·스토어 업로드는 사용자가 요청할 때만 한다. 1.0.4 빌드는 만들지 않고 다음 업데이트에 묶는다.

## 6. 완료 기준
- EditMode 전체 통과(98건 기준에서 증가, 자석 상자 규칙 테스트 2건은 D1 변경으로 삭제됨).
- 에디터 플레이 검증 결과(재현 절차·결과)를 `docs/verification/`에 날짜별로 기록하고 확인하지 못한 항목은 "미검증"으로 남긴다.
- 변경은 `SideProject-Mobile`에서 먼저 하고 검증 후 `SideProject`로 옮긴다(`SideProject`는 origin보다 2커밋 뒤처져 있으므로 먼저 동기화).
- 작업 로그를 `D:\Codex\Log\YYYYMMDD_(작업이름)_Log.md`에 남긴다.
