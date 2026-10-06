# Urban Survival Google Play 등록 준비 체크리스트

- 작성일: 2026-10-06 (Task 14 진행 중 갱신)
- 앱 정보: 제품명 `Urban Survival`, 회사명 `YWS`, 패키지명 `com.yws.urbansurvival`(Play에 올린 뒤에는 변경 불가)
- 관련 문서: 설계 `docs/superpowers/specs/2026-10-06-mobile-port-design.md`, 인수인계 `docs/superpowers/2026-10-06-mobile-port-handoff.md`
- 표기: `[x]` 확인 완료, `[ ]` 미완료, **사용자**는 Play Console 등 사용자가 직접 해야 하는 항목
- Play 정책은 바뀔 수 있으므로 **등록 직전에 Google Play 공식 문서로 다시 확인**한다. 아래 수치(타깃 API 36, 개인 계정 비공개 테스트 12명·14일)는 모바일 기획서 부록 A(2026-10-06 기준)에서 가져온 값이다.

## 1. 빌드 요건 (개발에서 확인 가능)

- [x] 패키지명 `com.yws.urbansurvival`, 회사명 `YWS`, 제품명 `Urban Survival` 설정(`Urban Survival/Mobile/Apply Android Settings` 메뉴, 커밋 `d593480`)
- [x] 가로 화면만 허용(세로 자동 회전 끔), IL2CPP, ARM64만 빌드, 최소 API 25, 타깃 API 36, AAB 출력 설정
- [x] 프로젝트의 URP 호환 모드 정의 `URP_COMPATIBILITY_MODE`를 Android에도 추가(없으면 Unity 6.3이 빌드를 거부)
- [ ] 릴리스 AAB 빌드 성공(서명 포함) — Task 14 Step 4
- [ ] AAB 매니페스트 확인: 패키지명, `targetSdkVersion` ≥ 36, `minSdkVersion` 25, 네이티브 라이브러리 `arm64-v8a`만 포함 — Task 14 Step 5
- [ ] 버전 정책: `bundleVersion` `1.0.0`, `AndroidBundleVersionCode` `1`. **업로드할 때마다 `AndroidBundleVersionCode`를 1씩 올린다.**
- [x] 앱 아이콘: 사용자가 `Assets/Images/AppIcon-1.png`(1254x1254)을 제공했고, 군인 중심 1000px 크롭(`Assets/Images/Android/AppIcon_1024.png`)을 적응형(배경 레이어에 전체 그림, 전경은 투명)·라운드·레거시 슬롯 18개에 적용했다(`Urban Survival/Mobile/Apply Android Icons`). 실제 런처에서 보이는 모양은 빌드해서 확인해야 한다
- [ ] 스플래시: Unity 라이선스 종류에 따라 Unity 로고 표시 여부가 달라진다. 사용자 확인 필요

## 2. 서명 (사용자 보관)

