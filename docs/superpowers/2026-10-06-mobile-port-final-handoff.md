# Urban Survival Android 모바일 포팅 — 최종 인수인계

- 작성일: 2026-10-06
- 대상 독자: 이 프로젝트를 이어받는 개발자(또는 다음 AI 세션)
- 이 문서는 **요약과 지도**다. Task별 상세 기록·검증 증거는 `docs/superpowers/2026-10-06-mobile-port-handoff.md`, 설계·계획은 `specs/`, `plans/`에 있다.
- 사실만 적었다. 확인하지 못한 것은 "미검증"으로 표시했다.

## 1. 한눈에 보기

| 항목 | 상태 |
|---|---|
| 모바일 입력·터치 UI·조준 모드·설정 | **구현 완료** (에디터 검증) |
| 자동 테스트 | EditMode **72/72 통과** (기존 14건 + 모바일 로직 58건) |
| Android 개발용 APK | **빌드·태블릿 설치 완료** (`1.0.0`/코드 1, 180 MB, 2026-10-06 13:26) |
| 실기기 검증 | 태블릿 1대(TB373FU)만. **S26 Ultra 미검증** |
| 성능 목표(500마리+보스 60 FPS) | **미측정** (개발 빌드의 STRESS 버튼으로 재야 함) |
| 릴리스(AAB) | **미생성** (키스토어·Play 계정 없음) |
| Play 스토어 등록 준비 | 문서·이미지 준비됨, 사용자 몫 항목 남음(7장) |

## 2. 저장소와 브랜치

| 저장소 | 주소 | 비고 |
|---|---|---|
| 기존 | https://github.com/ssa25879/URP_ZombieGame | 브랜치 `SideProject`, `SideProject-Mobile`, `main`. **기록에 GUI PRO Kit 포함, 공개 상태**. 비공개 전환 예정(사용자) |
| 공개용(신규) | https://github.com/ssa25879/UrbanSurvival | 기존 기록에서 GUI PRO Kit를 지운 사본(아래 "공개용 저장소 만든 방법") |

- 모바일 작업 브랜치: **`SideProject-Mobile`** (`SideProject`의 `d29867b`에서 분기). 모바일 변경은 이 브랜치에만 있다. `main`과 `SideProject`는 건드리지 않았다.
- 로컬 작업 폴더: `D:\work\Zombie`. 이 폴더에는 Git 추적에서 빠진 `Assets/GUI PRO Kit - Simple Casual/`이 **로컬에 그대로 있다**(`.gitignore`로 제외).
- **AGENTS.md(로컬, Git 추적 안 함)는 갱신하지 않았다.** AGENTS.md는 "작업 브랜치는 `SideProject`만"이라고 되어 있어 모바일 브랜치와 충돌한다. 반영 여부는 사용자가 정한다.

### GUI PRO Kit (저장소에 없음)

- 이유: 저장소가 공개이고 Asset Store 에셋 원본 재배포 가능성이 있어 사용자 지시로 제거. 프로젝트가 실제로 쓰는 것은 6개(스프라이트 5 + 일시정지 프리팹 1)뿐이다. 자세한 내용과 복구 방법은 `docs/store/third-party-not-in-repo.md`.
- **새로 clone하면 HUD 패널 프레임과 일시정지 메뉴가 비어 보인다.** Asset Store에서 같은 패키지를 `Assets/GUI PRO Kit - Simple Casual/`에 임포트하면 GUID가 복원되어 이어진다.

### 공개용 저장소 만든 방법 (재현용)

원본 폴더는 건드리지 않고 별도 bare 복제본에서 `git filter-branch`로 처리했다.

```bash
git clone --bare --no-hardlinks D:/work/Zombie D:/work/Zombie_clean.git
cd D:/work/Zombie_clean.git && git remote remove origin
FILTER_BRANCH_SQUELCH_WARNING=1 git filter-branch -f --prune-empty --tag-name-filter cat \
  --index-filter 'git rm -r -q --cached --ignore-unmatch -- "Assets/GUI PRO Kit - Simple Casual" "Assets/GUI PRO Kit - Simple Casual.meta"' -- --branches
# 이전 기록 제거: refs/original 삭제, reflog 만료, gc --prune=now
```

