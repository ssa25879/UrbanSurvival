# Toon 캐릭터 Animator 설계 및 에셋 사용 결정

작성일: 2026-09-17. 상태: 설계 후 사용자 승인으로 첫 Animator 생성·연결 및 단독 평가 완료. 게임플레이 연동은 후속 작업.

## 1. 사용자 확정 사항과 우선순위

- `Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022`는 게임의 무기·캐릭터 에셋으로 사용한다.
- `Assets/SideProjectAssets/kenney_ui-pack`은 UI 에셋으로 사용한다.
- 이 결정은 이전 계획의 Universal Base Characters/Ultimate Guns Pack 사용 전제 및 대체 여부 미확인 기록보다 우선한다.
- 도시 맵 에셋 변경은 요청되지 않았으므로 별도로 바꾸지 않는다.
- 현재 씬에 올린 테스트 캐릭터의 Animator와 Animator Controller를 먼저 준비한다. 이 작업을 기존 P0/P1 앞부분의 첫 시각 검증 단위로 배치한다.
- SideProject만 사용한다. 기존 파일 수정 전 백업 여부 확인 규칙은 유지한다.

## 2. 실제 확인한 것과 확인하지 못한 것

### 로컬 자산

캐릭터 경로: `Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/Characters/FBX/`

| 모델 | FBX AnimationStack에서 확인한 주요 동작 |
|---|---|
| Character_Soldier.fbx | Death, Duck, HitReact, Idle, Idle_Shoot, Jump, Jump_Idle, Jump_Land, No, Punch, Run, Run_Gun, Wave, Yes |
| Character_Hazmat.fbx | 위 동작과 Run_Shoot, Walk, Walk_Shoot |
| Character_Enemy.fbx | Hazmat과 같은 주요 동작 이름. 접두사가 있는/없는 AnimationStack이 함께 있어 중복 목록 확인 필요 |

FBX 바이너리의 AnimationStack 노드를 읽어 확인했다. Unity에 임포트된 AnimationClip 목록·길이·곡선 바인딩·재생 성공 여부와 동일하다고 단정하지 않는다. 실제 임포트 이름에는 `CharacterArmature|` 접두사가 붙을 수 있다.

- 셋 모두 메타데이터에 `animationType: 2`, `human: []`, `importAnimation: 1`이 있다. Generic 임포트 구성을 출발점으로 삼되 실제 에디터 상태에서 확인한다.
- `clipAnimations: []`는 사용자 정의 분할 설정이 비어 있다는 관측이며 애니메이션이 없다는 뜻으로 해석하지 않는다.
- Soldier의 저장된 skeleton에는 CharacterArmature, Root, Hips, Torso, UpperArm/LowerArm, 손가락, UpperLeg/LowerLeg, Foot 등이 있다. Humanoid 유효 Avatar 여부는 확인하지 않았다.
- 무기 FBX에 AK, SMG, Shotgun이 있어 기획의 소총·SMG·산탄총 외형 후보로 사용한다. 총구 축·손잡이·크기·재질은 실제 연결 시 검사한다.
- Kenney 폴더에는 PNG, Vector, Font, Sounds가 있다. 기존 uGUI 구조를 유지하면서 실제 사용할 스프라이트만 선택한다.

### 현재 씬과 기존 구조

- 초기에는 Unity MCP 연결이 없었으나 조사 중 `Zombie@5fba521e` 연결이 활성화됐다. 프로젝트 경로와 Unity6000.3.19f1, 편집 모드·컴파일 중 아님을 확인했다.
- 저장된 `Assets/Scenes/Main.unity`에는 세 신규 캐릭터 이름 및 Soldier 모델 GUID 참조를 찾지 못했다. 다른 이름·다른 씬·미저장 상태인지 확인이 필요하다.
- 사용자가 알려준 `CharacterArmature`를 조회하여 실제 경로 `Character_Soldier/CharacterArmature`를 확인했다. Armature에는 Transform만 있고 부모 Character_Soldier에는 PlayerMovement/PlayerInput/PlayerHealth/PlayerShooter/Rigidbody/CapsuleCollider가 있다. 부모에도 Animator는 없다.
- Main 씬은 isDirty=true, buildIndex=-1이다. 미저장 상태를 보존했으며 씬 저장·재생·수정을 하지 않았다. 콘솔 error/warning 조회는0건이지만 Play Mode 정상 동작 검증을 의미하지 않는다.
- Character_Soldier의 PlayerShooter에서 gun/gunPivot/leftHandMount/rightHandMount가 모두 null이다. Animator만 추가해도 전투 가능 상태가 되는 것은 아니며, 기존 OnEnable의 gun 접근 전에 참조 연결 또는 테스트 컴포넌트 분리가 필요하다.
- Packages/manifest.json 및 packages-lock.json에 Unity MCP 패키지가 사용자 측에서 추가된 변경을 관측했다. 이 변경은 보존한다.
- 기존 `Assets/Animations/ShooterAnimator.controller`는 Move(float), Reload(trigger), Die(trigger), Base Movement/Upper Body 레이어와 상체 마스크/IK Pass를 사용한다.
- 기존 Humanoid Idle/Walk/Run, Aim Idle, Reload, Human Death 클립은 보존한다. 새로운 Generic 캐릭터에서 그대로 재생되거나 기존 손 IK가 동작한다고 가정하지 않는다.
- 기존 PlayerMovement/PlayerShooter는 같은 오브젝트에서 Animator를 찾는다. 모델 자식에 Animator를 배치한다면 해당 참조와 IK 콜백 구조도 함께 조정해야 한다.