- Play App Signing을 쓰는 것을 기준으로 한다. 개발자는 **업로드 키**를 만들어 안전하게 보관한다.
- [ ] 업로드 키스토어 생성. 위치는 `D:\work\Zombie\Keystore\`(`.gitignore`에 `/Keystore/`, `*.keystore`, `*.jks`, `keystore.properties` 있음). **사용자**가 직접 만든다(비밀번호를 대화·파일에 적지 않는다). 예:
  ```bash
  "C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe" -genkeypair -v -keystore Keystore/urbansurvival-upload.keystore -alias urbansurvival-upload -keyalg RSA -keysize 2048 -validity 10000
  ```
- [ ] Unity `Project Settings > Player > Publishing Settings`에 키스토어 경로·별칭·비밀번호 입력(**사용자**)
- [ ] 분실하면 안 되는 정보를 따로 백업: 키스토어 파일, 키스토어 비밀번호, 키 별칭, 키 별칭 비밀번호(**사용자**, Git 커밋 금지)
- 개발 중 기기 테스트용 APK는 Unity의 debug 서명으로 만든다. Play에 올릴 AAB만 업로드 키로 서명한다.

## 3. 개인정보·데이터 수집 점검 (코드 확인 결과)

점검 방법: `Assets/Scripts`, `Assets/Modules`, `Assets/Game`, `Assets/Editor`에서 `UnityWebRequest`, `UnityEngine.Analytics`, `Unity.Services`, `Advertisement`, `AnalyticsService`, `System.Net.Http`, `WWW(`를 검색했다.

- [x] 게임 코드는 네트워크 통신, 계정, 광고, 인앱 결제를 쓰지 않는다(검색 결과 0건). 저장 데이터는 `PlayerPrefs`(설정, 조준 모드, 튜토리얼 확인 여부, 연습 최고 기록)뿐이고 기기 밖으로 보내지 않는다.
- [ ] **주의: Unity Analytics가 켜져 있다.** `ProjectSettings/UnityConnectSettings.asset`에서 `UnityAnalyticsSettings.m_Enabled: 1`, `m_InitializeOnStartup: 1`이고, `Packages/manifest.json`에 `com.unity.analytics` 3.8.2와 `com.unity.modules.unityanalytics`가 있다. 코드에서 직접 이벤트를 보내지 않아도 플레이어가 시작할 때 Unity의 기본 분석(기기·세션 정보)이 전송될 수 있다. 이 경우 **Data Safety 양식에 수집 항목(예: 기기 또는 기타 ID, 앱 활동 등)을 신고하고 개인정보처리방침에 적어야 한다.** 분석을 쓸 계획이 없다면 끄는 것이 신고 부담이 작다. **끌지 말지는 사용자가 결정**한다(끄려면 Project Settings > Services > Analytics를 끄고 필요 없는 `com.unity.analytics` 패키지 제거를 검토).
  - **2026-10-06 사용자 결정: 끈다.** 설정 변경과 릴리스 재빌드, 매니페스트의 `INTERNET` 제거 확인은 다음 작업(세션 1)에서 한다. 확인되면 이 항목을 체크하고 Data Safety는 "수집 없음"으로 작성한다.
- [ ] 개인정보처리방침 URL 준비(**사용자**). 수집이 없더라도 Play는 URL을 요구한다.
- [ ] Data Safety 양식 작성(**사용자**), 위 Analytics 결정 반영
- [ ] 권한: Android 빌드가 요구하는 권한을 AAB 매니페스트에서 확인한다(인터넷 권한이 포함되는지 포함). 불필요한 권한이 있으면 제거 방법을 검토 — Task 14 Step 5
- [ ] 대상 연령·콘텐츠 등급 설문(**사용자**): 이 게임은 좀비를 총으로 쏘는 폭력 표현이 있다. 설문에 사실대로 답한다.

## 4. Play Console에서 사용자가 직접 할 일

- [ ] Google Play 개발자 계정 생성, 등록비 결제, 개발자 신원 확인
- [ ] 앱 생성(앱 이름 `Urban Survival`, 패키지명 `com.yws.urbansurvival`)
- [ ] 앱 카테고리, 대상 연령, 콘텐츠 등급 설문, Data Safety, 개인정보처리방침 URL
- [ ] 스토어 설명(짧은 설명, 자세한 설명), 연락처 이메일
- [ ] 이미지: 앱 아이콘 512×512 PNG(준비됨: `StoreAssets/play-icon-512.png`), 피처 그래픽 1024×500(준비됨: `StoreAssets/play-feature-graphic-1024x500.png`, 알파 없는 24bit PNG, 원본은 `Assets/Images/AppIcon-2.png`), 휴대전화 스크린샷(최소 2장, 가로 화면, **미준비**). 태블릿 스크린샷은 선택 사항
- [ ] 내부/비공개 테스트 트랙 설정, 테스트 릴리스 업로드
- [ ] Production 제출
- [ ] **신규 개인 개발자 계정은 Production 접근 전 비공개 테스트 요건이 있을 수 있다.** 모바일 기획서 기준은 최소 12명의 테스터가 14일 동안 지속적으로 참여한 뒤 Production 접근을 신청하는 방식이다. 계정 종류(개인/조직)와 현재 정책을 등록 직전에 공식 문서로 확인한다.

## 5. 실기기 검수 요약 (Task 12·13 결과를 채운다)

- [ ] S26 Ultra: 설치·실행, 한 판 완료, 멀티터치, 노치/펀치홀, 한글 튜토리얼 렌더링
- [ ] XiaoxinPad 2025: 설치·실행, 한 판 완료, 화면비(약 16:10) 레이아웃, 한글 튜토리얼 렌더링
- [ ] 두 기기에서 동시 적 500마리 + 보스 상태 FPS 측정(목표 60, 최소 30)
- [ ] 30분 이상 연속 플레이(입력 고착·메모리·발열·크래시)

## 6. 릴리스 런북 (AAB 빌드 → Play 업로드)

앞 단계가 막히면 다음 단계로 넘어가지 않는다. 빌드는 사용자가 요청할 때만 한다.

1. **[사용자]** Play Console 개발자 계정 생성·결제·본인 확인, 앱 생성(`Urban Survival`, `com.yws.urbansurvival`).
2. **[사용자]** 업로드 키스토어 생성(2장 명령). 비밀번호는 대화·파일에 적지 않는다.
3. **[사용자 + 개발]** Unity `Project Settings > Player > Publishing Settings`에서 Custom Keystore를 지정하고 키스토어·별칭 비밀번호를 입력한다(**사용자**). 입력 후 개발이 릴리스 빌드를 만든다.
4. **[개발]** `EditorUserBuildSettings.buildAppBundle = true`, 개발 빌드 끔, `AndroidBundleVersionCode`가 이전 업로드보다 큰지 확인.
5. **[개발]** 빌드(활성 대상이 Android일 때). 개발 빌드와 달리 `--options`는 넘기지 않는다(`None` 값은 지원되지 않아 빌드가 시작되지 않는다).
   ```bash
   unity command --caller plugin --skill unity-cli --no-banner build --target Android --outputPath Builds/Android/UrbanSurvival-1.0.0-1.aab --confirm true --detach
   ```
   완료 여부는 `unity command ... build_status`로 확인한다(`unity job status`는 "제출됨"까지만 알려 준다). 빌드 중 "Unsupported Input Handling" 창이 뜨면 Ignore.
6. **[개발]** AAB 검증: 파일 크기, 패키지명, `targetSdkVersion` ≥ 36, `minSdkVersion` 25, `arm64-v8a`만 포함, 권한 목록(인터넷 포함 여부). bundletool은 다운로드가 필요해 사용자 승인 후에 쓴다.
7. **[사용자]** Play Console: 앱 콘텐츠(개인정보처리방침 URL, 광고 없음, 콘텐츠 등급 설문, 타겟 연령, 데이터 보안), 스토어 등록정보(`docs/store/store-listing-draft.md`), 가격·배포 국가.
8. **[사용자]** 처음 업로드할 때 Play 앱 서명 등록, 내부 테스트 트랙에 AAB 업로드 → 테스터 초대.
9. **[사용자]** 개인 계정이면 비공개 테스트(12명·14일 요건, 등록 직전에 최신 정책 확인) 후 Production 신청.
10. **[개발]** 업로드 후 Play Console의 출시 전 보고서(Pre-launch report)와 크래시 보고서를 확인해 문제를 고친다.

## 7. 출시 전 발견된 문제와 조치 (2026-10-06 점검)

### 출시 차단(해결 전에는 올리지 않는다)

| 문제 | 근거 | 조치 | 담당 |
|---|---|---|---|
| **저장소가 공개 상태** | GitHub API 조회 결과 `private: false`. GUI PRO Kit는 이 브랜치에서 추적 제외했으나(커밋 기록·다른 브랜치에는 남음) 공개 저장소의 과거 기록에 파일이 있다 | 저장소 비공개 전환 또는 기록 삭제(`third-party-not-in-repo.md` 완전 제거 방법) | 사용자 |
| 출처 불명 오디오·초기 프로젝트 자원 | 효과음 7개, 일부 모델·재질에 출처 기록 없음(`Searching.ogg`는 2026-10-06 CC0 확인됨) | 출처 확인 또는 CC0 대체 | 사용자 + 개발 |
| Play 계정·키스토어·개인정보처리방침 URL·스크린샷 | 아직 없음 | 런북 1·2·7 | 사용자 |

### 설정 정리(에디터가 자유로울 때 개발이 처리)

| 항목 | 현재 값 | 문제 | 조치 |
|---|---|---|---|
| `AndroidTVCompatibility` | ~~`1`~~ → **`0`으로 변경 완료**(2026-10-06) | 기기 `dumpsys`에서 `LEANBACK_LAUNCHER` 카테고리가 확인되어 TV 호환이 실제로 켜져 있었음. 터치 전용이라 끔 | 다음 빌드에서 `LEANBACK_LAUNCHER`가 사라졌는지 `adb shell dumpsys package com.yws.urbansurvival`로 확인 |
| Unity 스플래시 로고 | 표시(`m_ShowUnitySplashLogo: 1`) | 라이선스에 따라 끌 수 있음 | 사용자가 Unity 라이선스 확인 후 결정 |
| Unity Analytics | 켜짐, 시작 시 초기화 | Data Safety·개인정보처리방침 영향 | 켤지 끌지 사용자 결정(`store-listing-draft.md` 6·7장) |
| Active Input Handling | `Both` | 빌드 때 경고 창. Android에서는 하나만 권장 | 현재는 Ignore로 진행. 단일 방식으로 바꾸려면 새 Input System 사용처 조사·재검증 필요 |
| 기본 품질 단계 | PC용 단계 그대로 | 모바일 성능 미측정 | Task 13: STRESS(500마리+보스) 측정 후 병목이 있으면 모바일 단계 추가 |

### 품질(실기기 확인 필요)

- 한글 튜토리얼이 Android에서 깨지는지(OS 폰트 이름으로 찾는 방식). 안 보이면 한글 폰트 에셋을 프로젝트에 넣어야 한다.
- 로그 `_burst_0_0` 네이티브 플러그인 로드 실패 메시지(릴리스 빌드에서 재확인).
- `lanternDouble`의 `Assets/Materials/light.mat`(`Unlit/Color`)이 오류 셰이더로 나와 전구가 분홍색일 수 있음 → URP Unlit으로 교체(사용자 승인 후).
- S26 Ultra 설치·노치 확인, 30분 연속 플레이, 두 기기 FPS(목표 60, 최소 30).

### 개발 빌드(APK `1.0.0`/코드 1)를 태블릿에 설치해서 기기가 보고한 값 (2026-10-06 `adb shell dumpsys package`)

- `targetSdk=36`, `minSdk=25`, `versionName=1.0.0`, `versionCode=1`, `primaryCpuAbi=arm64-v8a`(32비트 없음).
- 요청 권한: `android.permission.INTERNET` 하나. 개발 빌드는 프로파일러 연결 때문에 INTERNET이 들어갈 수 있다. **릴리스 빌드에서 INTERNET이 남는지 다시 확인**하고, 남으면 Data Safety·개인정보처리방침 서술에 영향이 있으니 불필요하면 제거 설정을 검토한다(게임은 네트워크를 쓰지 않는다. Unity Analytics를 켜 두면 INTERNET이 필요하다).
  - 2026-10-06 릴리스 APK(`20450fd` 기준) 확인: `INTERNET`이 **남아 있음**(`forceInternetPermission=false`, Analytics 켜짐 → Analytics가 원인으로 추정, 미확인). 이 APK는 **Unity 디버그 키로 서명한 기기 테스트용이라 Play에 올릴 수 없다.** 업로드는 업로드 키스토어로 서명한 AAB만 한다. Analytics를 끈 뒤 다시 확인한다.
- 런처 카테고리에 `LEANBACK_LAUNCHER`가 있었다(→ TV 호환을 껐으므로 다음 빌드에서 사라져야 한다).
