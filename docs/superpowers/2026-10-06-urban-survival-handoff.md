# Urban Survival 인수인계 — 남성 음성 적용과 Windows 빌드

기준일: 2026-10-06. 이번 세션에서 실제 수행한 변경과 검증을 기록한다. 전체 게임 기능 검수가 끝났다는 의미는 아니다.

## 완료한 작업

- GitHub `origin/SideProject` 최신화 확인 후 작업했다. 다른 브랜치는 사용하지 않았다.
- 사용자 선택에 따라 HaelDB의 **Male Grunt/Yelling sounds** 팩을 OpenGameArt에서 내려받았다. 페이지의 복수 라이선스 중 CC0 1.0을 선택했다.
- 파일 62개 중 같은 `3`번 접두사 그룹의 두 파일을 우선 후보로 사용했다. 직접 청취하지 못했으므로 음색과 피격·사망 용도의 최종 적합성은 사용자 확인이 필요하다.

| 용도 | 프로젝트 파일 | 원본 | 길이 |
|---|---|---|---|
| 피격 | `Assets/Game/Audio/Voice/Male/Male_Hit.wav` | `3grunt3.wav` | 0.5초 |
| 사망 | `Assets/Game/Audio/Voice/Male/Male_Death.wav` | `3yell9.wav` | 2초 |

- WAV는 원본 바이트 그대로 복사하고 이름만 바꿨다. Unity 임포트 설정은 Force To Mono, PCM, Decompress On Load다.
- 출처·선택한 라이선스·원본 파일 대응·청취 한계는 [SOURCE.md](../../Assets/Game/Audio/Voice/Male/SOURCE.md)에 남겼다.
- `PlayerHealth`의 기존 `hitClip`과 `deathClip` 슬롯을 사용했다. 기존 코드를 수정하거나 랜덤 음성 기능을 추가하지 않았다.
- 아래 네 씬의 실제 플레이어 `Character_Soldier`에 연결했다. `Woman Damage` / `Woman Die`를 `Male_Hit` / `Male_Death`로 교체했다.
  - `Assets/Game/Scenes/UrbanSurvival.unity`
  - `Assets/Game/Scenes/UrbanSurvival_ResultTest.unity`
  - `Assets/Game/Scenes/BossTestScene.unity`
  - `Assets/Game/Scenes/FinalBossTestScene.unity`
- 씬에는 기존 비교용 `Player Character`도 있다. 이름이 `Character_Soldier`인 플레이어만 바꿨다. `Assets/Scenes/Main.unity`는 수정하지 않았다.
- 네 씬을 다시 열어 저장된 음성 참조, AudioSource의 `SFX` 믹서 그룹, 미저장 변경 없음 상태를 확인했다. 원래 열려 있던 `FinalBossTestScene`으로 돌아갔다.

## Git 상태

- 음성 적용 커밋: **`d29867b` — `feat: 남성 캐릭터 피격·사망 음성 적용`**.
- GitHub `SideProject`에 푸시 완료. 이번 확인에서 HEAD와 origin/SideProject의 차이는 `0 / 0`이다.
- 음성·출처·메타 파일과 위 네 씬만 커밋에 포함했다.
- 아래 기존 변경은 작업 범위에서 제외하고 그대로 보존했다. 임의 커밋·복원·삭제하지 않는다.
  - `Assets/SideProjectAssets/Zombie Apocalypse Kit - March 2024/Materials/Headlights.mat` 수정
  - `ProjectSettings/ProjectSettings.asset` 수정
  - 루트 `AGENTS_20260928_1310_백업.md`, `AGENTS_20261001_1037_백업.md` 미추적 파일
- 새 인수인계 문서 변경은 음성 커밋 이후 작성한 것이므로 아직 커밋하지 않았다. 커밋 전 사용자 확인이 필요하다.

## Windows 빌드 결과

