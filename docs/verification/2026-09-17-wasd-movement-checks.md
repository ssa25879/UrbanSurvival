# WASD 이동 검증 — 2026-09-17

- 복구 기준 커밋: a3ce6d9 (SideProject). 구현 후 커밋·푸시는 수행하지 않았다.
- 변경 파일: Assets/Scripts/PlayerMovement.cs. 기존 입력축과 Animator 첫 파라미터 선택을 유지한다.
- 카메라 기준 평면 이동, 대각선 속도 제한, 회전 불변, 좌우 이동의 Locomotion 전달을 구현했다.
- 구현 전 방향 계산 누락으로 실패했고, 구현 후 방향 계산 240개 및 카메라 없음 대체 방향 검증을 통과했다.
- 독립 Preview Scene에서 Rigidbody 이동·회전 불변·Animator 값 전달 6개 검증을 통과했다.
- 최초 물리 검증은 에디터에서 Play Mode 전용 CreateScene을 호출하여 실패했다. 격리된 물리 공간을 가진 Preview Scene으로 바꿨다. 이후 모델에 이미 있는 Animator를 중복 추가하려던 검증 코드도 수정했다. 두 문제는 테스트 환경의 문제이며 이동 코드 수정은 필요하지 않았다.
- 실제 키보드 입력 및 전체 Main 씬 Play Mode는 사용자 화면에서 추가 확인해야 한다. 마우스 조준은 이번 범위 밖이다.

아래 코드는 Unity MCP execute_code의 메서드 본문으로 실행한다. 검증용 오브젝트는 finally에서 정리한다.

## 방향 계산

```csharp
var method = typeof(PlayerMovement).GetMethod("GetMoveDirection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
if (method == null) return "FAIL: camera-relative WASD movement calculation is missing";
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try {
 var go = new GameObject("WASD verification");
 UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
 go.AddComponent<Rigidbody>();
 var input = go.AddComponent<PlayerInput>();
 var movement = go.AddComponent<PlayerMovement>();
 var cam = new GameObject("Verification camera");
 UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam, scene);
 typeof(PlayerMovement).GetField("playerInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(movement, input);
 typeof(PlayerMovement).GetField("movementCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(movement, cam.transform);
 var cases = new Vector2[] { Vector2.zero, Vector2.up, Vector2.down, Vector2.left, Vector2.right, Vector2.one, new Vector2(-1,1), new Vector2(1,-1), -Vector2.one, new Vector2(0.3f,0.4f) };
 int passed = 0;
 foreach (float yaw in new float[] {0,45,90,180}) {
  foreach (float pitch in new float[] {0,60,90}) {
   cam.transform.rotation = Quaternion.Euler(pitch,yaw,0);
   foreach (var axes in cases) {
    typeof(PlayerInput).GetProperty("rotate").SetValue(input, axes.x, null);
    typeof(PlayerInput).GetProperty("move").SetValue(input, axes.y, null);
    var expectedInput = Vector2.ClampMagnitude(axes,1);
    var expected = Quaternion.Euler(0,yaw,0) * new Vector3(expectedInput.x,0,expectedInput.y);
    foreach (float facing in new float[] {0,123}) {
     go.transform.rotation = Quaternion.Euler(0,facing,0);
     var actual = (Vector3)method.Invoke(movement,null);
     if (Vector3.Distance(actual,expected)>0.0001f) return "FAIL: yaw="+yaw+" pitch="+pitch+" input="+axes+" facing="+facing+" actual="+actual+" expected="+expected;
     passed++;
    }
   }
  }
 }
 typeof(PlayerMovement).GetField("movementCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(movement,null);
 typeof(PlayerInput).GetProperty("rotate").SetValue(input,1f,null);
 typeof(PlayerInput).GetProperty("move").SetValue(input,0f,null);
 if (Vector3.Distance((Vector3)method.Invoke(movement,null),Vector3.right)>0.0001f) return "FAIL: no-camera fallback";
 return "PASS: "+passed+" camera/input/facing cases; no-camera fallback";
} finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
```

## Rigidbody와 Locomotion

```csharp
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try {
 var model = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/Characters/FBX/Character_Soldier.fbx");
 var go = UnityEngine.Object.Instantiate(model);
 UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
 var body = go.AddComponent<Rigidbody>(); body.useGravity=false;
 var input = go.AddComponent<PlayerInput>();
 var animator = go.GetComponent<Animator>();
 animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Game/Animations/ToonPlayer.controller");
 animator.applyRootMotion=false; animator.Rebind(); animator.Update(0);
 var movement = go.AddComponent<PlayerMovement>();
 var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
 typeof(PlayerMovement).GetMethod("Start",flags).Invoke(movement,null);
 var cam = new GameObject("Test camera");
 UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam,scene);
 cam.transform.rotation = Quaternion.Euler(60,45,0);
 typeof(PlayerMovement).GetField("movementCamera",flags).SetValue(movement,cam.transform);
 int passed=0;
 foreach(var axes in new Vector2[] {Vector2.zero,Vector2.up,Vector2.down,Vector2.left,Vector2.right,Vector2.one}) {
  body.position=Vector3.zero; body.rotation=Quaternion.Euler(0,123,0); body.linearVelocity=Vector3.zero;
  Physics.SyncTransforms();
  typeof(PlayerInput).GetProperty("rotate").SetValue(input,axes.x,null);
  typeof(PlayerInput).GetProperty("move").SetValue(input,axes.y,null);
  var before = body.rotation;
  typeof(PlayerMovement).GetMethod("FixedUpdate",flags).Invoke(movement,null);
  scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
  var clamped=Vector2.ClampMagnitude(axes,1);
  var expected=Quaternion.Euler(0,45,0)*new Vector3(clamped.x,0,clamped.y)*movement.moveSpeed*Time.fixedDeltaTime;
  if(Vector3.Distance(body.position,expected)>0.0001f) return "FAIL: Rigidbody input="+axes+" actual="+body.position+" expected="+expected;
  if(Quaternion.Angle(before,body.rotation)>0.001f) return "FAIL: WASD rotated character";
  if(Mathf.Abs(animator.GetFloat(animator.parameters[0].name)-clamped.magnitude)>0.0001f) return "FAIL: Locomotion value";
  passed++;
 }
 return "PASS: "+passed+" Rigidbody movement/rotation/Locomotion cases";
} finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
```

