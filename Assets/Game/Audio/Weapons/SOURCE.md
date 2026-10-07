# 무기 발사음 출처

- 팩: The Free Firearm Sound Library — Ben Jaszczak, Brian Nelson, Kevin Heras, Matthew Nanney
- 페이지: https://opengameart.org/content/the-free-firearm-sound-library
- 라이선스: CC0 1.0 (Public Domain Dedication). 크레딧 불필요. 2026-10-06 페이지에서 확인.
- 받은 파일: `Prepared SFX Library.7z` (193,954,738 바이트, 2026-10-06)
- 선택: 사용자가 미리듣기 후보 중에서 고름(2026-10-06)

| 프로젝트 파일 | 원본 파일 | 원본 설명(Master Sheet) | 쓰는 곳 |
|---|---|---|---|
| `Pistol_1911.wav` | `1911/A_42P.wav` | 1911 .45 handgun, near distance | `Pistol Data.asset` |
| `Rifle_AK47.wav` | `AK-47/C_28P.wav` | AK-47 7.62x39 single gunshot, near distance | `Gun Data.asset` (소총, 화면 표기 AR) |
| `SMG_CarlGustavM45.wav` | `Carl Gustav M45/G_31P.wav` | Carl Gustav M45 9mm single gunshot, near distance | `SMG Data.asset` |
| `Shotgun_WinchesterModel12.wav` | `Model 12/K_22P.wav` | Winchester Model 12 12 gauge pump, near distance | `Shotgun Data.asset` |

## 가공
- 각 원본 파일에 들어 있는 첫 발만 잘랐다. 시작은 신호가 소음 수준(+6 dB)을 벗어나기 20 ms 전, 끝은 잔향이 소음 수준(+6 dB)까지 줄어든 지점 + 50 ms. 단, 다음 발 100 ms 전과 3초 중 먼저 오는 쪽을 넘지 않는다. 끝의 25%(최소 300 ms)는 코사인 곡선으로 페이드한다.
- 길이: 권총 2.17초, 소총 2.60초(다음 발 직전에서 끝), SMG 2.08초.
- **산탄총은 예외(사용자 선택, 2026-10-06):** 위 기준으로 자른 3.03초 버전 대신 처음 자른 짧은 버전(0.91초)을 쓴다. 기준: 시작은 피크의 5%를 넘기 10 ms 전, 끝은 200 ms 동안 피크의 1% 아래로 내려간 지점 + 0.1초, 끝 30 ms 직선 페이드.
- 형식은 원본 그대로(96 kHz, 24비트, 스테레오)이고 음량은 바꾸지 않았다.
- 대체한 음원: `Assets/Audios/Gun Shoot.wav`(이제민 예제 출처. 메타데이터에 SoundBible "Smack"(Personal Use Only)이 적혀 있어 교체). 원본 파일은 지우지 않는다.
