# Urban Survival 모바일 포팅 인수인계

- 작업 브랜치: `SideProject-Mobile` (원격 등록 완료)
- 기준 문서: 설계 `docs/superpowers/specs/2026-10-06-mobile-port-design.md`, 계획 `docs/superpowers/plans/2026-10-06-mobile-port.md`
- 실행 방식: 병행(직접 구현 + 작업량이 큰 Task는 서브에이전트). Unity 에디터가 하나라 서브에이전트는 순차 실행한다.
- 이 문서는 Task가 하나 끝날 때마다 항목을 추가한다. 확인하지 못한 항목은 "미검증"으로 적는다.
- 백업: 사용자가 Git 브랜치로 갈음하기로 확인했다(2026-10-06). 파일 백업본은 만들지 않는다.

## 에디터 사용 메모
- `mcp__UnityMCP__execute_code`(CodeDom, C# 6)에서는 `Object`가 모호하므로 `UnityEngine.Object.FindFirstObjectByType<T>()`처럼 완전한 이름을 쓴다.
- 플레이 모드 진입 직후 첫 호출은 도메인 리로드로 타임아웃이 날 수 있다. 몇 초 뒤 다시 호출한다.

## Task 0 — 브랜치·기준 확보 (완료)

- 변경: `.gitignore`에 `*.keystore`, `*.jks`, `keystore.properties`, `/Keystore/` 추가.
- 브랜치 `SideProject-Mobile`, 원격 등록 확인.
- Android 빌드 모듈 확인: Unity 6000.3.19f1 `PlaybackEngines/AndroidPlayer`에 SDK, NDK, OpenJDK가 있다.
- PC 기준(에디터 플레이, `UrbanSurvival.unity`): 플레이 진입 정상, 권총 탄창 10발·Ready, 5초 안에 좀비 소환, 메인 카메라 존재.
- **미검증(사용자 수동 확인 필요):** WASD 이동, 마우스 조준, 좌클릭 발사, R 재장전, 1~4 교체, Esc 일시정지. 키보드·마우스 입력은 자동으로 재현하지 못했다. 이 항목은 Task 7 이후 PC 회귀 확인의 비교 기준이다.
- 활성 빌드 대상은 아직 StandaloneWindows64이다(Task 12에서 Android로 전환).
