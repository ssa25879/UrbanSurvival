# 저장소에 포함하지 않는 외부 에셋

- 작성일: 2026-10-06 (사용자 지시: GUI PRO Kit를 저장소에서 제거)
- **갱신(2026-10-06 저녁, 사용자 결정): 비공개 저장소 `URP_ZombieGame`(origin)에는 포함, 공개 저장소 `UrbanSurvival`에서만 제외한다.** 원본 저장소는 비공개로 전환됐고, `SideProject-Mobile`도 `e038672`에서 GUI PRO를 다시 추적한다(`.gitignore` 규칙 삭제). 공개본은 `tools/publish-clean-repo.sh`가 모든 커밋에서 GUI PRO를 지우고, `e038672` 이후 커밋의 공개본 `.gitignore`에는 제외 규칙을 되살린다(이미 공개된 커밋의 SHA는 그대로).
- **갱신(2026-10-07): 앱 빌드에서도 GUI PRO를 뺐다(`3ddca6f`).** 화면에 쓰던 흰색 도형 3개는 자체 제작(`Assets/Game/UI/Shapes/`), 일시정지 아이콘 4개는 Kenney Game Icons(CC0, `Assets/Game/UI/Icons/`)로 바꾸고 일시정지 메뉴는 GUI PRO 프리팹 연결을 풀었다. 이어서 `48906b8`로 GUI PRO 폴더도 삭제했다(사용자 지시, 삭제 후 화면 비교 통과). 옛 커밋에는 남아 있으므로 공개본 스크립트는 계속 기록에서 지운다. 아래 "새로 받았을 때" 절차는 더 이상 필요 없다.
- 아래 본문은 "제거" 당시 기록이다. "이 저장소"는 이제 **공개본**을 뜻한다.

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
3. 비공개 origin을 clone하면 GUI PRO가 함께 받아지므로 이 단계가 필요 없다. 공개본(`UrbanSurvival`)을 clone했을 때만 1번이 필요하다.

### 완전히 없애려면(선택)

- **방법 1 — 대체:** 위 6개를 CC0 에셋(저장소에 이미 있는 Kenney UI Pack 등)으로 교체하고 일시정지 프리팹을 다시 만든다. 5개 씬(바이너리)을 다시 연결해야 해서 작업량이 있다. 이렇게 하면 앱에 들어가는 GUI PRO 자원도 없어진다.
- **방법 2 — 저장소 비공개 전환:** GitHub 저장소를 비공개로 바꾸면 새 공개는 막힌다(이미 복제된 사본은 회수할 수 없다).
- **방법 3 — 기록 삭제:** 지난 커밋과 다른 브랜치에도 파일이 남아 있다. `git filter-repo` 등으로 기록에서 지우고 모든 브랜치를 강제 푸시해야 한다. 되돌릴 수 없고 공유 저장소의 기록을 바꾸므로 **사용자 승인 후**에만 진행한다.

## Toon Shooter Game Kit - Dec 2022

- 위치: `Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/`
- 사용자 진술(2026-10-06): CC0라고 한다. **라이선스 원문은 확인하지 못했다**(저장소에 라이선스 파일 없음).
- 권고: 구한 페이지의 라이선스 표기를 확인한 뒤 같은 폴더에 `License.txt`로 근거(페이지 주소, 확인일)를 남긴다. 근거를 남기기 전까지 `asset-license-audit.md`에는 "사용자 진술, 미확인"으로 표기한다.
