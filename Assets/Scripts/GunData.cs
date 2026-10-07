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