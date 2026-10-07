# Urban Survival 인수인계 — 아이템 자석 연출

기준일: 2026-10-07. 이번 세션(macOS PC)에서 실제 수행한 변경과 검증을 기록한다. 검증하지 못한 항목은 "미검증"으로 적었다.

## 완료한 작업

- 인수인계 zip(`인수인계.zip`)을 읽고 `SideProject`를 origin 최신(`17f8cd2`)으로 fast-forward 했다.
- 로컬 `AGENTS.md`(Git 제외)를 zip의 최신본으로 교체하고, "진행 예정" 표기 3곳(Analytics, 습득 범위 3 m, 발사음)을 커밋 번호로 고쳤다. 교체 때 만든 백업 파일(`AGENTS_20261007_1312_백업.md`)은 같은 분에 만든 두 번째 백업이 덮어써서 **9/30 원본은 남아 있지 않다**(현재 그 파일 내용은 zip 최신본과 같다). 이 파일들은 미추적이며 커밋하지 않는다.
- **아이템 자석 연출 구현**(사용자 설계 결정 반영)
  - 습득 범위(월드 반경 3 m)에 들어온 아이템이 플레이어 쪽으로 날아가고, **도착하면 효과 적용과 습득음 재생**.
  - 비행 시간 **0.3초**(사용자 확정, 문서에는 "초기 제안 0.2~0.3초"만 있었음). 인스펙터 필드 `ItemMagnet.flightDuration`으로 조정한다.
  - 커밋: `SideProject-Mobile` `23fb6ea`, `SideProject` `e0a2b62`(cherry-pick, 충돌 없음). 둘 다 비공개 `origin`에 푸시했다.

## 구현 요약

| 파일 | 내용 |
|---|---|
| `Assets/Scripts/ItemMagnet.cs` (신규) | `Begin(PlayerHealth)`로 비행 시작. 시작 위치에서 현재 플레이어 가슴 높이(`targetHeight` 0.8 m)로 ease-in 보간(목표는 매 프레임 플레이어 위치). 도착 시 `PlayerHealth.CollectItem` 호출 |
| `Assets/Scripts/PlayerHealth.cs` | `OnTriggerEnter`에서 아이템에 `ItemMagnet`이 있으면 `Begin`, 없으면 기존처럼 즉시 습득. 효과 적용·습득음은 공개 메서드 `CollectItem`으로 분리(사망 시 무시) |
| 프리팹 5종 | `AmmoPack`, `HealthPack`, `RiflePickup`, `SMGPickup`, `ShotgunPickup`에 `ItemMagnet` 추가(프리팹 YAML 직접 편집, Unity에서 로드 확인) |

동작 규칙(사용자 결정)
- 중복 습득 방지: 비행 시작 시 아이템의 트리거 콜라이더를 끈다.
- 무기 픽업의 바닥 글로우 `GlowPad`는 비행 시작 시 숨긴다(자식 이름으로 찾음).
- 일시정지: `Time.deltaTime` 기준이라 `timeScale` 0에서 비행도 멈춘다.
- 비행 중 플레이어가 사망하면 비행을 멈추고 아이템은 그 자리에 남으며 효과는 적용되지 않는다.
- `Coin`은 `ItemMagnet`이 없어 기존처럼 즉시 습득한다.
- 비행 중 드랍 디스폰 타이머(`ZombieSpawner.lootDespawnTime`)가 끝나면 아이템이 사라지고 효과는 적용되지 않는다(드문 경우, 방어 코드 없음).

## 검증 결과

Unity 6000.3.19f1 에디터(macOS)에서 플레이 모드로 확인했다. `timeScale`을 0.05~0.1로 낮춰 비행을 느리게 만든 상태에서 위치를 샘플링했다.

