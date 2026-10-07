# Urban Survival 에셋 라이선스 점검 (Play 스토어 출시 전)

- 작성일: 2026-10-06
- 목적: 상업 배포(Google Play)에 쓰는 외부 에셋의 출처와 사용 조건을 확인한다.
- 방법: 저장소의 라이선스 파일과 `SOURCE.md`, `AGENTS.md` 기록을 읽었다. **인터넷에서 원문을 다시 확인하지 않았다.** 아래 "확인됨"은 저장소 안 파일로 확인한 것이고, "미확인"은 저장소에 근거가 없다는 뜻이다(문제가 있다는 뜻이 아니다).
- 이 문서는 법률 자문이 아니다. 출시 전에 사용자가 각 항목의 원문 라이선스를 확인해야 한다.

## 1. 요약

| 구분 | 상태 |
|---|---|
| CC0로 확인된 에셋 | Quaternius 좀비 키트, Kenney UI 팩 2종, Kenney 폰트, 남성 피격·사망 음성, 무기 발사음 4종(The Free Firearm Sound Library) |
| 출처 확인(2026-10-06), 저자 허락(CC0 아님) | 효과음 7개(`Assets/Audios`)와 초기 모델 7개: 이제민 『레트로의 유니티 게임 프로그래밍 에센스』 예제 저장소와 동일 파일(4장 참고). `Gun Shoot.wav`는 상위 출처 주의 |
| 사용자 확인 필요(출처·라이선스 파일 없음) | `Assets/Animations`·`Materials` 등 위 대조에 들지 않은 초기 프로젝트 자원 |
| ~~사용자 진술(원문 미확인)~~ | Toon Shooter Game Kit: CC0 — 2026-10-07 Quaternius 페이지에서 확인(6장) |
| 저장소에서 제거 | GUI PRO Kit(2026-10-06, 앱에는 6개 자원 포함, `third-party-not-in-repo.md`) |
| 사용자 책임 | AI로 만든 앱 아이콘·피처 그래픽의 생성 도구 약관(상업 이용 가능 여부) |

**출시 전 반드시 정리할 것:** 출처를 모르는 효과음·배경음악·초기 프로젝트 자원. 출처를 증명할 수 없으면 CC0 에셋으로 교체하거나 제거한다.

## 2. 에셋별 점검표