- 검증: GUI PRO가 나오는 커밋 0개, 도달 가능한 객체 0개, `fsck` 이상 없음, `SideProject-Mobile`·`main` 최신 트리 해시가 원본과 동일, `SideProject`만 트리가 다름(GUI PRO 제거). 용량 1,701 MB → 147 MB. 커밋 SHA는 기존 저장소와 다르다.
- 이 사본에는 작성자 이메일이 그대로 들어 있다(`ssaqwe123@gmail.com`, `contributor@noreply.invalid`). 효과음 7개(출처 미확인)도 포함되어 있다. 둘 다 사용자 결정 대기(7장).

## 3. 구현 내용 (`SideProject-Mobile`)

### 구조

```
[터치 UI]  --쓴다-->  MobileInputState(정적)  --읽는다-->  PlayerInput  -->  PlayerMovement / PlayerShooter(전투 로직 미수정)
 MobileTouchOverlay                                           ^ aimWorldDirection
   (스틱·FIRE·R·슬롯·일시정지)                               AutoAimTargeting  (Zombie.alive, GunData.range)
설정 창 --> MobileAimModeSettingsBinder --> MobileAimSettings(PlayerPrefs "MobileAimMode")
```

- 전투 판정은 PC와 같다. **입력만 교체**한다. `PlayerShooter`·`Gun`은 읽기 접근자 2개 외에 수정하지 않았다.
- 모바일 UI는 씬 파일을 수정하지 않고 **런타임에 코드로 생성**한다(씬이 바이너리라서). 모바일(`MobilePlatform.IsMobile`)에서만 만들어진다.

### 파일 지도

| 위치 | 내용 |
|---|---|
| `Assets/Scripts/MobileCore/` (어셈블리 `UrbanSurvival.MobileCore`) | 게임 코드에 의존하지 않는 순수 로직: `MobileInputState`, `MobilePlatform`, `JoystickMath`, `TwinStickFireTracker`, `AutoAimTargeting`, `MobileAimMode`(+`MobileAimSettings`), `MobileTutorialText`, `WeaponDisplayName` |
| `Assets/Scripts/MobileUI/` | `MobileTouchOverlay`(조립·상태), `TouchJoystick`, `TouchButton`, `MobileUIFactory`, `SafeAreaFitter`, `MobileHudLayout`(탄약 패널 상단 중앙), `MobileAimModeSettingsBinder`(설정 창 토글), `MobileQuitHider`, `MobilePerformance`(프레임 설정 + 개발 빌드 전용 FPS/STRESS) |
| `Assets/Scripts/PlayerInput.cs` | 모바일 입력 경로(`ReadMobileInput`), `aimWorldDirection`, `RequireFireRelease` |
| `Assets/Scripts/PlayerMovement.cs` | `hasAimWorldDirection`이면 모바일 조준 방향 사용 |
| `Assets/Scripts/Zombie.cs` | 정적 `Zombie.alive` 목록 |
| `Assets/Scripts/PlayerShooter.cs` | `CurrentSlotIndex`, `IsSlotUnlocked` (읽기 전용) |
| `Assets/Scripts/WeaponHUD.cs`, `ReloadIndicator.cs`, `TutorialPopup.cs` | 표기 변경(AR), 모바일 문구, 한글 폰트 후보 추가 |
| `Assets/Modules/GameSettingsKit/Runtime/SettingsStore.cs` | 모바일에서 해상도·전체 화면·vSync 적용 건너뜀(플랫폼 일반 가드) |
| `Assets/Editor/MobileEditorMenu.cs` | 메뉴 3개(아래) |
| `Assets/Game/Tests/EditMode/Mobile*.cs`, `JoystickMathTests.cs` 등 | 모바일 로직 테스트 58건 |
| `Assets/Images/`, `StoreAssets/` | 앱 아이콘 원본·가공본, Play 스토어 이미지 |
| `docs/store/` | 에셋 라이선스 점검, 스토어 문안·개인정보처리방침 초안, GUI PRO 제외 안내 |

### 모바일 동작 규칙 (현재)