| 항목 | `SideProject-Mobile` | `SideProject` |
|---|---|---|
| 컴파일 오류 | 0건 | 0건 |
| 프리팹 5종에 `ItemMagnet` 연결 | 확인 | 확인 |
| 비행 중 거리 감소, 트리거 콜라이더 꺼짐 | 확인 | 확인 |
| 도착 후 효과(체력 50→80, 소총 슬롯 해금) | 확인 | 소총 슬롯 해금 확인 |
| 무기 픽업 `GlowPad` 숨김 | 확인 | 확인 |
| 일시정지 중 비행 정지, 재개 후 도착 | 확인 | 확인 |
| 비행 중 사망(제자리 유지, 오류 0건) | 확인 | **미검증**(같은 코드로 가정) |
| EditMode 테스트 | 88/88 통과 | **미검증** |

## 미검증 / 남은 확인

- 실제 속도(0.3초)에서 연출이 어떻게 보이는지 눈으로 확인하지 못했다(수치 샘플링만 함).
- 습득음 재생을 청취로 확인하지 못했다.
- SMG·산탄총 픽업과 AmmoPack의 개별 비행은 따로 검증하지 않았다(프리팹 구조 동일, 컴포넌트 연결만 확인).
- 안드로이드 빌드와 S26 Ultra 실기에서는 확인하지 못했다.
- 좀비 500마리 부하 상태에서 연출이 어떤지 확인하지 못했다(비행 중인 아이템만 `Update`를 돌므로 부하는 작을 것으로 추정).
- 다음 후보: 비행 시간·도착 높이 조정(프리팹 인스펙터), 디스폰 타이머 충돌 방어, 기획서·`AGENTS.md`에 자석 연출 항목 추가(`AGENTS.md`에는 아직 없음).

## 이번 세션에서 확인한 사실

- `SideProject`에는 이미 반영돼 있었다: 총성 4종(`1865201`), 습득 범위 3 m(`83bdbe9`), Analytics 끄기(`c1f5dfb`, `UnityConnectSettings`의 `m_Enabled: 0` 확인), 기타 효과음 CC0 교체(`d3808dc`), 플레이어 프리팹화(`ef97c7b`), 기본 아이콘(`17f8cd2`).
- Analytics를 껐어도 릴리스 빌드 매니페스트에서 `INTERNET` 권한이 빠졌는지는 **미검증**이다(재빌드가 필요하며 사용자가 요청할 때만 빌드한다).
- `.gitignore`의 로컬 수정(`/*.docx` 3줄)은 사용자 것이며 커밋하지 않았다. 브랜치 전환 때 막히므로 `git stash push .gitignore` 후 전환, 이후 `git stash pop`으로 복원했다.

## 재개 시 참고

- 프로젝트 경로는 PC마다 다르다. 이 PC는 `/Users/mr09/Desktop/URP_ZombieGame`이며 메인 폴더 브랜치는 작업에 따라 바뀐다(현재 `SideProject`). 작업 전 브랜치를 확인한다.
- Unity CLI(`unity`)로 열린 에디터를 조작했다: `unity open .`, `unity status`, `unity command eval --code '...'`, `unity command run_tests --mode EditMode`, `editor_play` / `editor_stop`. 에디터가 막 열린 직후에는 `503 Server Busy`가 나오므로 `editor_status`가 `ready`가 될 때까지 재시도한다.
- 에디터 플레이 검증 요령: `Application.runInBackground = true`(런타임에서만, 프로젝트 설정 변경 없음), `ZombieSpawner`를 끄고(`enabled = false`) 검증하지 않으면 좀비가 플레이어를 죽여 `!dead` 검사로 습득 판정이 꺼진다. 비행 시간이 짧으니 `Time.timeScale`을 0.05~0.1로 낮춰 샘플링한다.
- Unity를 열어도 이번에는 자동 재저장 파일이 생기지 않았다. 생기면 커밋하지도 되돌리지도 않는다.
- 공개 저장소 `UrbanSurvival`에는 반영하지 않았다(사용자 요청 시에만).
