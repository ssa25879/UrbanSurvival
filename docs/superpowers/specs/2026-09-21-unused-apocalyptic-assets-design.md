# Apocalyptic World 미사용 에셋 분리 설계

> 2026-09-22 기준 변경: 이 문서는 과거 Apocalyptic_World 정리 작업의 기록이다. 이후 환경 제작에는 Toon Shooter Game Kit의 Environment를 사용한다. 아래 정리 범위를 현재 환경 에셋 선택이나 추가 삭제 승인으로 해석하지 않는다.

## 목적

`Assets/SideProjectAssets/Apocalyptic_World` 묶음에서 `Assets/Scenes/Main.unity`가 실제로 사용하는 에셋과 그 재귀 의존성만 Unity 프로젝트에 남긴다. 사용하지 않는 에셋은 Git에 올리지 않고 `D:\2026 윤우상\TopViewSurvivalAsset\미사용`으로 이동해 보관한다.

## 확정 조건

- 작업 브랜치는 `SideProject`만 사용한다.
- 사용 여부의 기준 씬은 `Assets/Scenes/Main.unity`이다.
- `InGameScene.unity`와 해당 `.meta`는 이미 삭제된 상태를 유지하고 Git 업로드 대상에서 제외한다.
- 정리 범위는 `Assets/SideProjectAssets/Apocalyptic_World` 묶음으로 한정한다.
- 기존 게임 코드, UI, 오디오, 기존 캐릭터 에셋 등 범위 밖 파일은 정리하지 않는다.
- 백업은 생성하지 않는다.
- 미사용 에셋은 원본과 `.meta`를 함께 이동한다.
- 보관 위치는 `D:\2026 윤우상\TopViewSurvivalAsset\미사용`이다.
- 외부 보관 폴더에서는 프로젝트 내부의 상대 경로를 유지한다.
- 외부 보관 폴더의 파일은 Git에 추가하지 않는다.

## 사용 에셋 판정

1. `Main.unity`에 기록된 GUID를 시작점으로 삼는다.
2. 각 GUID에 대응하는 에셋을 찾는다.
3. 해당 에셋 본문과 `.meta`에 기록된 GUID를 재귀적으로 추적한다.
4. 모델, 프리팹, 머티리얼, 텍스처, 애니메이션, 컨트롤러, 셰이더 및 importer 외부 참조가 더 이상 나오지 않을 때까지 반복한다.
5. 재귀 참조 집합에 포함된 `Apocalyptic_World` 파일과 `.meta`만 사용 중으로 판정한다.
6. Unity 폴더가 일부라도 유지되는 경우 그 폴더의 `.meta`도 유지한다.
7. 참조 집합에 포함되지 않은 나머지 파일은 미사용으로 판정한다.

정적 GUID 분석 결과가 불완전할 수 있으므로 최종 판정은 Unity `AssetDatabase.GetDependencies` 결과와 비교한다. Unity Editor를 연결할 수 없는 상태에서는 실제 이동을 시작하지 않는다.

## 이동 구조

예를 들어 다음 파일이 미사용이면:

`Assets/SideProjectAssets/Apocalyptic_World/Art/Textures/Example.bmp`

아래 위치로 이동한다:

`D:\2026 윤우상\TopViewSurvivalAsset\미사용\Assets\SideProjectAssets\Apocalyptic_World\Art\Textures\Example.bmp`

`Example.bmp.meta`도 같은 목적지 폴더로 이동한다. 목적지에 같은 상대 경로의 파일이 이미 존재하면 덮어쓰지 않는다. 내용이 같으면 기존 파일을 보존하고, 내용이 다르면 작업을 중단해 충돌 파일을 보고한다.

## 안전 절차

1. 이동 전 현재 Git 상태와 `SideProject` 브랜치를 다시 확인한다.
2. 정적 GUID 의존성과 Unity 의존성 결과를 비교해 유지 목록을 만든다.
3. 이동 예정 목록을 파일 수와 총 용량을 포함한 보고서로 먼저 생성한다.
4. 목적지 충돌을 검사한다.
5. 파일과 `.meta`를 상대 경로 그대로 이동한다.
6. 원본과 목적지의 파일 수 및 크기를 비교한다.
7. 이동 후 빈 폴더와 대응 폴더 `.meta`를 함께 정리한다.
8. Git 스테이징을 재구성해 실제 유지 파일과 삭제 상태만 반영한다.

## 검증

- `Main.unity`의 모든 GUID가 유효한 에셋 또는 Unity 기본 리소스를 가리키는지 확인한다.
- Unity Console에 컴파일 오류와 Missing 참조 오류가 없는지 확인한다.
- `Main`을 열어 `Character_Soldier`, 카메라, 주요 프리팹과 머티리얼이 정상 표시되는지 확인한다.
- Play Mode에서 기본 이동과 카메라 동작을 확인한다.
- `InGameScene`이 Git 업로드 목록에 없는지 확인한다.
- 외부 미사용 폴더의 파일 수·용량이 이동 목록과 일치하는지 확인한다.
- Git 커밋 직전 100MB 이상 파일과 전체 업로드 크기를 다시 확인한다.

## Git 반영

- 정리 결과는 `SideProject` 브랜치에만 커밋한다.
- 미사용 외부 보관 폴더는 저장소 밖에 있으므로 Git에 포함하지 않는다.
- 대량 에셋 이동과 프로젝트 설정 변경을 섞지 않는다.
- 검증이 끝나기 전에는 원격 저장소로 푸시하지 않는다.

## 완료 조건

- `Apocalyptic_World` 안에는 `Main`에서 실제 사용하는 파일과 필요한 폴더 `.meta`만 남는다.
- 모든 미사용 파일과 `.meta`가 지정된 외부 폴더에 상대 경로를 유지한 채 존재한다.
- `Main`에 Missing 참조 및 컴파일 오류가 없다.
- 정리된 사용 에셋만 `SideProject` 브랜치의 업로드 대상이 된다.