- 조준 모드(설정의 `TWIN-STICK AIM` 토글, 기본 오토 에임):
  - **오토 에임:** 이동 스틱 + FIRE 버튼. FIRE를 누르는 동안 사거리 안 가장 가까운 적을 조준(없으면 이동 방향 → 마지막 방향).
  - **쌍둥이 스틱:** 오른쪽 스틱으로 조준·발사. 스틱을 당긴 채 탄창이 비면 **자동 재장전**하고, 재장전이 끝나면 당기고 있는 한 **계속 발사**한다.
- 권총·샷건(단발)도 **누르고 있으면 자동 연속 발사**(모바일 전용). 발사 간격은 `Gun`이 제한한다. 탄창이 비면 오토 에임에서는 `Empty`에서 멈추고 R 또는 FIRE를 다시 눌러야 재장전(기존 규칙).
- 정지·포커스 변경·게임 오버·조준 모드 전환 때 입력 상태를 초기화하고 다음 발사는 한 번 놓은 뒤로 미룬다(오발 방지).
- 배치(1920×1080 기준): 이동 스틱 왼쪽 아래, FIRE·R 오른쪽 아래 모서리, 그 위에 무기 슬롯 4칸(`PISTOL / AR / SMG / SG`), 일시정지 오른쪽 위, **탄약 패널 상단 중앙**(보스가 있으면 보스 체력바 아래). 화면 스타일은 기존 HUD(둥근 패널 + 앰버 상단 라인 + Kenney 폰트)와 같다.
- `AK`는 화면 표기만 `AR`로 바꿨다(씬의 총 오브젝트 이름은 `AK` 그대로).
- 모바일에서 QUIT 버튼 숨김, 모바일용 튜토리얼 문구, `RELOAD`/`NO AMMO` 문구(키 안내 제거).

## 4. 앱 설정 (Android)

회사명 `YWS`, 제품명 `Urban Survival`, 패키지명 `com.yws.urbansurvival`, 버전 `1.0.0`/코드 `1`, 가로 화면만, IL2CPP, ARM64만, 최소 API 25, 타깃 API 36, **Android TV 호환 끔**, `URP_COMPATIBILITY_MODE` 정의 추가(Android), 아이콘 18개 슬롯 적용.

- **주의:** 회사명·제품명을 바꿨으므로 Windows 빌드·에디터의 PlayerPrefs 저장 위치가 `DefaultCompany/Zombie`에서 `YWS/Urban Survival`로 바뀌었다(이전 값은 삭제되지 않았고 새 위치에서 안 보일 뿐).
- **현재 `buildAppBundle = false`**(개발용 APK 용). AAB를 만들 때 `true`로 켠다.
- 에디터 메뉴(`Assets/Editor/MobileEditorMenu.cs`): `Urban Survival/Mobile/Force Mobile Input In Editor`(에디터에서 모바일 입력 강제, EditorPrefs, **끝나면 끈다**), `Apply Android Settings`, `Apply Android Icons`.

## 5. 빌드·테스트 방법

에디터에는 Unity CLI(`unity`)가 연결되어 있다. 아래 접두를 쓴다.

```bash
U="unity command --caller plugin --skill unity-cli --no-banner --result-only"
$U run_tests --mode EditMode                 # 72건, 결과 즉시 반환
$U eval '<C# 한 줄>'                          # 에디터에서 C# 실행(Roslyn)
$U eval_file --file <경로.cs>                 # 긴 스크립트(5초 메인 스레드 제한 주의)
$U editor_play / editor_stop
```

- **에디터가 Android 대상이다.** PC(Windows) 빌드를 하려면 먼저 `$U switch_build_target --target StandaloneWindows64 --confirm true`(재임포트 수 분). 지금은 Android 상태.
- **개발용 APK 빌드:**
  ```bash
  unity command --caller plugin --skill unity-cli --no-banner build --target Android \
    --outputPath Builds/Android/UrbanSurvival-dev.apk --options '["Development","AllowDebugging"]' --confirm true --detach
  ```
  - 완료는 `$U build_status`로 본다(`unity job status`는 "제출됨"까지만 알려 준다).
  - **"Unsupported Input Handling on Android"(`Both`) 창이 뜨면 Ignore.** 이 창이 떠 있는 동안 에디터가 응답하지 않아 CLI가 타임아웃 난다. 세션 동안 안 묻게 하려면 "Don't ask again" 체크.
  - 릴리스(AAB) 빌드는 `--options`를 넘기지 않는다(`None` 값은 지원 안 됨).
  - 소요: 첫 빌드 약 22분, 이후 약 8분.