## 3. 접근 방식과 권장 설계

### 권장: 원본 Generic 동작으로 최소 컨트롤러 검증

모델 자체의 동작을 사용하여 대기→이동→사망부터 확인한다. 원본 FBX의 Rig 타입을 변경하지 않고 캐릭터별 실제 임포트 클립을 연결한다. 원본 에셋 수정 없이 별도의 프로젝트용 Animator Controller와 캐릭터 프리팹 변형본을 만든다.

장점은 현재 보유한 클립과 본 구조를 그대로 확인할 수 있고 기존 Humanoid 애니메이션 변환을 먼저 해결할 필요가 없다는 점이다. 재장전·방향별 보행 클립 부족은 별도 과제로 명시한다.

### 대안: Humanoid 전환 후 기존 애니메이션 재사용

필수 본 매핑과 유효 Avatar, 자세·스케일·손 위치 검증을 통과하면 후보가 된다. 현재 리그의 자동 매핑 성공을 보장할 수 없으므로 먼저 전환하지 않는다. 원본 대신 프로젝트용 복사본에서 시험하고 기존 손 IK를 포함해 검증해야 한다.

### 제외: 기존 ShooterAnimator를 무검증으로 복사 연결

기존 컨트롤러는 Humanoid 클립·마스크·IK와 결합되어 있다. 파라미터 이름이 같다는 이유만으로 새 Generic 모델과 호환된다고 볼 수 없어 초기 방식으로 선택하지 않는다.