| 에셋 | 위치 | 용도 | 라이선스 근거 | 상태 | 조치 |
|---|---|---|---|---|---|
| Zombie Apocalypse Kit (Quaternius) | `Assets/SideProjectAssets/Zombie Apocalypse Kit - March 2024/` | 좀비 모델, 도로 타일, 장애물 | `License.txt`: CC0 1.0 (파일 머리글은 "Ultimate Platformer Pack"으로 적혀 있으나 CC0 본문은 동일) | 확인됨 | 크레딧은 선택. 가능하면 게임 정보에 "Quaternius" 표기 |
| Kenney UI Pack / UI Pack Sci-Fi | `Assets/SideProjectAssets/kenney_ui-pack*/` | HUD UI 스프라이트 | 각 `License.txt`: CC0 | 확인됨 | 크레딧 선택 |
| Kenney Future Narrow 글꼴 | `Assets/Fonts/`, `GUI PRO Kit/Fonts/` | UI 글꼴 | `Assets/Fonts/License.txt`: CC0 | 확인됨 | 크레딧 선택 |
| 남성 피격·사망 음성 | `Assets/Game/Audio/Voice/Male/` | 플레이어 피격·사망 | `SOURCE.md`: HaelDB, OpenGameArt, CC0 1.0 선택(2026-10-06) | 확인됨(용도 매핑은 청취 미확인) | 사용자가 음색이 맞는지 들어 볼 것 |
| 무기 발사음 4종 | `Assets/Game/Audio/Weapons/` | 권총·소총·SMG·산탄총 발사음 | `SOURCE.md`: The Free Firearm Sound Library(Ben Jaszczak 외), OpenGameArt, CC0 1.0(2026-10-06 페이지 확인) | 확인됨 | 2026-10-07 `4d37f3f`로 적용(`SideProject-Mobile`). 크레딧 불필요 |
| Toon Shooter Game Kit - Dec 2022 | `Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/` | 플레이어 캐릭터, 무기 4종, 환경 일부 | 저장소에 라이선스 파일·출처 기록 없음(`Preview.jpg`만 있음) | **사용자 진술: CC0(원문 미확인)** | 라이선스 페이지 주소·확인일을 같은 폴더에 `License.txt`로 남길 것 |
| GUI PRO Kit - Simple Casual | `Assets/GUI PRO Kit - Simple Casual/` | HUD 패널 프레임, 일시정지 UI | 저장소에 라이선스 파일 없음(README만) | **저장소에서 제거함(2026-10-06)** | 앱에는 6개 자원만 포함(`third-party-not-in-repo.md`). 완전 제거·기록 삭제는 사용자 결정 |
| 효과음 7개 | `Assets/Audios/` (Gun Shoot/Reload, Pick Up, Woman Damage/Die, Zombie Damage/Die) | 총기·픽업·피격 음 | IJEMIN/Unity-Programming-Essence `14~19/Zombie/Assets/Audios/`와 Git blob 해시 일치(7/7, 2026-10-06). 저장소 README: 유니티짱 제외 상업·비상업 제약 없이 사용, 크레딧 불필요, 무수정 예제 프로젝트 상업 재배포 금지 | **확인됨(저자 허락)**. `Gun Shoot.wav`는 상위 출처 주의 | `Gun Shoot.wav` 교체 검토(4장) |
| 배경음악 `Searching.ogg` | `Assets/Game/Audio/Music/` | 인트로 BGM | 사용자 확인: OpenGameArt https://opengameart.org/content/searching, CC0(2026-10-06) | 확인됨(사용자) | 작성자 이름을 페이지에서 확인해 `Assets/Game/Audio/Music/SOURCE.md`에 추가 |
| 배경음악 `singularity_calm.wav` | `Assets/Game/Audio/Music/` | 인게임 BGM | `AGENTS.md`: 사용자 제공, OpenGameArt CC0로 전달받음(원문 미확인) | 부분 확인 | OpenGameArt 페이지에서 곡·라이선스 원문 확인 |
| 초기 프로젝트 모델 7개 | `Assets/Models/` (Woman, Zombie, Uzi, Heart, Coin, Ammo, ShellCasing) | 비교용 옛 캐릭터·소품 | 효과음과 같은 예제 저장소 파일과 해시 일치(7/7) | **확인됨(저자 허락)** | — |
| 초기 프로젝트 기타 자원 | `Assets/Models/Level Art`, `Assets/Animations`, `Assets/Materials`, `Assets/Prefabs` 일부 | 일부 씬 소품(예: `lanternDouble`의 재질) | 해시 대조 안 함(같은 예제 저장소 출신으로 추정) | **미확인** | 실제로 빌드에 쓰이는 것만 추려 같은 방법으로 대조 |
| 앱 아이콘·피처 그래픽 | `Assets/Images/AppIcon-1.png`, `AppIcon-2.png` | 스토어 이미지, 런처 아이콘 | 사용자가 생성해서 제공 | **사용자 확인** | 생성 도구의 약관이 상업적 이용·스토어 등록을 허용하는지 확인. 기존 게임의 캐릭터·로고와 닮지 않았는지 확인 |
| Unity 패키지(URP, Cinemachine, Input System 등) | `Packages/manifest.json` | 엔진 기능 | Unity 패키지 라이선스 | 확인 불필요 | — |
| TextMesh Pro 기본 리소스 | `Assets/TextMesh Pro/` | 기본 폰트·셰이더 | Unity 제공 | 확인 불필요 | — |

## 3. 알려진 문제와 권고

