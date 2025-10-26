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
        // 1. Enemy 태그와 충돌했을 때
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                // 🔥 저장된 damageAmount 적용
                enemy.TakeDamage(damageAmount);
            }

            Destroy(gameObject); // 총알 제거
        }
        // 💡 2. CloudCore 태그와 충돌했을 때
        else if (other.CompareTag("CloudCore"))
        {
            CloudCore core = other.GetComponent<CloudCore>();

            if (core != null)
            {
                // CloudCore가 직접 피해를 받는 함수를 호출합니다.
                // 🔥 저장된 damageAmount 적용
                core.TakeDamageIfAttackable(damageAmount);
            }

            Destroy(gameObject); // 총알 제거
        }
        // 💡 CloudCore에 닿았을 때도 총알은 제거됩니다.
    }
}