공식 근거: [Generic 임포트](https://docs.unity3d.com/6000.0/Documentation/Manual/GenericAnimations.html), [Humanoid 리타기팅](https://docs.unity3d.com/6000.0/Documentation/Manual/Retargeting.html). Generic은 root node와 해당 계층의 동작을 확인하고, Humanoid 리타기팅은 설정된 Avatar가 전제다.

## 4. 첫 Animator Controller 구성

생성 파일: `Assets/Game/Animations/ToonPlayer.controller`.

첫 연결은 한 캐릭터에 한정한다. 모델별 본 경로가 같은지 검사하기 전 다른 캐릭터에 클립을 공유하지 않는다. 두 번째 캐릭터는 실제 경로가 호환될 때만 같은 상태 구조 및 Override Controller를 검토한다.

| 항목 | 첫 검증 단계 설계 |
|---|---|
| 레이어 | Base Layer 1개 |
| 기본 상태 | Locomotion |
| Locomotion | 1D Blend Tree: Speed=0은 해당 모델의 Idle, Speed=1은 Run_Gun |
| 파라미터 | Speed(float, 기본0), Die(trigger) |
| 사망 | Any State→Death, Die 트리거, Exit Time 없이 전환, 자기 자신으로 재전환 금지 |
| 사망 종료 | Death에서 자동 복귀 없음. 새로운 판의 초기화 시에만 Locomotion 복귀 |
| 전환 시간 | 사망0.1초를 시작값으로 프리뷰에서 확인 |
| 루프 | Idle/Run_Gun 반복, Death 비반복. 실제 임포트 설정과 마지막 자세 유지 확인 |
| Root Motion | 끔. 실제 이동은 Rigidbody가 소유 |
| IK | 첫 Generic 검증에서는 Humanoid IK Pass/기존 손 IK를 연결하지 않음 |

Idle이 비무장 자세일 수 있으므로 먼저 프리뷰한다. `Idle_Shoot`를 조준 대기로 쓸지 여부는 반복 사격 모션/반동 포함 여부를 확인한 뒤 결정한다. 이름만 보고 조준 정지 포즈로 채택하지 않는다.

초기 Speed는 이동 방향과 무관한 실제 수평 속도/최대 이동속도(0~1)로 정의한다. 기존 코드의 Move 파라미터를 연결 없이 재사용한다고 가정하지 않는다. 컨트롤러 단독 검증은 Animator 파라미터를 수동으로 변경하고, 후속 코드 연결에서 실제 속도를 전달한다.

## 5. 후속 애니메이션과 코드 연결

### 게임 규칙은 애니메이션이 결정하지 않는다

- 발사 성공·탄약·재장전 완료·사망 여부는 기존 게임 로직이 결정한다.
- 사격 모션은 Gun의 실제 발사 승인 결과에 반응한다. 빈 탄창에서 FireHeld만으로 사격 모션을 반복하지 않는다.
- 재장전 시간은 무기 기본 시간/캐릭터 배율이며 애니메이션 이벤트로 탄약을 이전하지 않는다.
- 재장전 클립을 확보하면 클립 길이/실제 재장전 시간으로 상태 재생 속도를 맞추되 UI·타이머가 게임 판정을 소유한다.

### 클립 부족 처리

- 조사한 FBX AnimationStack에 Reload 이름은 없다. 다른 이름의 실제 동작을 프리뷰하기 전 완전한 부재라고 확정하지 않는다.
- 재장전 전용 동작이 없다면 첫 프로토타입은 이동·조준 자세와 재장전 HUD를 유지한다. 다른 동작을 재장전이라고 표시하지 않는다.
- 좌/우 스트레이프 및 후진 전용 이름도 확인하지 못했다. 첫 프로토타입은 실제 이동과 조준을 정상 구현하고 전방 Run_Gun으로 외형만 임시 검증한다. 옆/뒤 이동의 발 미끄러짐은 알려진 시각 한계로 기록한다.
- 추가 클립 확보 또는 리타기팅 후에 MoveX/MoveZ 2D Blend Tree를 도입한다. 없는 방향 클립을 임의로 연결해 완성 처리하지 않는다.
- HitReact를 추가하더라도 이동·사격·재장전 중단 여부는 기획 규칙을 유지한다.
- 상체 사격/재장전 레이어는 Generic Transform 마스크의 본 경로와 원본 클립 곡선을 확인한 다음 추가한다. 이동 루트·다리를 덮어쓰지 않는지 검증한다.

### 테스트 캐릭터 연결 구조

- 씬에 올린 대상의 기존 부모·컴포넌트·Animator 유무를 먼저 검사한다. 불필요한 중복 Animator를 생성하지 않는다.
- 첫 테스트는 기존 컴포넌트가 있는 Character_Soldier 루트에 Animator를 배치하는 안을 우선한다. FBX의 클립 바인딩이 이 루트 아래 CharacterArmature 경로와 일치하는지 확인한다. CharacterArmature 자식에 중복 Animator를 붙이지 않는다.
- 최종 PlayerRoot/모델 자식 분리는 후속 공통 캐릭터 구조 단계에서 검토한다. 지금 단독 애니메이션 검증에 불필요한 계층 재구성을 포함하지 않는다.
- 실제 코드 연결 단계에서는 PlayerMovement/PlayerShooter/PlayerHealth가 사용할 Animator 참조를 명시적으로 지정한다.
- Generic 상태에서는 무기 소켓과 모델 자체 자세로 손·총 정렬을 먼저 맞춘다. 기존 OnAnimatorIK를 무조건 옮기거나 켜지 않는다.
- 테스트용 모델을 곧바로 최종 플레이어로 교체하지 않는다. 대기·이동·사망 검증 후 기존 플레이어 로직에 연결한다.

## 6. 작업 순서와 완료 기준

1. 대상 식별: Hierarchy 이름·씬·모델 경로·미저장 변경·Animator 유무 확인.
2. 클립 검증: 임포트된 AnimationClip 목록과 프리뷰, 길이, 반복, root node, 루트 이동, 경로 일치 확인.
3. 컨트롤러 생성: 새 경로에 Locomotion/Death와 Speed/Die 구성. 원본 컨트롤러는 유지.
4. 연결: 사용자 백업 결정 후 대상 Animator에 연결. 기존 컴포넌트가 있으면 수정 범위를 명시.
5. 단독 재생 검수: Speed0/0.5/1, Death1회, 사망 후 자동 복귀 없음, 루트 위치 불변, 콘솔 경고/오류 확인.
6. 이동 연동: P1 입력·카메라 조준과 실제 이동속도를 연결. 이동 방향과 조준 방향이 달라도 모델 회전/위치 소유권이 충돌하지 않는지 확인.
7. 전투 연동: 실제 발사·재장전·피격·사망 사건을 연결. 없는 애니메이션은 미완료 시각 항목으로 별도 기록.
8. UI 적용: Kenney PNG 중 필요한 버튼·패널·바를 기존 uGUI에 사용. 테스트용 Animator 확인에 전체 HUD 개편을 묶지 않는다.

단독 검수 통과는 Animator 기반 준비 완료일 뿐, 이동/전투/재장전 애니메이션 전체 완료가 아니다. 코드 연결은 행동 기반 테스트, 애니메이션 품질은 실제 재생·화면 검수를 병행한다.

## 7. 수정 및 승인 경계

- 설계 제시 후 사용자가 ‘백업 없이 생성·연결’을 승인했다. 이에 따라 아래8절의 Animator 자산 생성·씬 연결까지 수행했다. 게임 코드와 원본 FBX 메타데이터는 변경하지 않았다.
- 대상은 확인된 `Character_Soldier/CharacterArmature`이며 부모 Character_Soldier에 Animator를 연결했다. 클립 재생과 루트 위치 유지 검증 결과는 8절에 기록했다.
- 기존 AGENTS.md와 이전 설계 기록의 에셋 문구를 갱신하거나 기존 씬·메타데이터를 수정할 때는 사용자 백업 방식을 먼저 확인한다.
- 새로운 사용자 에셋 결정은 현재 문서에 먼저 기록했다. 과거 문서의 원래3종 에셋 전제와 충돌하면 이 문서1절과 최신 사용자 지시를 우선한다.

## 8. 실제 생성 결과와 검증

- 새 `Assets/Game/Animations/ToonPlayer.controller`: Speed(float), Die(trigger), Locomotion 1D Blend Tree, Any State→Death(0.1초, Exit Time 없음, 자기 자신으로 전환 금지).
- 새 `Assets/Game/Animations/ToonSoldier/Idle.anim`, `Run_Gun.anim`, `Death.anim`: FBX의 실제 임포트 클립을 복제했다. 원본은 비반복이므로 Idle/Run_Gun 복사본만 Loop Time을 켰고 Death는 비반복으로 유지했다.
- Character_Soldier 루트에 Animator를 추가하고 유효한 Generic Character_SoldierAvatar와 컨트롤러를 연결했다. Apply Root Motion=false.
- `Assets/Scenes/Main.unity`에 현재 에디터 상태를 저장했다. 따라서 씬 diff에는 작업 전 사용자가 배치한 미저장 캐릭터·컴포넌트 상태도 함께 포함된다. 기존 사용자 배치를 원복하거나 재구성하지 않았다.
- 별도 Preview Scene의 모델 복제본과 수동 PlayableGraph로 평가했다. 원래 씬의 게임플레이 스크립트는 실행하지 않았다.
- Speed0: Idle 가중치1, 움직인 본8개. Speed0.5: Idle/Run_Gun 각각0.5, 움직인 본15개. Speed1: Run_Gun 가중치1, 움직인 본15개.
- 세 경우 모두 루트 오브젝트 위치 변화0. Die 트리거 후 Death 진입, 클립 종료 이후 Death 유지, loop=false를 확인했다.
- 최초 프리뷰 확인은 본 회전만 관측하여 실패했으며, 프리뷰 Animator를 AlwaysAnimate로 설정하고 본 위치·회전을 함께 관측하여 재검증했다. 최초 실패를 통과로 처리하지 않았다.
- 연결 후 콘솔 error/warning 조회0건. 전체 Play Mode, 실제 입력에 따른 Speed 갱신, 사격·재장전 연결은 검증하지 않았다.
- Unity 씬 저장이 만든 빈 YAML 값 뒤 공백 때문에 git diff --check는 Main.unity의 trailing whitespace를 보고했다. 게임 런타임 오류와 구분하며 관련 없는 씬 전체 공백 정리는 수행하지 않았다.

## 9. 다음 실행 작업

1. 현재 PlayerMovement가 쓰는 `Move`와 새 컨트롤러의 `Speed`를 연결한다. 실제 수평 속도 정규화 기준으로 바꾸는 작업은 P1 이동 구현과 함께 진행한다.
2. PlayerShooter의 비어 있는 gun/gunPivot/손 소켓 참조를 Toon 무기와 연결한다. Generic 모델에서 기존 Humanoid IK를 무조건 실행하지 않게 호출 구조를 정리한다.
3. 현재 컨트롤러에는 Reload가 없으므로 기존 Shooter의 Reload 트리거를 바로 연결하지 않는다. 실제 클립 확보와 타이머 동기화 방향을 확정한다.
4. 기존 플레이어와 테스트 캐릭터가 모두 있는 씬에서 GameManager의 플레이어 대상 선택을 명시한다. FindObjectOfType 결과에 의존하여 테스트 캐릭터가 자동 선택될 것이라 가정하지 않는다.
5. 이후 실제 Play Mode에서 카메라 기준 이동·독립 조준·전투를 통합 검증한다.

## 10. 최종 확인

- 별도 읽기 전용 검토에서 컨트롤러 상태·파라미터·루프·씬 참조가 요구사항과 일치함을 확인했다.
- 대상 Animator 1개, 유효한 Generic Avatar, 클립 Transform 바인딩 누락 0개, 저장 후 sceneDirty=false를 확인했다.
- Move/Speed 불일치와 무기 참조 연결은 후속 작업이며 이번 완료 범위에 포함하지 않는다.
