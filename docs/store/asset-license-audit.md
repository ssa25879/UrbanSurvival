# Urban Survival 에셋 라이선스 점검 (Play 스토어 출시 전)

- 작성일: 2026-10-06
- 목적: 상업 배포(Google Play)에 쓰는 외부 에셋의 출처와 사용 조건을 확인한다.
- 방법: 저장소의 라이선스 파일과 `SOURCE.md`, `AGENTS.md` 기록을 읽었다. **인터넷에서 원문을 다시 확인하지 않았다.** 아래 "확인됨"은 저장소 안 파일로 확인한 것이고, "미확인"은 저장소에 근거가 없다는 뜻이다(문제가 있다는 뜻이 아니다).
- 이 문서는 법률 자문이 아니다. 출시 전에 사용자가 각 항목의 원문 라이선스를 확인해야 한다.

## 1. 요약

| 구분 | 상태 |
|---|---|
| CC0로 확인된 에셋 | Quaternius 좀비 키트, Kenney UI 팩 2종, Kenney 폰트, 남성 피격·사망 음성 |
| 출처 확인(2026-10-06), 저자 허락(CC0 아님) | 효과음 7개(`Assets/Audios`)와 초기 모델 7개: 이제민 『레트로의 유니티 게임 프로그래밍 에센스』 예제 저장소와 동일 파일(4장 참고). `Gun Shoot.wav`는 상위 출처 주의 |
| 사용자 확인 필요(출처·라이선스 파일 없음) | `Assets/Animations`·`Materials` 등 위 대조에 들지 않은 초기 프로젝트 자원 |
| 사용자 진술(원문 미확인) | Toon Shooter Game Kit: CC0 |
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

1. **효과음 7개(`Assets/Audios`):** 2026-10-06에 출처를 확인했다(4장, 예제 저자의 사용 허락). 남은 위험은 `Gun Shoot.wav`의 상위 출처(SoundBible "Smack", Personal Use Only로 표시) 하나이며, 출시 전 CC0 총성으로 교체를 권한다. (배경음악 `Searching.ogg`는 2026-10-06 사용자가 OpenGameArt CC0로 확인함. `singularity_calm.wav`는 CC0로 전달받았으나 페이지 주소가 아직 없다.)
2. **두 에셋(Toon Shooter, GUI PRO)과 공개 저장소 — 확인된 사실:** 2026-10-06에 로그인 없이 GitHub API(`api.github.com/repos/ssa25879/URP_ZombieGame`)로 조회했을 때 HTTP 200, `"private": false`였다. 즉 **저장소가 공개 상태**다. 일반적으로 Asset Store 에셋은 컴파일된 게임에 포함해 배포할 수 있지만 원본 파일을 그대로 재배포(공개 저장소에 원본 업로드 포함)하는 것은 허용되지 않는 경우가 많다. 이 두 에셋의 라이선스를 확인하기 전까지는 **저장소를 비공개로 전환하거나 해당 에셋을 저장소에서 제외하는 것**을 검토해야 한다. 저장소 공개 설정 변경은 사용자가 직접 한다(GitHub 저장소 Settings > Danger Zone > Change visibility). 이미 올라간 커밋 기록에 파일이 남아 있어 비공개 전환 외에 기록 정리는 별도 작업이다.
3. **크레딧 화면:** CC0는 의무가 아니지만 Quaternius와 Kenney를 게임 안(설정 또는 정보 화면)이나 스토어 설명에 표기하는 것이 관례다. 현재 게임에는 크레딧 화면이 없다(필요하면 별도 작업으로 추가).

## 4. 초기 프로젝트 자원 출처 조사 (2026-10-06)

- 방법: GitHub API로 https://github.com/IJEMIN/Unity-Programming-Essence 의 전체 파일 트리를 받아, 로컬 파일의 `git hash-object` 값과 비교했다. 효과음 7개와 `Assets/Models`의 모델 7개가 모두 같은 해시였다(바이트 단위 동일). 이 저장소는 이제민 저 『레트로의 유니티 게임 프로그래밍 에센스』(한빛미디어) 예제 모음이고, 해당 파일은 "좀비 서바이버" 예제(`14`~`19`장)에 들어 있다.
- 라이선스(저장소 README "라이선스와 크레딧" 요약): 유니티짱 에셋을 제외한 모든 프로젝트·에셋·코드는 자기 프로젝트의 템플릿이나 추가 에셋으로 상업·비상업 구분 없이 제약 없이 쓸 수 있고 크레딧은 필요 없다. 수정하지 않은 예제 프로젝트를 그대로 상업적으로 재배포(판매)하는 것만 금지다. **CC0 선언은 아니다.** 저자가 직접 밝힌 사용 허락이다. 이 게임은 예제를 그대로 파는 것이 아니므로 허용 범위로 판단한다(법률 자문 아님).
- **주의: `Gun Shoot.wav`.** 파일 안의 메타데이터(INFO 청크)에 제목 "Smack sound", 작성자 "SoundBible.com"이 적혀 있다. SoundBible의 "Smack" 페이지(https://soundbible.com/441-Smack.html)는 라이선스가 **Personal Use Only**다. 예제 저자가 이 소리를 가공했거나 메타데이터만 남은 것일 수 있고, 두 소리가 실제로 같은지는 청취로 비교하지 않았다. 상업 출시 전에 `Gun Shoot.wav`를 CC0 총성으로 바꾸는 것을 권한다(현재 무기 4종이 이 파일을 공통으로 쓴다).
- `Gun Reload.wav` 메타데이터에는 편집 프로그램(AVS4YOU) 표시만 있다. `Woman Damage/Die.ogg`에는 "Pro Tools"로 인코딩했다는 표시만 있다. 출처 정보는 없다.

## 5. 사용자 확인 질문

- Toon Shooter Game Kit와 GUI PRO Kit는 어디서(어떤 계정으로) 구했나? 라이선스 문서는?
- `Gun Shoot.wav`를 출시 전에 교체할지(4장 주의)
- ~~GitHub 저장소 `ssa25879/URP_ZombieGame`을 비공개로 바꿀 수 있나?~~ 2026-10-06 사용자가 비공개로 전환함(비로그인 API 조회 404로 확인)
