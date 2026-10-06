# 저장소에 포함하지 않는 외부 에셋

- 작성일: 2026-10-06 (사용자 지시: GUI PRO Kit를 저장소에서 제거)
- 이 브랜치(`SideProject-Mobile`)에서 아래 에셋은 Git 추적에서 제외되어 있다. **로컬 작업 폴더에는 그대로 있고** `.gitignore`가 막고 있어 실수로 다시 올라가지 않는다.

## GUI PRO Kit - Simple Casual

- 위치: `Assets/GUI PRO Kit - Simple Casual/` (파일 약 4,960개, 약 151 MB)와 `Assets/GUI PRO Kit - Simple Casual.meta`
- 제거 이유: 저장소가 공개 상태이고 라이선스를 확인하지 못했다(`asset-license-audit.md`). Asset Store 에셋은 보통 컴파일된 앱에는 넣을 수 있어도 원본 파일을 공개 저장소에 재배포할 수는 없다.
- **프로젝트가 실제로 쓰는 것(Unity 의존성 조회, 2026-10-06):** 키트 전체 중 아래 6개뿐이다.
  - 스프라이트 `BasicFrame_Rectangle02_s_White.png`(HUD·메뉴 패널 프레임, 9-슬라이스), `LineFrame_White.png`(앰버 상단 라인), `BasicFrame_Circle_337_White.png`, `Icon_WhiteIcon_Home.png`, `Icon_WhiteIcon_Setting_s.png`
  - 프리팹 `Play_Pause_common (1).prefab`(일시정지 메뉴, 씬 오버라이드로 조정됨)
- 이 에셋을 참조하는 프로젝트 자산: 씬 `Intro`, `UrbanSurvival`, `UrbanSurvival_ResultTest`, `BossTestScene`, `FinalBossTestScene`, 그리고 `Assets/Game/UI/UrbanSurvivalSettingsTheme.asset`(설정 창 테마). `Assets/Scenes/Main.unity`는 참조하지 않는다.
- 모바일 UI(`MobileUIFactory`)는 씬의 `WeaponHUD`가 쓰는 패널 스프라이트를 런타임에 가져다 쓰므로 간접적으로 같은 스프라이트를 쓴다.

### 이 저장소를 새로 받았을 때(다른 PC 등)

1. Asset Store에서 **GUI PRO Kit - Simple Casual**을 임포트해 `Assets/GUI PRO Kit - Simple Casual/`에 둔다(구매한 계정 필요). 같은 패키지를 임포트하면 원래 `.meta`의 GUID가 복원되어 씬·프리팹 참조가 이어진다.
2. 임포트하지 않으면 위 6개 에셋을 참조하는 UI가 비어 보이고(프레임 없음, 일시정지 메뉴 없음) 콘솔에 "missing" 경고가 난다.
3. **다른 브랜치(`SideProject`, `main`)에는 이 에셋이 아직 추적되어 있다.** 이 브랜치를 그쪽에 병합하면 병합이 해당 파일을 삭제하므로, 병합 전에 아래 "완전 제거 방법"을 먼저 결정한다.

### 완전히 없애려면(선택)

- **방법 1 — 대체:** 위 6개를 CC0 에셋(저장소에 이미 있는 Kenney UI Pack 등)으로 교체하고 일시정지 프리팹을 다시 만든다. 5개 씬(바이너리)을 다시 연결해야 해서 작업량이 있다. 이렇게 하면 앱에 들어가는 GUI PRO 자원도 없어진다.
- **방법 2 — 저장소 비공개 전환:** GitHub 저장소를 비공개로 바꾸면 새 공개는 막힌다(이미 복제된 사본은 회수할 수 없다).
- **방법 3 — 기록 삭제:** 지난 커밋과 다른 브랜치에도 파일이 남아 있다. `git filter-repo` 등으로 기록에서 지우고 모든 브랜치를 강제 푸시해야 한다. 되돌릴 수 없고 공유 저장소의 기록을 바꾸므로 **사용자 승인 후**에만 진행한다.

## Toon Shooter Game Kit - Dec 2022

- 위치: `Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/`
- 사용자 진술(2026-10-06): CC0라고 한다. **라이선스 원문은 확인하지 못했다**(저장소에 라이선스 파일 없음).
- 권고: 구한 페이지의 라이선스 표기를 확인한 뒤 같은 폴더에 `License.txt`로 근거(페이지 주소, 확인일)를 남긴다. 근거를 남기기 전까지 `asset-license-audit.md`에는 "사용자 진술, 미확인"으로 표기한다.
