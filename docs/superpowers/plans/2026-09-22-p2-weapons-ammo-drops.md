# P2: 무기 4종·탄약 상태·처치 드랍 시스템 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task (recommended for this plan — see rationale at the end). Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기획서 P2(소총과 탄약 상태) 요구사항 + 2026-09-22 기획 개정(무기 4종·드랍 테이블)을 코드에 반영한다. 권총(기본, 예비탄 무제한)·소총·SMG·산탄총 4종을 GunData 기반으로 구현하고, 좀비 처치 시 드랍 테이블(소총10%/SMG15%/산탄총15%/탄약20%/회복20%/꽝20%)로 무기 슬롯 해금·탄약 보급이 이루어지도록 한다.

**Architecture:** 기존에 씬에 배치된 물리 총 오브젝트는 `Character_Soldier` 밑의 "AK" 하나뿐이다(비주얼 리깅·IK 타깃이 이 오브젝트에 종속). 이번 작업은 **이 단일 Gun 오브젝트를 그대로 재사용**하고, `PlayerShooter`가 4개 슬롯(권총/소총/SMG/산탄총)의 `GunData`와 탄약 상태(탄창/예비탄)를 배열로 보관하다가 스왑 시점에 `Gun.ConfigureSlot()`으로 밀어넣는 방식으로 구현한다. 무기별 손에 든 모델 교체(비주얼)는 이번 범위에서 제외하고 후속 아트 작업으로 넘긴다 — 드랍된 픽업 아이템의 월드 비주얼만 무기별 FBX(Pistol/SMG/Shotgun)를 사용한다.

작업 중 **좀비 AI가 플레이어를 전혀 인식하지 못하는 기존 버그**(Character_Soldier가 `Default` 레이어인데 `Zombie.whatIsTarget`은 `Player` 레이어를 찾음 — OverlapSphere 실측으로 확인됨)를 함께 고친다. 이 수정 없이는 실제 Play Mode에서 좀비가 플레이어를 쫓아오지 않아 무기 시스템을 제대로 검증할 수 없다.

총구 장애물 판정을 위해 새 Unity 레이어 `Environment`를 만들어 환경 소품/그레이박스 맵을 여기로 옮긴다(좀비는 계속 `Default`에 남겨 총구 근접 오검출을 피함).

**Tech Stack:** Unity 6000.3.19f1, C# (기존 스크립트는 CodeDom 대상이 아니라 일반 Unity 컴파일 대상이므로 최신 C# 문법 사용 가능 — 단, Unity MCP `execute_code`로 즉석 검증할 때만 CodeDom 제약이 적용됨), Unity MCP(`execute_code`/`manage_scriptable_object`/`manage_editor`/`manage_gameobject`) 사용.

**Spec:** `AGENTS.md`(핵심 게임 규칙, 무기 4종 표, 드랍 테이블), `WorkNotes/이어서_작업_프롬프트.md`(P2 우선순위), 기획서 `docs/superpowers/specs/2026-09-17-urban-survival-development-design.md`(P2 절, F01~F16/I01~I09 — 이번 세션에서 전수 대조는 못했음을 유의)

## Global Constraints

- 작업 브랜치는 `SideProject`만 사용, `main` 수정 금지.
- 응답·로그는 한국어로 작성.
- 요청 범위만 구현, 기존 스타일과 구조 유지, 무관한 리팩터링 금지.
- 검증 없이 완료 선언 금지 — 각 태스크는 Unity MCP `read_console`로 오류 0건을 확인해야 완료로 간주.
- 원본 에셋(FBX 등)은 직접 수정하지 않는다.
- 재장전 시간(2.0/2.3/2.1/2.7초), 사거리(15/30/20/12m), 산포 반각(3/3/5/10도), 탄창(10/30/40/8), 예비탄 초기/상한(무제한, 150/240, 200/320, 40/64)은 `AGENTS.md`에 2026-09-22 확정된 값이므로 임의 변경 금지.
- 드랍 확률(소총10%/SMG15%/산탄총15%/탄약20%/회복상자20%/꽝20%)도 확정값.
- Play Mode 중 수정한 내용은 종료 시 되돌아가므로, 최종 반영은 반드시 Edit Mode에서 저장한다.
- `localRotation`을 통째로 대입하지 말 것(이 프로젝트 환경 소품 FBX는 루트에 보정 회전이 baked됨) — 이번 작업은 배치 스크립트를 다시 쓰지 않으므로 해당 없음이지만, 드랍 픽업 프리팹 생성 시 무기 FBX를 회전시켜야 한다면 동일하게 주의.

## Review Focus

- 탄창이 이미 가득 찬 상태에서 R을 눌러도 재장전 코루틴이 시작되지 않아야 한다(탄약 낭비/애니메이션 오재생 방지) — Task 3에서 `Reload()` 가드 테스트.
- 예비탄이 0인 무기(권총 제외)에서 재장전을 시도하면 즉시 거부되어야 한다(무한 대기 없음) — Task 3.
- 권총(예비탄 무제한, `ammoRemain = -1`)은 재장전을 아무리 반복해도 `ammoRemain`이 음수 상태를 유지해야 한다(다른 무기와 동일한 감소 로직을 타면 즉시 0 이하로 깨짐) — Task 3.
- 재장전 중(`State.Reloading`)에 1/2/3/4 키로 무기를 바꾸면 진행 중이던 재장전 코루틴이 새 무기의 탄약 상태를 덮어써 버그가 나면 안 된다 — Task 5에서 스왑 차단 테스트.
- 좀비의 공격 판정용 트리거 콜라이더(`isTrigger=true`인 BoxCollider)가 총알 레이캐스트에 맞아 처리되거나 총구 장애물로 오인되면 안 된다 — Task 3에서 `QueryTriggerInteraction.Ignore` 적용 테스트.

---

## Task 0: 발견된 버그 수정 — 좀비 AI 타겟팅 레이어 불일치 + Environment 레이어 신설

**배경(재현 결과):** Unity MCP로 직접 확인한 결과 `Character_Soldier`(플레이어 루트)는 `Default`(0) 레이어인데, `Zombie.cs`의 `whatIsTarget` 필드는 `Player`(9) 레이어만 가리키는 마스크(값 512)로 설정되어 있다. `Physics.OverlapSphere(플레이어 위치, 20f, 512)`를 실제로 실행하면 히트 0건, 반면 전체 레이어로 검사하면 플레이어 콜라이더가 잡힌다. 즉 **현재 상태로는 좀비가 플레이어를 절대 인식·추적하지 못한다.** `Player` 레이어(9)는 프로젝트에 이미 존재하지만 아무 오브젝트에도 배정되어 있지 않았다 — Character_Soldier 모델 교체 과정에서 누락된 것으로 보인다(이 세션에서 이미 겪은 AudioSource/LineRenderer 누락과 같은 패턴).

물리 충돌 매트릭스(`Physics.GetIgnoreLayerCollision(9,0)` = false)와 카메라 컬링 마스크(전체 레이어 렌더링)는 문제 없음을 확인했으므로 레이어만 바꿔도 안전하다.

같은 태스크에서 총구 장애물 판정에 쓸 새 레이어 `Environment`도 만든다. 좀비(`Default`)와 환경 소품을 레이어로 분리해 두면, 총구가 벽에 파묻혔을 때만 발사를 막고 근접한 좀비 때문에 오검출되는 일을 피할 수 있다.

**Files:**
- Modify: 씬 오브젝트 `Character_Soldier`의 레이어(스크립트 아님, `Assets/Game/Scenes/UrbanSurvival.unity` 저장으로 반영)
- Modify: 씬 오브젝트 `Environment Props`의 67개 자식 및 `Greybox Map`의 레이어
- Unity 프로젝트 레이어 테이블(`ProjectSettings/TagManager.asset`, `manage_editor add_layer`로 갱신)

**Interfaces:**
- Produces: `LayerMask.NameToLayer("Player")`, `LayerMask.NameToLayer("Environment")`가 이후 태스크(Gun.cs)에서 사용하는 상수 이름.

- [ ] **Step 1: `Environment` 레이어 추가**

`mcp__UnityMCP__manage_editor`를 `action: "add_layer"`, `layer_name: "Environment"`로 호출한다.

- [ ] **Step 2: 레이어 배정 및 검증 (execute_code)**

```csharp
var sb = new System.Text.StringBuilder();

var cs = GameObject.Find("Character_Soldier");
cs.layer = LayerMask.NameToLayer("Player");
sb.AppendLine("Character_Soldier layer -> " + LayerMask.LayerToName(cs.layer));

int envLayer = LayerMask.NameToLayer("Environment");
var propsParent = GameObject.Find("Environment Props");
int propCount = 0;
foreach (Transform child in propsParent.transform) {
    child.gameObject.layer = envLayer;
    propCount++;
}
sb.AppendLine("Environment Props reassigned: " + propCount);

var greybox = GameObject.Find("Greybox Map");
int greyboxCount = 0;
if (greybox != null) {
    // 바닥/벽 등 자식 전체와 자신을 Environment로
    greybox.layer = envLayer;
    foreach (Transform child in greybox.transform) {
        child.gameObject.layer = envLayer;
        greyboxCount++;
    }
}
sb.AppendLine("Greybox Map children reassigned: " + greyboxCount);

// 재확인: 좀비 타겟팅이 이제 플레이어를 찾는지
int mask = 1 << LayerMask.NameToLayer("Player");
var hits = Physics.OverlapSphere(cs.transform.position, 20f, mask);
sb.AppendLine("OverlapSphere(Player mask) hits after fix: " + hits.Length);

EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
return sb.ToString();
```