1. **효과음 7개(`Assets/Audios`):** 2026-10-06에 출처를 확인했다(4장, 예제 저자의 사용 허락). 남은 위험이던 `Gun Shoot.wav`(상위 출처 SoundBible "Smack", Personal Use Only로 표시)는 2026-10-07 무기별 CC0 발사음으로 교체했다(`4d37f3f`, 게임에서 더는 참조하지 않음, 파일은 남김). (배경음악 `Searching.ogg`는 2026-10-06 사용자가 OpenGameArt CC0로 확인함. `singularity_calm.wav`는 CC0로 전달받았으나 페이지 주소가 아직 없다.)
2. **두 에셋(Toon Shooter, GUI PRO)과 공개 저장소 — 확인된 사실:** 2026-10-06에 로그인 없이 GitHub API(`api.github.com/repos/ssa25879/URP_ZombieGame`)로 조회했을 때 HTTP 200, `"private": false`였다. 즉 **저장소가 공개 상태**다. 일반적으로 Asset Store 에셋은 컴파일된 게임에 포함해 배포할 수 있지만 원본 파일을 그대로 재배포(공개 저장소에 원본 업로드 포함)하는 것은 허용되지 않는 경우가 많다. 이 두 에셋의 라이선스를 확인하기 전까지는 **저장소를 비공개로 전환하거나 해당 에셋을 저장소에서 제외하는 것**을 검토해야 한다. 저장소 공개 설정 변경은 사용자가 직접 한다(GitHub 저장소 Settings > Danger Zone > Change visibility). 이미 올라간 커밋 기록에 파일이 남아 있어 비공개 전환 외에 기록 정리는 별도 작업이다.
3. **크레딧 화면:** CC0는 의무가 아니지만 Quaternius와 Kenney를 게임 안(설정 또는 정보 화면)이나 스토어 설명에 표기하는 것이 관례다. 현재 게임에는 크레딧 화면이 없다(필요하면 별도 작업으로 추가).

## 4. 초기 프로젝트 자원 출처 조사 (2026-10-06)

