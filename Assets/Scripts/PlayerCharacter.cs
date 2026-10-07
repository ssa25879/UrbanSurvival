using UnityEngine;

// 선택된 캐릭터 데이터의 배율을 플레이어 이동·체력·총기와 외형에 적용
// PlayerHealth.OnEnable이 최대 체력을 읽기 전에 적용되도록 먼저 실행한다
[DefaultExecutionOrder(-100)]
public class PlayerCharacter : MonoBehaviour {
    public CharacterData defaultCharacter; // 인트로를 거치지 않고 시작했을 때 사용할 캐릭터(캐릭터 A)
    public string uniformMaterialName = "Character_Main"; // 유니폼 색을 바꿀 머티리얼 이름

    public CharacterData current { get; private set; } // 이번 판에 적용된 캐릭터

    private void Awake() {
        current = CharacterSelection.selected != null ? CharacterSelection.selected : defaultCharacter;
        if (current == null)
        {
            return;
        }

        // 씬에 직렬화된 기준값에 배율을 곱한다(씬을 다시 불러오면 기준값으로 돌아오므로 재시작해도 누적되지 않음)
        GetComponent<PlayerMovement>().moveSpeed *= current.moveSpeedMultiplier;
        GetComponent<PlayerHealth>().startingHealth *= current.maxHealthMultiplier;

        // 비활성 슬롯 무기까지 모두 적용(GunData 원본은 수정하지 않음)
        foreach (Gun gun in GetComponentsInChildren<Gun>(true))
        {
            gun.attackSpeedMultiplier = current.attackSpeedMultiplier;
            gun.reloadSpeedMultiplier = current.reloadSpeedMultiplier;
            gun.characterDamageMultiplier = current.damageMultiplier;
        }

        ApplyUniformColor();
    }

    // 유니폼 머티리얼 슬롯만 색을 바꾼다(머티리얼 원본을 건드리지 않도록 MaterialPropertyBlock 사용, 총기는 제외)
    private void ApplyUniformColor() {
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer bodyRenderer in GetComponentsInChildren<Renderer>(true))
        {
            if (bodyRenderer.GetComponentInParent<Gun>() != null)
            {
                continue;
            }

            Material[] materials = bodyRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || materials[i].name != uniformMaterialName)
                {
                    continue;
                }

                bodyRenderer.GetPropertyBlock(block, i);
                block.SetColor("_BaseColor", current.uniformColor);
                bodyRenderer.SetPropertyBlock(block, i);
            }
        }
    }
}