Expected: `Character_Soldier layer -> Player`, `OverlapSphere(Player mask) hits after fix: 1`(이상, 최소 1건).

- [ ] **Step 3: NavMesh·물리 회귀 확인**

```csharp
var sb = new System.Text.StringBuilder();
// 환경 소품이 여전히 콜라이더로 막고 있는지(물리 충돌 매트릭스 재확인)
int envLayer = LayerMask.NameToLayer("Environment");
int playerLayer = LayerMask.NameToLayer("Player");
sb.AppendLine("Ignore(Environment,Player)=" + Physics.GetIgnoreLayerCollision(envLayer, playerLayer));
sb.AppendLine("Ignore(Environment,Default)=" + Physics.GetIgnoreLayerCollision(envLayer, 0));
return sb.ToString();
```

Expected: 둘 다 `False`(충돌 유지). `True`가 나오면 Project Settings > Physics의 Layer Collision Matrix에서 해당 조합을 켜야 한다(이 프로젝트에서 아직 아무도 끈 적이 없으므로 기본값 False가 정상 기대치).

- [ ] **Step 4: Console 확인 및 씬 저장**

`read_console`로 오류 0건 확인 후 `manage_scene`(save)로 `UrbanSurvival.unity` 저장.

- [ ] **Step 5: 커밋**

```bash
git add Assets/Game/Scenes/UrbanSurvival.unity ProjectSettings/TagManager.asset
git commit -m "fix: 좀비 AI 타겟팅 레이어 불일치 수정, Environment 레이어 신설"
```

---

## Task 1: GunData 확장 — 무기별 발사 모드·사거리·산포·펠릿·예비탄 상한 필드 추가

**Files:**
- Modify: `Assets/Scripts/GunData.cs`

**Interfaces:**
- Produces: `GunData.FireMode`(enum), `GunData.fireMode`, `GunData.range`, `GunData.spreadHalfAngle`, `GunData.pelletsPerShot`, `GunData.reserveAmmoCap` — Task 2(애셋 값 채우기), Task 3(Gun.cs 로직), Task 5(PlayerShooter)에서 사용.

- [ ] **Step 1: `GunData.cs` 전체 교체**

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable/GunData", fileName = "Gun Data")]

public class GunData : ScriptableObject
{
    // 입력을 누르고 있으면 연사할지(소총·SMG), 눌렀다 뗄 때마다 1발만 나갈지(권총·산탄총)
    public enum FireMode {
        Manual,     // 단발 / 누를 때 1회
        Automatic   // 누르고 있으면 연사
    }

    public AudioClip shotClip; // 발사 소리
    public AudioClip reloadClip; // 재장전 소리

    public float damage = 25; // 공격력(산탄총은 "펠릿 1발당" 공격력)

    // 예비 탄약: -1은 무제한(권총 전용)을 의미한다
    public int startAmmoRemain = 100; // 처음에 주어질 전체 탄약
    public int reserveAmmoCap = 240; // 예비 탄약 상한(-1이면 무제한, 상한 없음)
    public int magCapacity = 25; // 탄창 용량

    public float timeBetFire = 0.12f; // 총알 발사 간격
    public float reloadTime = 1.8f; // 재장전 소요 시간

    public FireMode fireMode = FireMode.Manual; // 입력 모드
    public float range = 30f; // 사거리(m)
    public float spreadHalfAngle = 3f; // 산포 반각(도)
    public int pelletsPerShot = 1; // 1회 발사당 탄알 수(산탄총만 1보다 큼)
}
```

- [ ] **Step 2: 컴파일 확인**

`read_console`로 오류 0건 확인(기존 `Assets/ScriptableData/Gun Data.asset`이 새 필드의 기본값을 그대로 받는지도 함께 확인 — Unity가 자동으로 기본값 채움).

---

## Task 2: 무기 4종 GunData 애셋 생성/갱신

기존 `Assets/ScriptableData/Gun Data.asset`은 AK(라이플) 비주얼에 이미 연결되어 있으므로 이를 "소총" 데이터로 갱신하고, 권총/SMG/산탄총 3개를 신규 생성한다.

**Files:**
- Modify: `Assets/ScriptableData/Gun Data.asset` (소총으로 갱신)
- Create: `Assets/ScriptableData/Pistol Data.asset`
- Create: `Assets/ScriptableData/SMG Data.asset`
- Create: `Assets/ScriptableData/Shotgun Data.asset`

**Interfaces:**
- Consumes: `GunData`(Task 1에서 정의한 필드)
- Produces: 4개 GunData 애셋 경로 — Task 5(PlayerShooter.weaponData 배열), Task 6(WeaponPickup 프리팹)에서 참조.

- [ ] **Step 1: 기존 애셋을 소총 값으로 갱신 (execute_code)**

```csharp
var path = "Assets/ScriptableData/Gun Data.asset";
var gd = AssetDatabase.LoadAssetAtPath<GunData>(path);
var so = new SerializedObject(gd);
so.FindProperty("damage").floatValue = 20f;
so.FindProperty("startAmmoRemain").intValue = 150;
so.FindProperty("reserveAmmoCap").intValue = 240;
so.FindProperty("magCapacity").intValue = 30;
so.FindProperty("timeBetFire").floatValue = 0.15f;
so.FindProperty("reloadTime").floatValue = 2.3f;
so.FindProperty("fireMode").enumValueIndex = (int)GunData.FireMode.Automatic;
so.FindProperty("range").floatValue = 30f;
so.FindProperty("spreadHalfAngle").floatValue = 3f;
so.FindProperty("pelletsPerShot").intValue = 1;
so.ApplyModifiedProperties();
EditorUtility.SetDirty(gd);
AssetDatabase.SaveAssets();
return "라이플 데이터 갱신 완료: " + AssetDatabase.GetAssetPath(gd);
```

- [ ] **Step 2: 권총/SMG/산탄총 애셋 생성 (execute_code, `manage_scriptable_object` 대신 직접 생성 — 4개를 한 번에 값까지 채우기 위해)**

```csharp
var sb = new System.Text.StringBuilder();

// 권총: 예비탄 무제한
var pistol = ScriptableObject.CreateInstance<GunData>();
pistol.damage = 10f;
pistol.startAmmoRemain = -1;
pistol.reserveAmmoCap = -1;
pistol.magCapacity = 10;
pistol.timeBetFire = 0.2f;
pistol.reloadTime = 2.0f;
pistol.fireMode = GunData.FireMode.Manual;
pistol.range = 15f;
pistol.spreadHalfAngle = 3f;
pistol.pelletsPerShot = 1;
AssetDatabase.CreateAsset(pistol, "Assets/ScriptableData/Pistol Data.asset");
sb.AppendLine("Pistol Data 생성 완료");

// SMG
var smg = ScriptableObject.CreateInstance<GunData>();
smg.damage = 12f;
smg.startAmmoRemain = 200;
smg.reserveAmmoCap = 320;
smg.magCapacity = 40;
smg.timeBetFire = 0.09f;
smg.reloadTime = 2.1f;
smg.fireMode = GunData.FireMode.Automatic;
smg.range = 20f;
smg.spreadHalfAngle = 5f;
smg.pelletsPerShot = 1;
AssetDatabase.CreateAsset(smg, "Assets/ScriptableData/SMG Data.asset");
sb.AppendLine("SMG Data 생성 완료");

// 산탄총: 펠릿 6개, 펠릿당 10 데미지
var shotgun = ScriptableObject.CreateInstance<GunData>();
shotgun.damage = 10f;
shotgun.startAmmoRemain = 40;
shotgun.reserveAmmoCap = 64;
shotgun.magCapacity = 8;
shotgun.timeBetFire = 0.75f;
shotgun.reloadTime = 2.7f;
shotgun.fireMode = GunData.FireMode.Manual;
shotgun.range = 12f;
shotgun.spreadHalfAngle = 10f;
shotgun.pelletsPerShot = 6;
AssetDatabase.CreateAsset(shotgun, "Assets/ScriptableData/Shotgun Data.asset");
sb.AppendLine("Shotgun Data 생성 완료");

AssetDatabase.SaveAssets();
return sb.ToString();
```

- [ ] **Step 3: 값 재확인 (execute_code)**

```csharp
var sb = new System.Text.StringBuilder();
string[] paths = { "Assets/ScriptableData/Gun Data.asset", "Assets/ScriptableData/Pistol Data.asset", "Assets/ScriptableData/SMG Data.asset", "Assets/ScriptableData/Shotgun Data.asset" };
foreach (var p in paths) {
    var gd = AssetDatabase.LoadAssetAtPath<GunData>(p);
    sb.AppendLine(p + " | dmg=" + gd.damage + " mag=" + gd.magCapacity + " reload=" + gd.reloadTime + " range=" + gd.range + " spread=" + gd.spreadHalfAngle + " pellets=" + gd.pelletsPerShot + " mode=" + gd.fireMode + " startAmmo=" + gd.startAmmoRemain + " cap=" + gd.reserveAmmoCap);
}
return sb.ToString();
```

Expected: AGENTS.md 표의 값과 정확히 일치(권총 startAmmo/cap = -1).

- [ ] **Step 4: 커밋**

```bash
git add Assets/ScriptableData/
git commit -m "feat: 무기 4종(권총/소총/SMG/산탄총) GunData 애셋 구성"
```

---

## Task 3: Gun.cs 핵심 로직 수정 — 사거리·레이어·트리거 제외·부모 IDamageable 탐색·총구 장애물·산포·슬롯 교체 API

**Files:**
- Modify: `Assets/Scripts/Gun.cs` (전체 교체)

**Interfaces:**
- Consumes: `GunData.range/spreadHalfAngle/pelletsPerShot/fireMode`(Task 1), `LayerMask.NameToLayer("Environment")`(Task 0)
- Produces: `Gun.ConfigureSlot(GunData data, int magAmmoValue, int reserveAmmoValue)` — Task 5(PlayerShooter)에서 슬롯 전환 시 호출. `Gun.Fire()`가 이제 `bool`을 반환(실제 발사 성공 여부) — Task 5는 이 반환값을 쓰지 않아도 되지만 시그니처가 바뀌었음을 알아야 함. `Gun.hitLayers`(public `LayerMask` 필드, 씬에서 Player 레이어를 뺀 값으로 별도 설정 필요 — Step 3).

- [ ] **Step 1: `Gun.cs` 전체 교체**

```csharp
using System.Collections;
using UnityEngine;

