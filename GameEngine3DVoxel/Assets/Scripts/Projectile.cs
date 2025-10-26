using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;  // 이동 속도
    public float lifeTime = 2f;    // 생존 시간 (초)

    // 📢 플레이어로부터 받아올 데미지 값
    private int damageAmount = 1;

    // 📢 외부에서 데미지 값을 설정하는 Public 메서드 (플레이어 스크립트에서 호출)
    public void SetDamage(int damage)
    {
        damageAmount = damage;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 일정 시간 후 자동 삭제 (메모리 관리)
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        // 로컬의 forward 방향(앞)으로 이동
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. "Enemy" 태그와 충돌했을 때
        if (other.CompareTag("Enemy"))
        {
            // 🔥🔥 핵심 수정! 🔥🔥
            // Enemy 스크립트 대신 IDamageable "신분증"을 찾습니다.
            IDamageable damageable = other.GetComponent<IDamageable>();

            if (damageable != null)
            {
                // "신분증"이 있다면, 그게 무슨 종류의 적이든 TakeDamage를 호출!
                damageable.TakeDamage(damageAmount);
            }

            Destroy(gameObject); // 총알 제거
        }
        // 💡 2. CloudCore 태그와 충돌했을 때 (이건 원래 로직 그대로 둡니다)
        else if (other.CompareTag("CloudCore"))
        {
            CloudCore core = other.GetComponent<CloudCore>();

            if (core != null)
            {
                core.TakeDamageIfAttackable(damageAmount);
            }

            Destroy(gameObject); // 총알 제거
        }
    }
}