# Urban Survival

탑뷰·쿼터뷰 좀비 서바이벌 슈터입니다. Unity 6(6000.3) URP로 만든 PC 싱글 플레이 게임이며, `SideProject-Mobile` 브랜치는 같은 전투 규칙에 터치 입력을 붙인 Android 포팅입니다.

## 게임
- WASD 이동과 마우스 조준(모바일은 이동 스틱 + 오토 에임 FIRE 버튼, 설정에서 쌍둥이 스틱 선택 가능)
- 권총으로 시작해 처치 드랍으로 소총·SMG·산탄총을 얻고 1~4번 키로 교체
- 30분 보스를 처치하면 1차 목표 달성, 이후 무한 모드
- 10분마다 등장하는 보스, 강화 적 웨이브, 회복·탄약·자석 아이템

## 브랜치
| 브랜치 | 내용 |
|---|---|
| `SideProject` | PC 버전 |
| `SideProject-Mobile` | Android 포팅(터치 UI, 안전 영역, 오토 에임) |
| `main` | 초기 기준선. 직접 수정하지 않습니다 |

## 열기
1. Unity 6000.3.19f1로 프로젝트 폴더를 엽니다.
2. `Assets/Game/Scenes/Intro.unity`를 열고 재생합니다(빌드 순서: Intro → UrbanSurvival).
3. EditMode 테스트는 Test Runner에서 실행합니다.
4. 모바일 브랜치에서 Android 설정은 메뉴 `Urban Survival > Mobile > Apply Android Settings`로 적용합니다.

## 외부 에셋과 라이선스
- 이 저장소에는 서드파티 에셋(Quaternius, Kenney 등 CC0 팩과 예제 프로젝트 자원)이 들어 있습니다. 출처와 조건은 `docs/store/asset-license-audit.md`를 보세요.
- 일부 유료 에셋(예: GUI PRO Kit)은 라이선스상 공개 저장소에 둘 수 없어 공개본에서 제외합니다. `docs/store/third-party-not-in-repo.md`를 보세요. 제외된 상태로 열면 일부 UI가 비어 보일 수 있습니다.
- **이 저장소의 코드·자체 제작 자원에 대한 오픈소스 라이선스는 아직 정하지 않았습니다.** `LICENSE` 파일이 추가되기 전까지는 별도 허락 없이 재사용·재배포할 수 없습니다.

## 문서
- 개인정보처리방침: `docs/privacy/index.html`
- 출시·스토어 준비: `docs/store/`
- 설계·계획: `docs/superpowers/`
- 검증 기록: `docs/verification/`