// 총을 구현
public class Gun : MonoBehaviour {
    // 총의 상태를 표현하는 데 사용할 타입을 선언
    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄알집이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 탄알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    public LayerMask hitLayers = ~0; // 총알이 명중 판정할 레이어(씬에서 Player 레이어 제외하도록 설정)
    public float muzzleCheckRadius = 0.05f; // 총구 장애물(벽) 검사 반경

    private int muzzleObstructionMask; // 총구 장애물 검사 전용 레이어 마스크(Environment만)

    private LineRenderer bulletLineRenderer; // 탄알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기

    public GunData gunData; // 총의 현재 데이터

    public int ammoRemain; // 남은 전체 탄알(-1이면 무제한)
    public int magAmmo; // 현재 탄알집에 남아 있는 탄알

    private float lastFireTime; // 총을 마지막으로 발사한 시점

    private void Awake() {
        // 사용할 컴포넌트의 참조 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        // 사용할 점을 두 개로 변경
        bulletLineRenderer.positionCount = 2;

        // 라인 렌더러 비활성화
        bulletLineRenderer.enabled = false;

        // 총구 장애물 검사는 Environment 레이어에만 반응(좀비 근접으로 인한 오검출 방지)
        int environmentLayer = LayerMask.NameToLayer("Environment");
        muzzleObstructionMask = environmentLayer >= 0 ? (1 << environmentLayer) : 0;
    }

    private void OnEnable() {
        // 처음 활성화될 때는 현재 gunData 기준으로 초기화
        ConfigureSlot(gunData, gunData != null ? gunData.magCapacity : 0, gunData != null ? gunData.startAmmoRemain : 0);
    }

    // 무기 슬롯을 교체할 때 총의 데이터와 탄약 상태를 갱신(PlayerShooter가 슬롯 전환 시 호출)
    public void ConfigureSlot(GunData data, int magAmmoValue, int reserveAmmoValue) {
        // 진행 중이던 재장전 등 코루틴 정리
        StopAllCoroutines();
        if (bulletLineRenderer != null)
        {
            bulletLineRenderer.enabled = false;
        }

        gunData = data;
        magAmmo = magAmmoValue;
        ammoRemain = reserveAmmoValue;
        state = magAmmo > 0 ? State.Ready : State.Empty;
        lastFireTime = 0f;
    }

    // 발사 시도. 실제로 총알이 나갔으면 true
    public bool Fire() {
        // 발사 가능 상태 && 마지막 발사 시점으로부터 gunData.timeBetFire 이상의 시간이 지남
        if (state != State.Ready || gunData == null)
        {
            return false;
        }

        if (Time.time < lastFireTime + gunData.timeBetFire)
        {
            return false;
        }

        // 벽에 총구가 막혀 있으면 발사를 거부하고 탄약을 소비하지 않는다
        if (IsMuzzleObstructed())
        {
            return false;
        }

        // 마지막 발사 시점 갱신
        lastFireTime = Time.time;
        // 발사 처리 실행
        Shot();
        return true;
    }

    // 총구가 벽(Environment 레이어)에 파묻혀 있는지 검사
    private bool IsMuzzleObstructed() {
        if (muzzleObstructionMask == 0)
        {
            return false;
        }

        return Physics.CheckSphere(fireTransform.position, muzzleCheckRadius, muzzleObstructionMask, QueryTriggerInteraction.Ignore);
    }

    // 실제 발사 처리
    private void Shot() {
        int pelletCount = Mathf.Max(1, gunData.pelletsPerShot);
        Vector3 lastHitPosition = fireTransform.position + fireTransform.forward * gunData.range;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 shotDirection = ApplySpread(fireTransform.forward, gunData.spreadHalfAngle);

            // 레이캐스트 저장용 컨테이너
            RaycastHit hit;

            // 레이캐스트: 사거리는 무기별 gunData.range, 레이어는 hitLayers, 트리거 콜라이더는 무시
            if (Physics.Raycast(fireTransform.position, shotDirection, out hit, gunData.range, hitLayers, QueryTriggerInteraction.Ignore))
            {
                // 레이가 충돌 한 경우
                // 충돌한 콜라이더 자신 또는 부모에서 IDamageable 탐색(자식 콜라이더에만 붙어 있는 경우 대응)
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

                if (target != null)
                {
                    // 상대방 OnDamage 함수 실행(산탄총은 펠릿마다 개별 판정)
                    target.OnDamage(gunData.damage, hit.point, hit.normal);
                }

                // 충돌한 위치 저장(마지막 펠릿 기준으로 궤적 표시)
                lastHitPosition = hit.point;
            }
            else
            {
                // 레이가 충돌 안함 - 최대 사정거리까지 날아갔을 때 위치를 충돌위치로
                lastHitPosition = fireTransform.position + shotDirection * gunData.range;
            }
        }

        // 발사 이펙트 코루틴으로 재생(마지막 펠릿의 궤적만 표시)
        StartCoroutine(ShotEffect(lastHitPosition));

