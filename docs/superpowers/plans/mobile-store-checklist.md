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