- 설치: `adb -s <기기> install -r Builds/Android/UrbanSurvival-dev.apk` (adb는 Unity Android 모듈 `PlaybackEngines/AndroidPlayer/SDK/platform-tools`).
- **사용자 규칙:** 빌드는 사용자가 요청할 때만 한다(메모리 `feedback_no-build-exe-without-asking`). 빌드 *실행·화면 캡처*는 먼저 묻는다.

### 에디터에서 모바일 흐름 확인하는 법

`Force Mobile Input In Editor`를 켜고 `UrbanSurvival.unity`를 플레이한 뒤 `eval`로 `MobileInputState`에 값을 쓰거나(`MobileInputState.AimStick = ...`) 합성 `PointerEventData`로 위젯을 누른다. 플레이어가 서 있으면 사망해 입력이 전부 초기화되므로 `PlayerHealth.startingHealth`를 크게 해 무적으로 만든다. 끝나면 플래그를 끈다.

## 6. 검증 현황

- 자동: EditMode 72/72(기존 `SpecDataTests` 14 + 모바일 58).
- 에디터 플레이(강제 모바일, 입력 주입): 오토 에임 대상 선택, 쌍둥이 스틱, 일시정지·재개 오발 방지, 재장전 후 계속 누름 규칙, 슬롯 교체, 설정 창 토글 저장·복원, 인트로 설정 창, QUIT 숨김, 모바일 문구, 탄약 패널 보스 시 위치, 단발 무기 자동 연속 발사, 쌍둥이 스틱 자동 재장전·재개, 아이콘 슬롯 18/18.
- 실기기(TB373FU, Android 15, 가로 2944×1840): 앱 기동, 크래시 없음, 터치 UI 배치 확인(캡처), 개발 빌드 FPS 표시 `60.0`(좀비 9마리). 사용자가 직접 플레이해 보았다(결과 상세는 기록 없음).
- `adb dumpsys` 확인값: `targetSdk=36`, `minSdk=25`, `versionName=1.0.0`, `primaryCpuAbi=arm64-v8a`, 권한 `INTERNET`(개발 빌드).

## 7. 열린 항목

### 사용자 결정·작업 대기

1. 공개용 저장소: 작성자 이메일을 가릴지, 효과음 7개를 기록에서 같이 뺄지. 기존 저장소 비공개 전환.
2. Unity Analytics를 끌지(Data Safety 영향), Unity 스플래시 로고(라이선스 확인).
3. Play 개발자 계정, 업로드 키스토어, 개인정보처리방침 URL, 연락처, 가로 스크린샷 최소 2장.
4. 에셋 출처: `Assets/Audios` 효과음 7개(출처 미확인), `singularity_calm.wav`(페이지 주소 없음), Toon Shooter(사용자 진술 CC0, 원문 미확인), GUI PRO(앱에는 6개 포함됨, 구매 라이선스 확인), 초기 프로젝트 모델·재질.
5. AGENTS.md 갱신(모바일 브랜치, GUI PRO 제외, 빌드 요청 시 규칙).

### 개발·검증 대기

- S26 Ultra 설치·노치·터치 검수, 멀티터치, 한글 튜토리얼 렌더링(Windows 폰트 이름 + Noto/sans-serif 후보로 찾는 방식, 실기기 확인 필요, 안 되면 한글 폰트 에셋 추가).
- 성능: 두 기기에서 STRESS(500마리+보스) FPS 측정(목표 60, 최소 30), 병목이 있을 때만 모바일 품질 단계 추가, 30분 연속 플레이. 현재 `Application.targetFrameRate=60`, vSync 모바일에서 끔.
- 릴리스 빌드(AAB)에서 `INTERNET` 권한·`LEANBACK_LAUNCHER` 제거 확인.
- 알려진 문제: 로그 `_burst_0_0` 네이티브 플러그인 로드 실패(원인 미확인), `lanternDouble`의 `light.mat`(`Unlit/Color`)이 오류 셰이더로 대체되어 전구가 분홍색일 수 있음(`light.mat`을 URP Unlit으로 교체하려면 사용자 승인), "TextMesh Pro Essential Resources are missing" 경고.
- Active Input Handling `Both` 경고(단일 방식으로 바꾸려면 새 Input System 사용처 조사·재검증).
- 터치 영역 크기(약 48dp), 메뉴(일시정지·게임 오버·결과)의 터치 조작은 실기기에서 확인되지 않았다.

