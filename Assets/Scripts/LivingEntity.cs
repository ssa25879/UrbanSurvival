using System;
using UnityEngine;

// 생명체로서 동작할 게임 오브젝트들을 위한 뼈대를 제공
// 체력, 데미지 받아들이기, 사망 기능, 사망 이벤트를 제공
public class LivingEntity : MonoBehaviour, IDamageable {
    public float startingHealth = 100f; // 시작 체력
    public float health { get; protected set; } // 현재 체력
    public bool dead { get; protected set; } // 사망 상태
    public event Action onDeath; // 사망시 발동할 이벤트

    // 생명체가 활성화될때 상태를 리셋
    protected virtual void OnEnable() {
        // 사망하지 않은 상태로 시작
        dead = false;
        // 체력을 시작 체력으로 초기화
        health = startingHealth;
    }

    // 데미지를 입는 기능
    public virtual void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal) {
        // 이미 사망한 경우 중복 데미지 처리 방지
        if (dead)
        {
            return;
        }

        // 데미지만큼 체력 감소, 하한 0으로 고정
        health = Mathf.Max(0f, health - damage);

        // 체력이 0 이하라면 사망 처리 실행
        if (health <= 0)
        {
            Die();
        }
    }

    // 체력을 회복하는 기능
    public virtual void RestoreHealth(float newHealth) {
        if (dead)
        {
            // 이미 사망한 경우 체력을 회복할 수 없음
            return;
        }

        // 체력 추가, 상한을 시작 체력으로 고정
        health = Mathf.Min(startingHealth, health + newHealth);
    }

    // 사망 처리
    public virtual void Die() {
        // 이미 사망 처리된 경우 중복 실행 방지(사망 상태를 가장 먼저 표시)
        if (dead)
        {
            return;
        }
        dead = true;

        // onDeath 이벤트에 등록된 메서드가 있다면 실행
        if (onDeath != null)
        {
            onDeath();
        }
    }
}