- 방법: GitHub API로 https://github.com/IJEMIN/Unity-Programming-Essence 의 전체 파일 트리를 받아, 로컬 파일의 `git hash-object` 값과 비교했다. 효과음 7개와 `Assets/Models`의 모델 7개가 모두 같은 해시였다(바이트 단위 동일). 이 저장소는 이제민 저 『레트로의 유니티 게임 프로그래밍 에센스』(한빛미디어) 예제 모음이고, 해당 파일은 "좀비 서바이버" 예제(`14`~`19`장)에 들어 있다.
- 라이선스(저장소 README "라이선스와 크레딧" 요약): 유니티짱 에셋을 제외한 모든 프로젝트·에셋·코드는 자기 프로젝트의 템플릿이나 추가 에셋으로 상업·비상업 구분 없이 제약 없이 쓸 수 있고 크레딧은 필요 없다. 수정하지 않은 예제 프로젝트를 그대로 상업적으로 재배포(판매)하는 것만 금지다. **CC0 선언은 아니다.** 저자가 직접 밝힌 사용 허락이다. 이 게임은 예제를 그대로 파는 것이 아니므로 허용 범위로 판단한다(법률 자문 아님).
- **주의: `Gun Shoot.wav`.** 파일 안의 메타데이터(INFO 청크)에 제목 "Smack sound", 작성자 "SoundBible.com"이 적혀 있다. SoundBible의 "Smack" 페이지(https://soundbible.com/441-Smack.html)는 라이선스가 **Personal Use Only**다. 예제 저자가 이 소리를 가공했거나 메타데이터만 남은 것일 수 있고, 두 소리가 실제로 같은지는 청취로 비교하지 않았다. → **2026-10-07 교체 완료**: 무기 4종의 발사음을 CC0 음원(`Assets/Game/Audio/Weapons/`)으로 바꿨다(`4d37f3f`, `SideProject-Mobile`). `SideProject`(PC)는 아직 이 파일을 쓴다.
- `Gun Reload.wav` 메타데이터에는 편집 프로그램(AVS4YOU) 표시만 있다. `Woman Damage/Die.ogg`에는 "Pro Tools"로 인코딩했다는 표시만 있다. 출처 정보는 없다.

## 5. 사용자 확인 질문

- Toon Shooter Game Kit와 GUI PRO Kit는 어디서(어떤 계정으로) 구했나? 라이선스 문서는?
- ~~`Gun Shoot.wav`를 출시 전에 교체할지~~ 2026-10-07 교체 완료(모바일 브랜치)
- ~~GitHub 저장소 `ssa25879/URP_ZombieGame`을 비공개로 바꿀 수 있나?~~ 2026-10-06 사용자가 비공개로 전환함(비로그인 API 조회 404로 확인)

## 6. 빌드 포함 기준 재점검 (2026-10-07)

- 방법: 에디터에서 빌드 목록 씬 5개(`Intro`, `UrbanSurvival`, `Main`, `BossTestScene`, `FinalBossTestScene`)와 `Resources` 폴더 9개 파일의 의존성을 `AssetDatabase.GetDependencies`로 모았다(343개). 스크립트가 이름으로 불러오는 에셋은 이 방법으로 잡히지 않을 수 있다. `StreamingAssets`·`Plugins` 폴더는 없다.
- 출처 대조: 초기 프로젝트 자원은 IJEMIN 예제 저장소의 Git blob 해시·경로와 비교했다.

| 구분 | 빌드에 들어가는 것 | 근거(2026-10-07 확인) | 판정 |
|---|---|---|---|
| Toon Shooter Game Kit | 캐릭터·무기·환경 36개 | Quaternius 팩 페이지 https://quaternius.com/packs/toonshootergamekit.html : "CC0", 개인·상업 무료 | **확인됨(CC0)**. 2장의 "사용자 진술, 미확인"을 대체 |
| Zombie Apocalypse Kit | 좀비·도로·장애물 33개 | 팩 안 `License.txt` CC0 | 확인됨 |
| Kenney UI 2종·폰트 | 스프라이트 6개, Future Narrow | 팩 안 License CC0 | 확인됨 |
| GUI PRO Kit - Simple Casual | ~~스프라이트 13개, 일시정지 프리팹 1개, Quicksand SDF 폰트 2개~~ → **2026-10-07 `3ddca6f`로 빌드에서 제거(0개), `48906b8`로 폴더 삭제** | Asset Store 페이지: Standard Unity Asset Store EULA(Single/Multi Entity) | **조건부.** EULA는 게임에 넣어 배포하는 것을 허용하지만, **사용자 계정으로 정식 구입(라이선스 보유)한 경우에만** 해당한다. 구입 내역 확인 필요. 원본 파일 재배포는 금지(공개 저장소에서는 제외 중) |
| 초기 예제 자원(IJEMIN) | `Main.unity`와 그 의존 자원: 모델 7, `Level Art` 18(묘지 소품), 텍스처 8, 재질 21, 애니메이션 8, 효과음 6, 프리팹 | 38개는 예제 파일과 해시 동일, 재질·메시 등 나머지는 같은 경로의 파일을 이 프로젝트가 고친 것. `ShooterAnimator.controller`만 예제에 없음(프로젝트에서 만든 것으로 추정) | 저자 허락 범위(1·4장). 단 아래 "주의 1" |
| 배경음악 | `singularity_calm.wav`, `Searching.ogg` | OpenGameArt "Singularity"(Vitalezzz) CC0, 파일 크기 53,944,406 바이트로 페이지의 53.9 MB와 일치 / "Searching"(yd) CC0, 2,113,188 바이트(2.1 MB) | **둘 다 확인됨(CC0)** |
| 발사음·남성 음성 | `Assets/Game/Audio/Weapons/`, `Voice/Male/` | 각 `SOURCE.md` CC0 | 확인됨 |
| TextMesh Pro 기본 리소스 | `LiberationSans`(OFL), **`EmojiOne.png`·`EmojiOne.asset`** | `Resources` 폴더라 사용 여부와 관계없이 빌드에 들어간다. EmojiOne 2.x 그림은 CC BY 4.0으로 알려져 있어 **상업 배포 시 저작자 표시가 필요**하다(TMP 동봉 `EmojiOne Attribution.txt`는 라이선스를 직접 확인하라고만 적음) | **주의 2** |

### 주의할 점(우선순위순)

1. ~~**GUI PRO Kit 구입 여부**~~ → 2026-10-07 해결: 빌드에서 GUI PRO를 모두 뺐다(`3ddca6f`). 패널·선·원은 자체 제작 도형(`Assets/Game/UI/Shapes/`), 일시정지 아이콘은 Kenney Game Icons(CC0, `Assets/Game/UI/Icons/`)로 바꿨고 화면 모양은 그대로다(교체 전후 캡처 픽셀 차이 0.05% 이하). 아래는 당시 기록. 1. **GUI PRO Kit 구입 여부(가장 중요).** 유료 Asset Store 에셋이다. 사용자 계정의 구매 내역(My Assets)에 있어야 앱에 넣을 수 있다. 무료 배포 사이트 등에서 받은 것이면 출시 전에 Kenney(CC0) 자원으로 바꿔야 한다(`third-party-not-in-repo.md` "방법 1").
2. **TMP EmojiOne(저작자 표시 필요).** 게임은 이모지를 쓰지 않는다. 출시 전에 둘 중 하나를 한다. (a) `TMP Settings`의 기본 스프라이트 에셋을 비우고 `EmojiOne` 파일을 빌드에서 빼거나 (b) 앱 안이나 스토어 설명에 EmojiOne(CC BY 4.0) 표기를 넣는다. (a)는 Unity 작업이다.
3. **`Main.unity`가 빌드 목록에 있음.** 어떤 코드도 이 씬을 불러오지 않는다(`IntroMenu`, `PracticeSelectMenu`, `UIManager`의 `LoadScene` 대상이 아님). 그런데 빌드에는 예제 레벨(묘지 소품, 여성 캐릭터, 파티클 등)이 그대로 실린다. 예제 저자의 허락 범위 안이지만 "무수정 예제의 상업 재배포 금지" 조항과 굳이 얽힐 이유가 없다. 용량도 커진다. 출시 빌드에서는 빌드 목록에서 빼는 것을 권한다(Unity 작업, 빼기 전에 다른 씬이 이 씬의 자원에 기대지 않는지 확인).
4. **앱 아이콘·피처 그래픽(AI 생성).** 생성 도구의 상업 이용 약관과, 기존 게임 캐릭터·로고와 닮지 않았는지 사용자가 확인한다(기존 항목).
5. **Unity Analytics가 아직 켜져 있음**(`m_Enabled: 1`, `com.unity.analytics` 3.8.2). 끄기로 결정했지만 아직 적용 전이다. 켠 채로 내면 Data Safety에 수집 항목을 신고해야 한다.
6. **앱 이름 "Urban Survival".** 웹 검색으로는 같은 이름의 Play 앱을 찾지 못했다(비슷한 이름 "Urban Legends - Survival" 등은 있음). 검색이 완전하지 않으니 Play Console 등록 전에 스토어에서 직접 검색하고, 필요하면 상표 검색(KIPRIS 등)을 한다.
7. 참고(문제 아님): 무기 파일 이름에 실제 총기 이름(AK-47, 1911 등)이 들어 있지만 화면 표기는 PISTOL/AR/SMG/SG라 노출되지 않는다. 피·좀비 폭력 표현이 있으므로 Play 콘텐츠 등급 설문(IARC)에 그대로 답하고, 아동 대상(Families) 앱으로 등록하지 않는다. Unity Personal 스플래시 화면은 켜져 있다(라이선스 조건에 맞음).

## 7. 2026-10-07 정리 결과와 남은 항목

### 처리 완료
| 항목 | 커밋 | 내용 |
|---|---|---|
| 무기 발사음 | `4d37f3f` | The Free Firearm Sound Library(CC0)로 교체, `Gun Shoot.wav` 미사용 |
| 효과음(좀비 피격·사망, 재장전, 습득 등) | `7b6fe99` | CC0 음원으로 교체(`Assets/Game/Audio/SFX/SOURCE.md`) |
| 비교 기준 씬 `Main.unity` | `df2d3f4` | 빌드 목록에서 제외(파일은 유지) |
| Unity Analytics | `df2d3f4` | 끔. `com.unity.analytics` 패키지는 남아 있어 `INTERNET` 제거 여부는 릴리스 빌드로 확인 예정 |
| 예전 플레이어 오브젝트 | `25fcd4f` | 플레이어 프리팹화, 꺼진 예전 Player Character 삭제 |
| GUI PRO Kit | `af4e06e`, `3ddca6f`, `48906b8` | 아이콘은 Kenney Game Icons(CC0), 도형은 자체 제작으로 교체 후 폴더 삭제. 빌드 의존성 0 |
| Toon Shooter Game Kit | — | Quaternius 공식 페이지에서 CC0 확인(6장) |

### 출처 추가 확인(세션 1 점검, `WorkNotes/20261007_빌드에셋_출처점검.md`)
- `Models/Level Art` 16개: FBX 안 경로로 Kenney Graveyard Kit(CC0) 확인. `Uzi`·`Ammo`: Kenney Weapon Pack(CC0) 경로. `Coin`: Quaternius PowerUps 경로(CC0로 알려짐, 팩 페이지는 404로 직접 확인 못 함).
- `ShellCasing.FBX`와 파티클 텍스처 7개: Unity "Particle Pack"(Standard Unity Asset Store EULA) 계열로 추정. 앱 빌드에 넣는 것은 EULA상 허용. **공개 저장소 `UrbanSurvival`에 두는 것은 사용자 결정으로 그대로 둔다(2026-10-07).**
- `Kenney Future Narrow.ttf`: 동봉 `License.txt`는 CC0이지만 TTF 내부 이름 표에 FontStruct 원작 표기(CC BY-SA 3.0)가 남아 있다. Kenney는 자사 폰트를 CC0로 배포하므로 위험은 낮다고 보지만, 크레딧에 "Kenney" 표기를 넣으면 더 안전하다.

### 빌드에 아직 남은 미확정·정리 대상(2026-10-07 11시 기준 의존성 조회)
| 자원 | 상태 | 제안 |
|---|---|---|
| `Woman.fbx`, `WomanSkin.png`, `Woman Damage/Die.ogg` | 빌드에 아직 포함(프리팹 기본값 등에서 참조). 출처는 IJEMIN 예제(Quaternius Animated Women 추정) | 세션 1이 참조를 정리해 빌드에서 뺀다(진행 예정) |
| `Pick Up.ogg` | 빌드에 아직 포함 | 교체 음원으로 연결이 다 옮겨졌는지 확인 |
| 휴머노이드 애니메이션 5종, `ShooterAnimator.controller` | 출처 미확정(IJEMIN 포괄 허용만 근거) | 실제 사용처 확인 후 필요 없으면 제거 |
| `Heart.obj`(회복 상자) | 출처 미확정(Quaternius PowerUps 추정) | 유지 또는 CC0 모델로 교체 |
| `Sprites/frame.png`, `Health Circle.png` | 출처 미확정(단순 도형) | 자체 제작 도형으로 교체 쉬움 |
| TMP `EmojiOne` | CC BY 4.0, 저작자 표시 필요 | 빌드에서 빼거나 크레딧 표기(사용자 결정 대기) |
