# 일시정지 메뉴 아이콘 출처

- 팩: Kenney "Game Icons" (Kenney Vleugels, www.kenney.nl)
- 페이지: https://kenney.nl/assets/game-icons
- 받은 파일: `kenney_game-icons.zip` (1,045,980 바이트, 2026-10-07)
- 라이선스: CC0 1.0 (`KenneyGameIcons_License.txt` 원문 동봉). 개인·상업 사용 가능, 크레딧 불필요.
- 사용 목적: GUI PRO Kit 아이콘을 대체(GUI PRO는 유료 Asset Store 에셋이라 출시 전 제거 검토, 2026-10-07 사용자 결정으로 아이콘 4개를 이 팩으로 통일).

| 프로젝트 파일 | 원본(`PNG/White/2x/`) | 대체하는 GUI PRO 아이콘 | 쓰는 곳 |
|---|---|---|---|
| `KenneyGameIcons_right.png` | `right.png` (100×100 → 여백 자름 + 가로 0.82로 늘림, 52×64) | `Icon_WhiteIcon_Arrow_Continue` | 일시정지 계속 버튼 |
| `KenneyGameIcons_gear.png` | `gear.png` (100×100 → 여백 자름 64×64) | `Icon_WhiteIcon_Setting_s` | 일시정지 설정 버튼 |
| `KenneyGameIcons_return.png` | `return.png` (100×100 → 여백 자름 62×56) | `Icon_WhiteIcon_Restart` | 일시정지 재시작 버튼 |
| `KenneyGameIcons_home.png` | `home.png` (100×100 → 여백 자름 64×64) | `Icon_WhiteIcon_Home` | 일시정지 선택(캐릭터 선택) 버튼 |

- 가공(2026-10-07, 사용자 확인): 원본 100×100의 투명 여백을 잘라냈다(그대로 쓰면 같은 칸에서 약 64% 크기로 작아 보임). 결과 크기: gear 64×64, return 62×56, home 64×64. `right`(계속)는 가로:세로 0.63이라 얇아 보여, 자른 뒤 가로로 늘려 기존 GUI PRO 아이콘과 같은 0.82 비율(52×64)로 만들었다(사용자 선택 "2번안"). 흰색·투명 배경은 그대로다. 게임에서는 `Image.color`로 색을 입힌다(계속: 어두운 색, 나머지: 밝은 색).
- 참고: `return.png`는 반시계 방향 화살표라 기존 재시작 아이콘(시계 방향)과 방향이 다르다.