        // 남은 탄약 -1(펠릿 수와 무관하게 1회 발사당 탄창 1발 소모)
        magAmmo--;
        if (magAmmo <= 0)
        {
            // 탄약 전체 소모시
            state = State.Empty;
        }
    }

    // 발사 방향에 무기별 산포(원뿔 근사)를 적용
    private Vector3 ApplySpread(Vector3 forward, float spreadHalfAngleDeg) {
        if (spreadHalfAngleDeg <= 0f)
        {
            return forward;
        }

        float pitch = Random.Range(-spreadHalfAngleDeg, spreadHalfAngleDeg);
        float yaw = Random.Range(-spreadHalfAngleDeg, spreadHalfAngleDeg);
        return Quaternion.Euler(pitch, yaw, 0f) * forward;
    }

    // 발사 이펙트와 소리를 재생하고 탄알 궤적을 그림
    private IEnumerator ShotEffect(Vector3 hitPosition) {
        // 총구 화염 재생
        muzzleFlashEffect.Play();
        // 탄피 배출 재생
        shellEjectEffect.Play();

        // 총 발사음 재생
        gunAudioPlayer.PlayOneShot(gunData.shotClip);

        // 발사 시작점 지정
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        // 판정 끝점은 입력으로 들어온 충돌 위치
        bulletLineRenderer.SetPosition(1, hitPosition);
        // 라인 렌더러를 활성화하여 탄알 궤적을 그림
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 탄알 궤적을 지움
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        bool magFull = magAmmo >= gunData.magCapacity;
        bool noReserve = ammoRemain == 0; // ammoRemain이 음수(무제한)면 절대 해당 안 됨

        if (state == State.Reloading || noReserve || magFull)
        {
            // 재장전중 / 남은 탄약 없음 / 이미 가득 참 - 장전 불가능
            return false;
        }
        // 재장전 시작
        StartCoroutine(ReloadRoutine());
        return true;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;
        // 재장전 소리 재생
        gunAudioPlayer.PlayOneShot(gunData.reloadClip);

        // 재장전 소요 시간 만큼 처리 쉬기
        yield return new WaitForSeconds(gunData.reloadTime);

        // 탄약 회복량 계산
        int ammoToFill = gunData.magCapacity - magAmmo;

        if (ammoRemain >= 0)
        {
            // 예비 탄약이 유한한 무기: 탄창에 채울 탄약이 남은 전체 탄약량보다 많다면 줄임
            if (ammoRemain < ammoToFill)
            {
                ammoToFill = ammoRemain;
            }

            ammoRemain -= ammoToFill;
        }
        // ammoRemain < 0(무제한, 권총)인 경우 예비 탄약을 소모하지 않는다

        // 장전을 함
        magAmmo += ammoToFill;

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`read_console`로 오류 0건 확인.

- [ ] **Step 3: 씬의 Gun(AK) 인스턴스에 `hitLayers` 설정 (execute_code)**

```csharp
var cs = GameObject.Find("Character_Soldier");
var gun = cs.GetComponentInChildren<Gun>(true);
var so = new SerializedObject(gun);
int playerLayer = LayerMask.NameToLayer("Player");
so.FindProperty("hitLayers").intValue = ~(1 << playerLayer); // Player 레이어만 제외, 나머지 전부 포함
so.ApplyModifiedProperties();
EditorUtility.SetDirty(gun);
return "hitLayers set, excluding layer " + playerLayer;
```

- [ ] **Step 4: 회귀 검증 — 트리거 제외 + 부모 탐색 + 레이어 제외 (Play Mode, execute_code)**

`manage_editor`(action: play)로 Play Mode 진입 후:

```csharp
var sb = new System.Text.StringBuilder();
var cs = GameObject.Find("Character_Soldier");
var gun = cs.GetComponentInChildren<Gun>(true);

// 좀비를 총구 정면 5m 지점에 배치해 명중 확인
var zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie.prefab");
Vector3 spawnPos = gun.fireTransform.position + gun.fireTransform.forward * 5f;
var zombieGO = GameObject.Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
var zombie = zombieGO.GetComponent<Zombie>();
var zombieData = AssetDatabase.LoadAssetAtPath<ZombieData>(AssetDatabase.FindAssets("t:ZombieData")[0] != null ? AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:ZombieData")[0]) : "");
zombie.Setup(zombieData);

int magBefore = gun.magAmmo;
bool fired = gun.Fire();
sb.AppendLine("Fire() 결과: " + fired + ", magAmmo " + magBefore + "->" + gun.magAmmo);
sb.AppendLine("좀비 체력(피격 후, 100이하여야 명중): " + zombie.health);

GameObject.Destroy(zombieGO);
return sb.ToString();
```

Expected: `fired=True`, `magAmmo`가 1 감소, 좀비 `health`가 `startingHealth`(100) 미만으로 감소(명중 확인). 트리거 콜라이더 때문에 좀비보다 먼저/잘못 맞는 현상이 없어야 한다(좀비가 유일한 장애물이므로 명중 자체가 트리거 제외 로직이 정상임을 방증).

- [ ] **Step 5: 총구 장애물 검사 확인 (Play Mode, execute_code)**

```csharp
var sb = new System.Text.StringBuilder();
var cs = GameObject.Find("Character_Soldier");
var gun = cs.GetComponentInChildren<Gun>(true);

int envLayer = LayerMask.NameToLayer("Environment");
var wallGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
wallGO.layer = envLayer;
wallGO.transform.position = gun.fireTransform.position; // 총구를 완전히 감싸도록 배치
wallGO.transform.localScale = Vector3.one * 2f;

int magBefore = gun.magAmmo;
bool fired = gun.Fire();
sb.AppendLine("장애물 있을 때 Fire() 결과(false여야 함): " + fired + ", magAmmo 불변: " + (magBefore == gun.magAmmo));

GameObject.Destroy(wallGO);
return sb.ToString();
```

Expected: `fired=False`, `magAmmo` 불변.

- [ ] **Step 6: Play Mode 종료, Console 확인, 씬 변경사항 없음 확인**

Play Mode 종료(Step 3에서 만든 `hitLayers` 설정은 **Edit Mode에서** 이미 저장했으므로 되돌아가지 않음). `read_console` 오류 0건 확인.

- [ ] **Step 7: 커밋**

```bash
git add Assets/Scripts/GunData.cs Assets/Scripts/Gun.cs Assets/Game/Scenes/UrbanSurvival.unity
git commit -m "feat: Gun 사거리/레이어/트리거제외/부모탐색/총구장애물/산포/슬롯교체 API 구현"
```

---

## Task 4: PlayerInput 확장 — 엣지 발사 입력, 무기 스왑 키(1/2/3/4)

**Files:**
- Modify: `Assets/Scripts/PlayerInput.cs` (전체 교체)

**Interfaces:**
- Produces: `PlayerInput.fireDown`(bool), `PlayerInput.swapToSlot1..4`(bool) — Task 5(PlayerShooter)에서 소비.

- [ ] **Step 1: `PlayerInput.cs` 전체 교체**

```csharp
using UnityEngine;

// 플레이어 캐릭터를 조작하기 위한 사용자 입력을 감지
// 감지된 입력값을 다른 컴포넌트들이 사용할 수 있도록 제공
public class PlayerInput : MonoBehaviour {
    public string moveAxisName = "Vertical"; // 앞뒤 움직임을 위한 입력축 이름
    public string rotateAxisName = "Horizontal"; // 좌우 회전을 위한 입력축 이름
    public string fireButtonName = "Fire1"; // 발사를 위한 입력 버튼 이름
    public string reloadButtonName = "Reload"; // 재장전을 위한 입력 버튼 이름

    // 값 할당은 내부에서만 가능
    public float move { get; private set; } // 감지된 움직임 입력값
    public float rotate { get; private set; } // 감지된 회전 입력값
    public bool fire { get; private set; } // 감지된 발사 입력값(누르고 있는 동안 true, 연사용)
    public bool fireDown { get; private set; } // 발사 버튼을 누른 첫 프레임(단발/엣지 판정용)
    public bool reload { get; private set; } // 감지된 재장전 입력값
    public Vector2 aimPosition { get; private set; } // 감지된 마우스 조준 화면 좌표

    public bool swapToSlot1 { get; private set; } // 1번 슬롯(권총) 스왑 입력
    public bool swapToSlot2 { get; private set; } // 2번 슬롯(소총) 스왑 입력
    public bool swapToSlot3 { get; private set; } // 3번 슬롯(SMG) 스왑 입력
    public bool swapToSlot4 { get; private set; } // 4번 슬롯(산탄총) 스왑 입력

    // 매프레임 사용자 입력을 감지
    private void Update() {
        // 게임오버 상태에서는 사용자 입력을 감지하지 않는다
        if (GameManager.instance && GameManager.instance.isGameover)
        {
            move = 0;
            rotate = 0;
            fire = false;
            fireDown = false;
            reload = false;
            swapToSlot1 = false;
            swapToSlot2 = false;
            swapToSlot3 = false;
            swapToSlot4 = false;
            return;
        }

        // move에 관한 입력 감지
        move = Input.GetAxis(moveAxisName);
        // rotate에 관한 입력 감지
        rotate = Input.GetAxis(rotateAxisName);
        // fire에 관한 입력 감지
        fire = Input.GetButton(fireButtonName);
        fireDown = Input.GetButtonDown(fireButtonName);
        // reload에 관한 입력 감지
        reload = Input.GetButtonDown(reloadButtonName);
        // 마우스 조준 위치 감지(이동과 독립적으로 처리)
        aimPosition = Input.mousePosition;

        // 무기 슬롯 스왑 입력(1=권총, 2=소총, 3=SMG, 4=산탄총)
        swapToSlot1 = Input.GetKeyDown(KeyCode.Alpha1);
        swapToSlot2 = Input.GetKeyDown(KeyCode.Alpha2);
        swapToSlot3 = Input.GetKeyDown(KeyCode.Alpha3);
        swapToSlot4 = Input.GetKeyDown(KeyCode.Alpha4);
    }
}
```

- [ ] **Step 2: 컴파일 확인**

`read_console`로 오류 0건 확인.

- [ ] **Step 3: 커밋**

```bash
git add Assets/Scripts/PlayerInput.cs
git commit -m "feat: 발사 엣지 입력과 무기 슬롯 스왑(1~4) 입력 추가"
```

---

## Task 5: PlayerShooter 재작성 — 무기 슬롯, 탄약 영속화, 발사/재장전 우선순위, 스왑

**배경(스펙 정리):**
- "탄창1발 유지→Empty→해제·새클릭→재장전": 탄약이 0이 된 직후(`State.Empty`)에 발사 버튼을 다시 누르거나(`fireDown`) R을 누르면 재장전이 시작된다(자동 재장전 아님).
- "R/발사 동시입력 우선순위": `State.Ready`이고 탄약이 있을 때 발사 입력과 재장전 입력이 동시에 들어오면 발사가 우선한다(기존 동작 유지).
- "재장전 후에는 발사 버튼을 놓았다 다시 눌러야 발사한다": 재장전이 시작되는 순간 `blockFireUntilRelease`를 세우고, 발사 버튼이 한 번이라도 떼어질 때까지 발사를 막는다. 무기 스왑 시에도 동일하게 적용한다(들고 있던 발사 버튼이 새 무기에 그대로 이어지지 않도록).
- 자동(연사) 무기는 `playerInput.fire`(누르고 있는 동안 true)를, 단발 무기는 `playerInput.fireDown`(엣지)을 발사 조건으로 사용한다.

**Files:**
- Modify: `Assets/Scripts/PlayerShooter.cs` (전체 교체)

**Interfaces:**
- Consumes: `Gun.ConfigureSlot`, `Gun.Fire() : bool`, `Gun.Reload() : bool`, `Gun.state`(Task 3), `PlayerInput.fireDown/swapToSlot1..4`(Task 4), `GunData`(Task 1)
- Produces: `PlayerShooter.WeaponSlot`(enum: Pistol=0, Rifle=1, SMG=2, Shotgun=3) — Task 6(WeaponPickup)에서 사용. `PlayerShooter.UnlockWeapon(WeaponSlot slot, int bonusReserveAmmo)`, `PlayerShooter.AddAmmoToCurrentWeapon(int amount)` — Task 6·AmmoPack 수정에서 호출.

- [ ] **Step 1: `PlayerShooter.cs` 전체 교체**

```csharp
using UnityEngine;

// 주어진 Gun 오브젝트를 쏘거나 재장전
// 무기 4종(권총/소총/SMG/산탄총)을 슬롯으로 관리하며 탄약 상태를 슬롯별로 보존한다
// 알맞은 애니메이션을 재생하고 IK를 사용해 캐릭터 양손이 총에 위치하도록 조정
public class PlayerShooter : MonoBehaviour {
    // 무기 슬롯 순서(키보드 1~4와 대응)
    public enum WeaponSlot {
        Pistol = 0,
        Rifle = 1,
        SMG = 2,
        Shotgun = 3
    }

    private const int SlotCount = 4;

    public Gun gun; // 사용할 총(비주얼은 공용, 데이터만 슬롯별로 교체)
    public Transform gunPivot; // 총 배치의 기준점
    public Transform leftHandMount; // 총의 왼쪽 손잡이, 왼손이 위치할 지점
    public Transform rightHandMount; // 총의 오른쪽 손잡이, 오른손이 위치할 지점

    [Header("무기 슬롯 데이터 (0=권총,1=소총,2=SMG,3=산탄총)")]
    public GunData[] weaponData = new GunData[SlotCount];

    private bool[] unlocked = new bool[SlotCount]; // 슬롯 보유 여부(권총은 항상 true)
    private int[] savedMagAmmo = new int[SlotCount]; // 비활성 슬롯의 탄창 잔량 보존
    private int[] savedReserveAmmo = new int[SlotCount]; // 비활성 슬롯의 예비탄 보존
    private int currentSlot; // 현재 장착 중인 슬롯 인덱스

    private bool blockFireUntilRelease; // 재장전/스왑 직후 발사 버튼을 새로 눌러야 하는 상태

    private PlayerInput playerInput; // 플레이어의 입력
    private Animator playerAnimator; // 애니메이터 컴포넌트

    private void Start() {
        // 사용할 컴포넌트들을 가져오기
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();

        // 권총은 항상 보유
        unlocked[(int)WeaponSlot.Pistol] = true;

        for (int i = 0; i < SlotCount; i++)
        {
            if (weaponData[i] != null)
            {
                savedMagAmmo[i] = weaponData[i].magCapacity;
                savedReserveAmmo[i] = weaponData[i].startAmmoRemain;
            }
        }

        currentSlot = (int)WeaponSlot.Pistol;
        gun.ConfigureSlot(weaponData[currentSlot], savedMagAmmo[currentSlot], savedReserveAmmo[currentSlot]);
    }

    private void OnEnable() {
        // 슈터가 활성화될 때 총도 함께 활성화
        gun.gameObject.SetActive(true);
    }

    private void OnDisable() {
        // 슈터가 비활성화될 때 총도 함께 비활성화
        gun.gameObject.SetActive(false);
    }

    private void Update() {
        HandleWeaponSwapInput();

        // 재장전/스왑 직후에는 발사 버튼을 한 번 떼야 다시 발사할 수 있다
        if (blockFireUntilRelease && !playerInput.fire)
        {
            blockFireUntilRelease = false;
        }

        if (gun.state == Gun.State.Empty)
        {
            // 탄창이 빈 상태: 새 발사 입력(엣지) 또는 R 입력으로만 재장전(자동 재장전 없음)
            if (playerInput.fireDown || playerInput.reload)
            {
                TryReload();
            }
        }
        else if (gun.state == Gun.State.Ready)
        {
            bool wantsFire = !blockFireUntilRelease && GetFireInput();

            if (wantsFire)
            {
                gun.Fire();
            }
            else if (playerInput.reload)
            {
                // 발사와 재장전이 동시에 들어오면 발사가 우선(위 if에서 이미 처리됨)
                TryReload();
            }
        }

        // UI에 탄약 수 갱신
        UpdateUI();
    }

    // 현재 장착한 무기의 발사 모드(연사/단발)에 맞는 입력값 반환
    private bool GetFireInput() {
        if (gun.gunData == null)
        {
            return false;
        }

        return gun.gunData.fireMode == GunData.FireMode.Automatic ? playerInput.fire : playerInput.fireDown;
    }

    private void TryReload() {
        if (gun.Reload())
        {
            // 재장전 입력 감지 후 재장전 성공 시 애니메이션 재생
            playerAnimator.SetTrigger("Reload");
            blockFireUntilRelease = true;
        }
    }

    // 1~4번 키 입력에 따라 무기 슬롯 교체
    private void HandleWeaponSwapInput() {
        // 재장전 중에는 무기를 바꾸지 않는다(진행 중이던 코루틴이 새 무기 탄약을 덮어쓰는 것을 방지)
        if (gun.state == Gun.State.Reloading)
        {
            return;
        }

        int requestedSlot = -1;
        if (playerInput.swapToSlot1) requestedSlot = (int)WeaponSlot.Pistol;
        else if (playerInput.swapToSlot2) requestedSlot = (int)WeaponSlot.Rifle;
        else if (playerInput.swapToSlot3) requestedSlot = (int)WeaponSlot.SMG;
        else if (playerInput.swapToSlot4) requestedSlot = (int)WeaponSlot.Shotgun;

        if (requestedSlot >= 0)
        {
            EquipSlot(requestedSlot);
        }
    }

    // 지정한 슬롯으로 무기 교체(미보유 슬롯 입력은 무시)
    private void EquipSlot(int slotIndex) {
        if (slotIndex == currentSlot || !unlocked[slotIndex] || weaponData[slotIndex] == null)
        {
            return;
        }

        // 현재 무기의 탄약 상태를 저장
        savedMagAmmo[currentSlot] = gun.magAmmo;
        savedReserveAmmo[currentSlot] = gun.ammoRemain;

        currentSlot = slotIndex;
        gun.ConfigureSlot(weaponData[currentSlot], savedMagAmmo[currentSlot], savedReserveAmmo[currentSlot]);
        blockFireUntilRelease = true;
    }

    // 처치 드랍으로 무기를 획득했을 때 호출(WeaponPickup에서 사용) — 슬롯을 해금만 하고 자동 장착하지는 않는다
    public void UnlockWeapon(WeaponSlot slot, int bonusReserveAmmo) {
        int index = (int)slot;
        if (weaponData[index] == null)
        {
            return;
        }

        if (!unlocked[index])
        {
            unlocked[index] = true;
            savedMagAmmo[index] = weaponData[index].magCapacity;
            savedReserveAmmo[index] = weaponData[index].startAmmoRemain;
        }

        if (bonusReserveAmmo > 0 && weaponData[index].reserveAmmoCap >= 0)
        {
            savedReserveAmmo[index] = Mathf.Min(savedReserveAmmo[index] + bonusReserveAmmo, weaponData[index].reserveAmmoCap);
        }

        if (index == currentSlot)
        {
            // 이미 장착 중인 슬롯이면 총에도 즉시 반영
            gun.ConfigureSlot(weaponData[currentSlot], savedMagAmmo[currentSlot], savedReserveAmmo[currentSlot]);
        }
    }

    // 처치 드랍(추가 탄약) 또는 AmmoPack 아이템이 현재 장착 무기의 예비탄을 채울 때 사용
    public void AddAmmoToCurrentWeapon(int amount) {
        GunData currentData = weaponData[currentSlot];
        if (currentData == null || currentData.reserveAmmoCap < 0)
        {
            // 예비탄 무제한 무기는 채울 필요 없음
            return;
        }

        gun.ammoRemain = Mathf.Min(gun.ammoRemain + amount, currentData.reserveAmmoCap);
    }

    // 탄약 UI 갱신
    private void UpdateUI() {
        if (gun != null && UIManager.instance != null)
        {
            // UI 매니저의 탄약 텍스트에 탄창의 탄약과 남은 전체 탄약을 표시
            UIManager.instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
        }
    }

    // 애니메이터의 IK 갱신
    private void OnAnimatorIK(int layerIndex)
    {
        if (gunPivot == null || leftHandMount == null || rightHandMount == null)
        {
            // 레거시 IK 소켓 미배정 상태(현재 Animation Rigging으로 대체 운용 중) - 아무 것도 하지 않음
            return;
        }

        // 총의 기준점을 캐릭터 오른쪽 팔꿈치 위치로 이동시킴
        gunPivot.position = playerAnimator.GetIKHintPosition(AvatarIKHint.RightElbow);

        // IK를 사용하여 왼손의 위치와 회전값을 총의 왼쪽 손잡이에 맞춤
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandMount.rotation);

        // IK를 사용하여 오른손 위치와 회전값을 총의 오른쪽 손잡이에 맞춤
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);
        playerAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.RightHand, rightHandMount.rotation);
    }
}
```

주의: 기존 `OnAnimatorIK`는 `gunPivot`/`leftHandMount`/`rightHandMount`가 `null`이어도 그대로 `.position`에 접근해 `NullReferenceException`이 날 수 있는 코드였다(다만 `ToonPlayer.controller`의 IK Pass가 꺼져 있어 실제 호출된 적이 없었을 뿐 — 작업 로그의 "현재는 무해" 항목과 일치). 이번에 null 가드를 추가해 향후 IK Pass가 켜지더라도 즉시 크래시하지 않도록 안전하게 만들었다. 이 필드들 자체를 채우는 것은 범위 밖(향후 아트 작업)이다.

- [ ] **Step 2: 씬에서 `weaponData` 배열 배정 (execute_code)**

```csharp
var cs = GameObject.Find("Character_Soldier");
var shooter = cs.GetComponent<PlayerShooter>();
var so = new SerializedObject(shooter);

var pistol = AssetDatabase.LoadAssetAtPath<GunData>("Assets/ScriptableData/Pistol Data.asset");
var rifle = AssetDatabase.LoadAssetAtPath<GunData>("Assets/ScriptableData/Gun Data.asset");
var smg = AssetDatabase.LoadAssetAtPath<GunData>("Assets/ScriptableData/SMG Data.asset");
var shotgun = AssetDatabase.LoadAssetAtPath<GunData>("Assets/ScriptableData/Shotgun Data.asset");

var arrayProp = so.FindProperty("weaponData");
arrayProp.arraySize = 4;
arrayProp.GetArrayElementAtIndex(0).objectReferenceValue = pistol;
arrayProp.GetArrayElementAtIndex(1).objectReferenceValue = rifle;
arrayProp.GetArrayElementAtIndex(2).objectReferenceValue = smg;
arrayProp.GetArrayElementAtIndex(3).objectReferenceValue = shotgun;
so.ApplyModifiedProperties();
EditorUtility.SetDirty(shooter);

return "weaponData 배열 배정 완료";
```

- [ ] **Step 3: 컴파일 및 Console 확인**

`read_console`로 오류 0건 확인.

- [ ] **Step 4: Play Mode 통합 검증 — 시작 시 권총 장착, 스왑, 발사 모드별 동작**

Play Mode 진입 후 execute_code:

```csharp
var sb = new System.Text.StringBuilder();
var cs = GameObject.Find("Character_Soldier");
var shooter = cs.GetComponent<PlayerShooter>();
var gun = cs.GetComponentInChildren<Gun>(true);

sb.AppendLine("시작 무기 gunData: " + gun.gunData.name + " (Pistol Data여야 함)");
sb.AppendLine("시작 magAmmo=" + gun.magAmmo + " ammoRemain=" + gun.ammoRemain + " (10 / -1 이어야 함)");

// 2번 슬롯(소총)은 아직 미보유 -> 스왑 무시되어야 함
var soReflect = new SerializedObject(shooter);
// private 필드는 리플렉션으로 직접 접근해 UnlockWeapon 없이 스왑 시도 검증
var fi = typeof(PlayerShooter).GetMethod("EquipSlot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
fi.Invoke(shooter, new object[] { 1 });
sb.AppendLine("미보유 슬롯(소총) 스왑 시도 후에도 여전히 Pistol Data인가: " + (gun.gunData.name == "Pistol Data"));

// 권총으로 3번 발사(단발 모드 확인 - Fire()를 여러 번 호출해도 timeBetFire 간격 없이 연속 호출 시 일부는 실패해야 함)
int before = gun.magAmmo;
bool f1 = gun.Fire();
bool f2 = gun.Fire(); // 같은 프레임, timeBetFire 안 지남 -> false여야 함
sb.AppendLine("연속 Fire() 결과: f1=" + f1 + " f2=" + f2 + " magAmmo " + before + "->" + gun.magAmmo);

return sb.ToString();
```

Expected: 시작 무기 = Pistol Data, `magAmmo=10, ammoRemain=-1`, 미보유 슬롯 스왑이 무시됨, `f1=True, f2=False`(발사 간격 미충족), `magAmmo`가 1만 감소.

- [ ] **Step 5: 무기 해금 후 스왑·탄약 보존 검증 (Play Mode, execute_code)**

```csharp
var sb = new System.Text.StringBuilder();
var cs = GameObject.Find("Character_Soldier");
var shooter = cs.GetComponent<PlayerShooter>();
var gun = cs.GetComponentInChildren<Gun>(true);

// 소총 해금
shooter.UnlockWeapon(PlayerShooter.WeaponSlot.Rifle, 0);

var equipMethod = typeof(PlayerShooter).GetMethod("EquipSlot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
equipMethod.Invoke(shooter, new object[] { (int)PlayerShooter.WeaponSlot.Rifle });
sb.AppendLine("소총 장착 후 gunData: " + gun.gunData.name + " magAmmo=" + gun.magAmmo + " ammoRemain=" + gun.ammoRemain + " (30 / 150 이어야 함)");

// 소총 탄약 일부 소모 후 권총으로 스왑, 다시 소총으로 스왑했을 때 소모된 탄약이 유지되는지
gun.magAmmo = 25; // 5발 소모 가정
equipMethod.Invoke(shooter, new object[] { (int)PlayerShooter.WeaponSlot.Pistol });
sb.AppendLine("권총으로 스왑 후: " + gun.gunData.name);
equipMethod.Invoke(shooter, new object[] { (int)PlayerShooter.WeaponSlot.Rifle });
sb.AppendLine("다시 소총으로 스왑 후 magAmmo(25여야 함): " + gun.magAmmo);

return sb.ToString();
```

Expected: 소총 해금 직후 `magAmmo=30, ammoRemain=150`, 스왑을 오가도 `magAmmo=25`가 그대로 유지.

- [ ] **Step 6: Play Mode 종료, Console 확인**

- [ ] **Step 7: 커밋**

```bash
git add Assets/Scripts/PlayerShooter.cs Assets/Game/Scenes/UrbanSurvival.unity
git commit -m "feat: 무기 슬롯 시스템, 재장전/발사 우선순위, 릴리즈 게이팅 구현"
```

---

## Task 6: 무기 처치 드랍 아이템 — WeaponPickup 스크립트 및 프리팹 3종, AmmoPack 갱신

**Files:**
- Create: `Assets/Scripts/WeaponPickup.cs`
- Modify: `Assets/Scripts/AmmoPack.cs` (현재 장착 무기 예비탄 보급 방식으로 변경)
- Create: `Assets/Prefabs/RiflePickup.prefab`
- Create: `Assets/Prefabs/SMGPickup.prefab`
- Create: `Assets/Prefabs/ShotgunPickup.prefab`

**Interfaces:**
- Consumes: `PlayerShooter.WeaponSlot`, `PlayerShooter.UnlockWeapon`, `PlayerShooter.AddAmmoToCurrentWeapon`(Task 5), `IItem`(기존)
- Produces: 3개 프리팹 경로 — Task 7(ZombieSpawner 드랍 테이블)에서 참조.

- [ ] **Step 1: `WeaponPickup.cs` 생성**

```csharp
using UnityEngine;

// 처치 드랍으로 획득하는 무기 습득 아이템(소총/SMG/산탄총 전용, 권총은 항상 보유하므로 대상 아님)
public class WeaponPickup : MonoBehaviour, IItem {
    public PlayerShooter.WeaponSlot slot; // 이 아이템이 해금할 무기 슬롯

    public void Use(GameObject target) {
        // 전달 받은 게임 오브젝트로부터 PlayerShooter 컴포넌트를 가져오기 시도
        PlayerShooter playerShooter = target.GetComponent<PlayerShooter>();

        if (playerShooter != null)
        {
            // 해당 슬롯을 해금(이미 보유 중이면 상태 유지, 처음 획득이면 기본 탄약으로 초기화)
            playerShooter.UnlockWeapon(slot, 0);
        }

        // 사용되었으므로, 자신을 파괴
        Destroy(gameObject);
    }
}
```

- [ ] **Step 2: `AmmoPack.cs`를 "현재 장착 무기 예비탄 보급"으로 변경**

```csharp
using UnityEngine;

// 현재 장착 중인 무기의 예비 탄약을 충전하는 아이템
public class AmmoPack : MonoBehaviour, IItem {
    public int ammo = 30; // 충전할 총알 수

    public void Use(GameObject target) {
        // 전달 받은 게임 오브젝트로부터 PlayerShooter 컴포넌트를 가져오기 시도
        PlayerShooter playerShooter = target.GetComponent<PlayerShooter>();

        if (playerShooter != null)
        {
            // 현재 장착한 무기의 예비 탄약을 ammo만큼 채움(상한 초과분은 버려짐, 무제한 무기는 무시)
            playerShooter.AddAmmoToCurrentWeapon(ammo);
        }

        // 사용되었으므로, 자신을 파괴
        Destroy(gameObject);
    }
}
```

- [ ] **Step 3: 컴파일 확인**

`read_console`로 오류 0건 확인.

- [ ] **Step 4: 무기 픽업 프리팹 3종 생성 (execute_code — AmmoPack.prefab 구조를 참고해 유사하게 구성)**

```csharp
var sb = new System.Text.StringBuilder();

string[] fbxPaths = {
    "Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/Guns/FBX/AK.fbx", // 소총 픽업 비주얼(플레이어 손 모델과 동일 계열)
    "Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/Guns/FBX/SMG.fbx",
    "Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/Guns/FBX/Shotgun.fbx"
};
string[] names = { "RiflePickup", "SMGPickup", "ShotgunPickup" };
int[] slots = { (int)PlayerShooter.WeaponSlot.Rifle, (int)PlayerShooter.WeaponSlot.SMG, (int)PlayerShooter.WeaponSlot.Shotgun };

for (int i = 0; i < names.Length; i++)
{
    var root = new GameObject(names[i]);
    var sphere = root.AddComponent<SphereCollider>();
    sphere.isTrigger = true;
    sphere.radius = 0.6f;
    root.AddComponent<Rotator>();
    var pickup = root.AddComponent<WeaponPickup>();
    pickup.slot = (PlayerShooter.WeaponSlot)slots[i];

    var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPaths[i]);
    var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
    visual.transform.localPosition = Vector3.zero;
    visual.transform.localScale = Vector3.one * 0.5f;

    string prefabPath = "Assets/Prefabs/" + names[i] + ".prefab";
    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
    GameObject.DestroyImmediate(root);
    sb.AppendLine(prefabPath + " 생성 완료");
}

AssetDatabase.SaveAssets();
return sb.ToString();
```

- [ ] **Step 5: 생성된 프리팹 검증 (execute_code)**

```csharp
var sb = new System.Text.StringBuilder();
string[] paths = { "Assets/Prefabs/RiflePickup.prefab", "Assets/Prefabs/SMGPickup.prefab", "Assets/Prefabs/ShotgunPickup.prefab" };
foreach (var p in paths)
{
    var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
    var pickup = go.GetComponent<WeaponPickup>();
    var col = go.GetComponent<SphereCollider>();
    sb.AppendLine(p + " | slot=" + pickup.slot + " isTrigger=" + col.isTrigger);
}
return sb.ToString();
```

Expected: 오류 없이 3개 프리팹 모두 `isTrigger=True`, `slot`이 각각 Rifle/SMG/Shotgun.

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/WeaponPickup.cs Assets/Scripts/AmmoPack.cs Assets/Prefabs/RiflePickup.prefab* Assets/Prefabs/SMGPickup.prefab* Assets/Prefabs/ShotgunPickup.prefab*
git commit -m "feat: 무기 처치 드랍 픽업 아이템(WeaponPickup) 추가, AmmoPack을 현재무기 예비탄 보급으로 변경"
```

---

## Task 7: 좀비 처치 드랍 테이블 — ZombieSpawner 수정

**가정(초기값, 기획서에 명시 없음):** 드랍 아이템은 `ItemSpawner`의 주기 스폰 아이템과 달리 자동 소멸 타이머를 두지 않는다(처치 보상이므로 플레이어가 주울 때까지 유지). 이 가정이 다르면 이후 조정한다.

**Files:**
- Modify: `Assets/Scripts/ZombieSpawner.cs`

**Interfaces:**
- Consumes: `Assets/Prefabs/RiflePickup.prefab`, `SMGPickup.prefab`, `ShotgunPickup.prefab`(Task 6), 기존 `Assets/Prefabs/AmmoPack.prefab`, `HealthPack.prefab`

- [ ] **Step 1: `ZombieSpawner.cs`에 드랍 필드와 로직 추가**

`Assets/Scripts/ZombieSpawner.cs:7`의 클래스 필드 선언부에 아래를 추가:

```csharp
    [Header("처치 드랍 프리팹 (소총10%/SMG15%/산탄총15%/탄약20%/회복20%/꽝20%)")]
    public GameObject riflePickupPrefab;
    public GameObject smgPickupPrefab;
    public GameObject shotgunPickupPrefab;
    public GameObject ammoPackPrefab;
    public GameObject healthPackPrefab;
```

`CreateZombie()`(파일 57번째 줄 부근)의 `onDeath` 이벤트 등록부를 아래처럼 한 줄 추가:

```csharp
        // onDeath 이벤트에 메서드 등록 - 리스트에서 제거, 화면에서 제거, 점수 증가, 드랍 판정
        zombie.onDeath += () => zombies.Remove(zombie);
        zombie.onDeath += () => Destroy(zombie.gameObject, despawnTime);
        zombie.onDeath += () => GameManager.instance.AddScore(zombieScore);
        zombie.onDeath += () => DropLoot(zombie.transform.position);
```

새 메서드를 클래스 끝에 추가:

```csharp
    // 처치 시 드랍 테이블 판정: 소총10% / SMG15% / 산탄총15% / 탄약20% / 회복상자20% / 꽝(드랍없음)20%
    private void DropLoot(Vector3 position) {
        float roll = Random.Range(0f, 100f);
        GameObject dropPrefab = null;

        if (roll < 10f) dropPrefab = riflePickupPrefab;
        else if (roll < 25f) dropPrefab = smgPickupPrefab;
        else if (roll < 40f) dropPrefab = shotgunPickupPrefab;
        else if (roll < 60f) dropPrefab = ammoPackPrefab;
        else if (roll < 80f) dropPrefab = healthPackPrefab;
        // 80 이상(20%): 꽝, 드랍 없음

        if (dropPrefab != null)
        {
            Instantiate(dropPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
        }
    }
```

- [ ] **Step 2: 컴파일 확인**

`read_console`로 오류 0건 확인.

- [ ] **Step 3: 씬의 ZombieSpawner에 프리팹 배정 (execute_code)**

```csharp
var spawnerGO = GameObject.Find("Zombie Spawner");
var spawner = spawnerGO.GetComponent<ZombieSpawner>();
var so = new SerializedObject(spawner);

so.FindProperty("riflePickupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RiflePickup.prefab");
so.FindProperty("smgPickupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SMGPickup.prefab");
so.FindProperty("shotgunPickupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ShotgunPickup.prefab");
so.FindProperty("ammoPackPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AmmoPack.prefab");
so.FindProperty("healthPackPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HealthPack.prefab");
so.ApplyModifiedProperties();
EditorUtility.SetDirty(spawner);

return "ZombieSpawner 드랍 프리팹 배정 완료";
```

- [ ] **Step 4: 드랍 확률 통계 검증 (Edit Mode, execute_code — 결정론적 시드로 분포 확인)**

`DropLoot`는 `private`이므로 리플렉션으로 여러 번 호출해 각 구간이 실제로 다른 프리팹을 반환하는지 확인한다:

```csharp
var spawnerGO = GameObject.Find("Zombie Spawner");
var spawner = spawnerGO.GetComponent<ZombieSpawner>();
var method = typeof(ZombieSpawner).GetMethod("DropLoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

var sb = new System.Text.StringBuilder();
int before = spawnerGO.transform.childCount;
Random.InitState(1);
for (int i = 0; i < 20; i++)
{
    method.Invoke(spawner, new object[] { Vector3.zero + Vector3.right * i });
}
sb.AppendLine("20회 드랍 판정 후 씬에 생성된 오브젝트 확인은 Hierarchy에서 GameObject.FindObjectsOfType으로:");
var rifles = GameObject.FindObjectsOfType<WeaponPickup>();
sb.AppendLine("생성된 WeaponPickup 인스턴스 수: " + rifles.Length);
foreach (var r in rifles) GameObject.DestroyImmediate(r.gameObject);
var ammos = GameObject.FindObjectsOfType<AmmoPack>();
sb.AppendLine("생성된 AmmoPack 인스턴스 수: " + ammos.Length);
foreach (var a in ammos) GameObject.DestroyImmediate(a.gameObject);
var healths = GameObject.FindObjectsOfType<HealthPack>();
sb.AppendLine("생성된 HealthPack 인스턴스 수: " + healths.Length);
foreach (var h in healths) GameObject.DestroyImmediate(h.gameObject);

return sb.ToString();
```

Expected: 20회 시도 중 무기/탄약/회복 드랍이 섞여서 나오고(전부 0이거나 전부 같은 종류가 아님), 합계가 20 이하(꽝 포함). 테스트로 생성한 오브젝트는 모두 정리(`DestroyImmediate`)했는지 확인.

- [ ] **Step 5: Console 확인, 씬 저장**

- [ ] **Step 6: 커밋**

```bash
git add Assets/Scripts/ZombieSpawner.cs Assets/Game/Scenes/UrbanSurvival.unity
git commit -m "feat: 좀비 처치 드랍 테이블(소총/SMG/산탄총/탄약/회복/꽝) 구현"
```

---

## Task 8: 통합 검증 — Play Mode 전체 흐름

**Files:** 없음(검증 전용)

- [ ] **Step 1: Play Mode 진입, 4무기 전체 발사/재장전 흐름 확인**

```csharp
var sb = new System.Text.StringBuilder();
var cs = GameObject.Find("Character_Soldier");
var shooter = cs.GetComponent<PlayerShooter>();
var gun = cs.GetComponentInChildren<Gun>(true);

// 4개 슬롯 전부 해금
shooter.UnlockWeapon(PlayerShooter.WeaponSlot.Rifle, 0);
shooter.UnlockWeapon(PlayerShooter.WeaponSlot.SMG, 0);
shooter.UnlockWeapon(PlayerShooter.WeaponSlot.Shotgun, 0);

var equipMethod = typeof(PlayerShooter).GetMethod("EquipSlot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

for (int slot = 0; slot < 4; slot++)
{
    equipMethod.Invoke(shooter, new object[] { slot });
    string beforeState = gun.state.ToString();
    int beforeMag = gun.magAmmo;
    bool fired = gun.Fire();
    sb.AppendLine("슬롯" + slot + " (" + gun.gunData.name + "): 장착상태=" + beforeState + " Fire()=" + fired + " magAmmo " + beforeMag + "->" + gun.magAmmo);

    // 탄창을 강제로 0으로 만들어 Empty->Reload 흐름 확인
    gun.magAmmo = 0;
    // Empty 상태 갱신은 다음 Fire()/ConfigureSlot에서 일어나므로 직접 재현
    bool reloaded = gun.Reload();
    sb.AppendLine("  강제 Empty 후 Reload() 시도(마지막 로직 무관하게 항상 시도) = " + reloaded);
}

return sb.ToString();
```

Expected: 4개 무기 모두 `Fire()=True`로 최소 1발 발사, `Reload()` 시도가 예비탄이 있는 무기는 `True`.

- [ ] **Step 2: 실제 좀비 상대 통합 시나리오(발사→명중→처치→드랍→습득)**

```csharp
var sb = new System.Text.StringBuilder();
var cs = GameObject.Find("Character_Soldier");
var gun = cs.GetComponentInChildren<Gun>(true);
var shooter = cs.GetComponent<PlayerShooter>();

var equipMethod = typeof(PlayerShooter).GetMethod("EquipSlot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
equipMethod.Invoke(shooter, new object[] { (int)PlayerShooter.WeaponSlot.Rifle }); // 소총(고데미지)로 확실히 처치

var zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie.prefab");
var zombieDataGuid = AssetDatabase.FindAssets("t:ZombieData")[0];
var zombieData = AssetDatabase.LoadAssetAtPath<ZombieData>(AssetDatabase.GUIDToAssetPath(zombieDataGuid));

Vector3 spawnPos = gun.fireTransform.position + gun.fireTransform.forward * 5f;
var zombieGO = GameObject.Instantiate(zombiePrefab, spawnPos, Quaternion.identity);
var zombie = zombieGO.GetComponent<Zombie>();
zombie.Setup(zombieData);

// 확실히 죽을 때까지 데미지(소총 20뎀, 체력에 맞춰 반복)
int shotsFired = 0;
while (!zombie.dead && shotsFired < 20)
{
    gun.ConfigureSlot(gun.gunData, gun.gunData.magCapacity, gun.ammoRemain); // 매 발마다 재충전해 timeBetFire만 우회
    gun.Fire();
    shotsFired++;
}
sb.AppendLine("좀비 처치까지 발사 횟수: " + shotsFired + ", dead=" + zombie.dead);

var pickupsBefore = GameObject.FindObjectsOfType<WeaponPickup>().Length + GameObject.FindObjectsOfType<AmmoPack>().Length + GameObject.FindObjectsOfType<HealthPack>().Length;
sb.AppendLine("처치 직후 드랍 오브젝트 존재 여부(0~1개, 20% 확률로 0개일 수 있음): " + pickupsBefore);

return sb.ToString();
```

Expected: 좀비가 처치되고(`dead=True`), 드랍 판정 결과 0개 또는 1개의 픽업 오브젝트가 생성됨(오류 없이 실행 완료). 20% 확률로 0개가 나올 수 있으므로 여러 번 재실행해 최소 한 번은 드랍되는 것을 육안 확인.

- [ ] **Step 3: Console 최종 확인**

`read_console`(types: error, warning)로 이번 세션에서 새로 발생한 오류/경고가 없는지 확인.

- [ ] **Step 4: Play Mode 종료**

- [ ] **Step 5: 최종 씬 저장 및 작업 로그 기록**

`manage_scene`(save)로 `UrbanSurvival.unity` 저장. `D:\Codex\Log\20260922_P2무기드랍_Log.md`(또는 당일 날짜 파일)에 목적/대상 파일/변경/검증 결과 기록(AGENTS.md 작업 로그 규칙).

- [ ] **Step 6: `WorkNotes/현재_작업_상태.md`, `WorkNotes/남은_작업.md` 갱신**

P2 완료 항목 반영, 다음 우선순위(P3: 생명체와 일반 적 — 반복 Die 호출 방어, HP 상한/하한, 시간비례 난이도 코드화)로 갱신.

- [ ] **Step 7: 최종 커밋**

```bash
git add -A
git commit -m "test: P2 무기/탄약/드랍 시스템 통합 검증 및 작업 기록 갱신"
```

---

## 2026-09-22 범위 확정(사용자 확인) — 추가 Task

사용자 확인 결과 아래 두 가지를 이번 P2 범위에 포함한다(기존에는 제외 예정이었음).

### Task 3b: 무기 비주얼 교체(모델 스왑) — Task 3 이후, Task 5 이전에 진행

AK 구조 실측 결과: `Character_Soldier/.../Index1.R/AK` 밑에 `LeftHandGrip`(빈 오브젝트, TwoBoneIKConstraint "LeftHandIK"의 `data.target`)과 `Fire Position`(Gun.fireTransform)이 자식으로 있다. 동일 패턴으로 `Pistol`/`SMG`/`Shotgun` FBX(이미 보유: `Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/Guns/FBX/Pistol.fbx`, `SMG.fbx`, `Shotgun.fbx`)를 같은 `Index1.R` 밑에 형제로 배치한다.

- [ ] 각 무기 오브젝트(Pistol/SMG/Shotgun)를 `Index1.R` 밑에 생성: FBX를 자식으로 인스턴스화, `LeftHandGrip`(빈 GameObject, AK의 로컬 위치를 근사 참고값으로 시작), `Fire Position`(빈 GameObject, 총열 방향 축 확인 후 배치) 자식 추가, 각각에 `Gun` 컴포넌트 + `LineRenderer` + `AudioSource` 부착(AK와 동일 구성), `fireTransform`을 그 무기의 `Fire Position`으로, `gunData`를 해당 GunData 애셋으로 연결.
- [ ] 새로 만드는 3개는 씬 시작 시 `SetActive(false)`(AK만 처음에 활성).
- [ ] `PlayerShooter`에 `Gun[] guns`(4개, 인덱스=WeaponSlot) 필드 추가. `EquipSlot()`을 "데이터만 교체" 방식에서 "오브젝트 활성/비활성 전환" 방식으로 변경: 이전 슬롯 `guns[currentSlot].gameObject.SetActive(false)`, 새 슬롯 `guns[slotIndex].gameObject.SetActive(true)`, `gun = guns[slotIndex]`(공개 필드 갱신, 기존 `UpdateUI`/`Fire`/`Reload` 호출부는 그대로 `gun`을 사용), `LeftHandIK.data.target`을 새 슬롯의 `LeftHandGrip`으로 교체.
- [ ] `Gun.OnEnable()`이 매번 재활성화될 때 탄약을 초기화해 버리면 안 되므로, `private bool ammoInitialized` 가드를 추가해 최초 1회만 `ConfigureSlot(gunData, gunData.magCapacity, gunData.startAmmoRemain)` 실행 — 이후 재활성화 시에는 필드값(`magAmmo`/`ammoRemain`)이 그대로 보존된다. 이렇게 하면 Task 5에서 설계한 `savedMagAmmo`/`savedReserveAmmo` 배열은 **불필요**해진다(Gun 인스턴스 자신이 상태를 들고 있음) — Task 5 구현 시 이 배열들을 빼고 단순화한다.
- [ ] 각 무기 FBX의 총열 방향(로컬 축)이 AK와 같은지(-X축) 다른지 개별 확인 후 `Fire Position`의 회전을 맞춘다(무기마다 원점 축이 다를 수 있음 — Gun.fireTransform 오발사 버그 수정 때와 동일한 방식으로 캐릭터를 회전시키며 `fireTransform.forward` 수평 성분을 비교해 검증).
- [ ] Play Mode에서 4개 무기를 순서대로 장착하며 스크린샷으로 모델이 실제로 바뀌는지, 왼손이 각 무기의 `LeftHandGrip`을 자연스럽게 잡는지 확인.

### Task 9: 재장전 모션 — Mixamo 업로드 리타겟

**배경:** `ToonPlayer.controller`에 Reload 상태가 없고, 보유 에셋팩 전체에 재장전 애니메이션이 없음을 확인함(Idle/Run류 Locomotion 1개 + Death 2개뿐). `Character_Soldier`는 Generic 아바타라 Mixamo 표준 Humanoid 리타겟을 바로 쓸 수 없으므로, Mixamo의 "Upload Character"(커스텀 리깅) 기능으로 이 캐릭터 전용 리타겟 애니메이션을 받는다.

**주의(정책):** 로그인 자격 증명 입력은 대행할 수 없다 — Adobe/Mixamo 로그인은 사용자가 브라우저에서 직접 진행해야 한다. 파일 다운로드는 매번 사용자에게 파일명·출처·용량을 밝히고 명시적 동의를 받은 뒤 진행한다.

- [ ] 내장 브라우저로 `mixamo.com` 접속, 사용자에게 로그인 요청(로그인은 사용자가 직접 수행).
- [ ] "Upload Character"로 `Assets/SideProjectAssets/Toon Shooter Game Kit - Dec 2022/Characters/FBX/Character_Soldier.fbx`를 업로드(사용자 동의 후 업로드 진행 — 업로드도 외부 서비스로의 파일 전송이므로 사전 고지).
- [ ] 자동/수동 리깅 캘리브레이션(어깨·팔꿈치·손목·엉덩이·무릎 위치 클릭) — 정밀한 3D 클릭 보정이 필요해 사용자가 직접 수행하는 것을 권장, 필요 시 화면 보며 좌표 안내.
- [ ] "Reload"/"Reload Weapon" 계열 애니메이션 검색, 이 캐릭터로 리타겟된 프리뷰 확인.
- [ ] FBX 다운로드 전 사용자에게 파일명·용량 고지 후 동의받아 다운로드(예: `Assets/SideProjectAssets/Mixamo/Character_Soldier_Reload.fbx`로 임포트).
- [ ] Unity로 임포트 후 `animationType=Generic`으로 설정(Character_Soldier와 동일 리그 매핑이어야 함), 애니메이션 클립을 Loop 없이 추출.
- [ ] `ToonPlayer.controller`에 `Reload`(Trigger) 파라미터, `Reload` 상태(Any State -> Reload, 종료 시 Locomotion 복귀 Exit Time 사용) 추가.
- [ ] Play Mode에서 R 입력 시 재장전 애니메이션이 실제 재생되는지, 총 발사 자세로 자연스럽게 복귀하는지 확인.

이 Task는 외부 서비스(로그인 필요)에 의존하므로 다른 Task 완료 후 별도로 진행하며, 로그인 단계에서 사용자 개입이 필요하면 즉시 안내한다.

## 이번 계획의 의도적 범위 제외 사항

- **무기 스왑 UI 표시(현재 슬롯 강조, 미보유 슬롯 회색 처리 등)**: `UIManager.UpdateAmmoText`만 그대로 사용하고 슬롯 UI는 추가하지 않는다.
- **드랍 아이템 자동 소멸 타이머**: 위 Task 7의 가정대로 없음으로 처리.
- **적 스탯 시간비례 증가, 30분+무한모드, GameManager 상태 머신**은 P3/P4 범위이므로 이번 계획에 포함하지 않는다.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-09-22-p2-weapons-ammo-drops.md`.