### 다음 작업 순서(권장)

1. 사용자가 7장 1~3번 결정 → 공개용 저장소 반영.
2. 새 개발 빌드로 두 기기 검수 + STRESS 측정(빌드는 요청 시).
3. 키스토어 생성 후 릴리스 AAB 빌드, 권한·매니페스트 확인, Play 내부 테스트 업로드(`docs/superpowers/plans/mobile-store-checklist.md` 6장 런북).

## 8. 내린 결정 기록 (장부 발췌)

- 별도 worktree 없이 `SideProject-Mobile`에서 직접 작업(에디터가 이 폴더를 열고 있음).
- 인수인계는 사용자의 기존 `urban-survival-handoff.md`와 섞지 않고 새 파일로 작성.
- 에디터가 한 대라 서브에이전트는 순차 실행(터치 UI Task 8·9, 스타일 개선 Task 9b).
- `PlayerInput`/`PlayerMovement`/`Zombie` 같은 기존 `.cs`는 Rider 훅이 파일 전체를 재포맷해 diff가 부풀 수 있어 **PowerShell 정확 치환**으로 수정(CRLF·BOM 유지, 수정 직후 `git diff --stat` 확인). 메모리 `rider-hook-reformats-cs-files` 참고.
- 메뉴 버튼 크기 조정 코드는 기기 없이 측정할 수 없어 추가하지 않음.
- 사용자 지시 "빌드는 요청시 진행"에 따라 계획서의 빌드 단계(Task 12 재검수, Task 13 STRESS 측정, Task 14 AAB)는 보류.
- Rider 포맷·Unity 자동 재저장 파일(`Assets/TextMesh Pro/*` 대량 변경, `Headlights.mat`, `Fonts`/`ProjectSettings`의 `runInBackground`)은 커밋하지 않았다. `ProjectSettings.asset`은 의도한 hunk만 `git apply --cached`로 스테이징.

## 9. 알아 둘 함정

- `execute_code`(MCP, CodeDom C# 6)에서는 `UnityEngine.Object`를 완전한 이름으로 쓴다. CLI `eval`은 Roslyn이라 더 자유롭다. 작은따옴표 이스케이프가 까다로우면 `eval_file`을 쓴다.
- 플레이 모드 진입 직후 첫 호출은 도메인 리로드로 타임아웃이 날 수 있다.
- 에디터 플레이는 포커스가 없으면 시간이 안 흐른다(`runInBackground`). 이 프로젝트는 `runInBackground: 1`이 로컬 변경으로 남아 있고 커밋하지 않았다.
- 에디터가 모달 창(Input Handling 등) 뒤에 있으면 모든 CLI·MCP 호출이 막힌다.
- `.superpowers/`(장부)는 `.git/info/exclude`로 로컬 제외. `Builds/`, `Keystore/`는 `.gitignore`.

## 부록. 저장소 운영 방식 (2026-10-06 사용자 결정)

- **소규모 업데이트:** 작업 폴더의 `origin`(`URP_ZombieGame`)에 평소처럼 커밋·푸시한다.
- **큰 업데이트:** 사용자가 요청할 때 공개용 저장소 `UrbanSurvival`에 올린다. 절차는 `tools/publish-clean-repo.sh`(제외 경로를 모든 커밋에서 지운 사본 생성·검증, 푸시 안 함) → 결과 확인 → `tools/publish-clean-repo.sh --push`(일반 푸시, `--force` 없음).
- 기록 정리는 결정적이라 같은 커밋에서 다시 만들면 같은 SHA가 나온다(`SideProject-Mobile` 323b1f1 재현 확인). 그래서 새 커밋은 공개 저장소에 fast-forward로 올라간다. 단, 작성자 이메일을 가리거나 제외 경로를 늘리면 SHA가 모두 바뀌어 공개 저장소를 새로 만들어야 한다.
- 작업 폴더 `origin`은 `URP_ZombieGame`을 유지한다.