- Unity **6000.3.19f1**, StandaloneWindows64, **IL2CPP**.
- 실행 파일: `<프로젝트 루트>/Builds/VoiceTest_20261006/UrbanSurvival.exe`.
- BuildReport: **Succeeded**, 오류 **0**, 경고 **2**, 소요 **173.569초**(약 2분 54초).
- 보고서 전체 크기: **1,727,350,240바이트**. 실행 파일과 `GameAssembly.dll` 생성 확인.
- 경고: Unity Services의 프로젝트 ID 미연결, Pipeline 런타임 설정 에셋 없음. 빌드를 실패시키지 않았다.
- 빌드 대상은 현재 활성 목록을 사용했다: Intro → UrbanSurvival → Main → BossTestScene → FinalBossTestScene. ResultTest는 이 빌드에 포함되지 않는다.
- 빌드 파일은 Git 제외 경로에 있으며 커밋·푸시하지 않았다. 배포 시 `.exe`뿐 아니라 같은 폴더의 데이터와 DLL도 함께 필요하다.
- 빌드 성공까지만 확인했다. 이번 빌드 실행, 실제 음성 청취, 피격·사망 재생은 아직 확인하지 않았다.

## 다음 확인 순서

1. 위 실행 파일을 실행해 인트로에서 실제 게임으로 진입한다.
2. 피격 시 남성 신음, 사망 시 남성 음성이 재생되는지 확인한다. 사망 직전 피격음과 사망음의 중첩 및 결과 화면 전환 시 잘림도 확인한다.
3. 설정 창 SFX 볼륨을 낮추거나 0으로 바꿔 음량이 반영되는지 확인한다.
4. `SOURCE.md`와 두 WAV를 미리듣고 현재 Soldier 캐릭터의 음색에 맞는지 결정한다. 사망 후보는 원본 이름이 `yell`이므로 최종 사망음으로 적절한지 직접 판단한다.
5. 교체 요청이 있다면 같은 팩의 다른 후보를 비교한다. 현재 적용 파일이 확정된 최종 품질이라고 가정하지 않는다.
6. 사용자 확인 후 인수인계 문서 변경만 별도로 커밋한다. 자동 재저장된 외부 재질·설정 변경은 섞지 않는다.

## 재개 시 참고

- 프로젝트 경로는 PC마다 다르므로 현재 작업 디렉터리와 브랜치를 먼저 확인한다. 로컬 AGENTS.md와 현재 채팅의 최신 사용자 지침을 확인하며, 서로 다르면 최신 사용자 지침을 우선한다. 과거 5웨이브·3무기 계획을 현재 게임 규칙으로 사용하지 않는다.
- 이 세션에서는 Unity MCP 도구가 없었으며 Unity CLI로 실행 중인 에디터를 조작했다. CLI 위치: `C:/Program Files/Unity Hub/resources/cli/unity.exe`. 에디터 지정은 `--project-path <프로젝트 루트>`.
- 샌드박스 안의 CLI는 에디터가 없다고 보고했지만 권한을 허용한 상태에서는 연결되었다. 연결 실패를 에디터 미실행으로 단정하지 않는다.
- 빌드 명령은 `command build --target StandaloneWindows64 --outputPath <exe 경로> --confirm`이다. 최초 명령의 queued 응답은 완료가 아니다. `Temp/pipeline_build_status.json`의 `status=completed`, `result=Succeeded`를 확인한다.
- Unity 생성 `.meta`의 빈 YAML 값 뒤 공백으로 staged diff 검사에서 경고가 있었다. 출처 Markdown 공백 검사는 통과했으며 메타 공백을 불필요하게 수정하지 않았다.
- 작업 로그: `D:/Codex/Log/20261006_UrbanSurvival_남성음성적용_Log.md`, `20261006_UrbanSurvival_음성테스트빌드_Log.md`.

원본 페이지: https://opengameart.org/content/male-gruntyelling-sounds
