using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;  // 이동 속도
    public float lifeTime = 2f;    // 생존 시간 (초)

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
                enemy.TakeDamage(1); // 체력 1 감소
            }

            Destroy(gameObject); // 총알 제거
        }
        // 💡 2. CloudCore 태그와 충돌했을 때
        else if (other.CompareTag("CloudCore"))
        {
            CloudCore core = other.GetComponent<CloudCore>();

            if (core != null)
            {
                // CloudCore 스크립트의 OnTriggerEnter 로직을 호출하는 대신, 
                // 여기서 직접 피해를 입히도록 수정합니다.

                // 🚨 주의: CloudCore 스크립트가 공격 가능한지 확인해야 합니다.
                // CloudCore 스크립트에 IsAttackable 속성이나 GetAttackableStatus() 함수가 있다고 가정합니다.

                // CloudCore가 직접 피해를 받는 함수를 호출합니다.
                // 이 함수는 CloudCore 내에서 isAttackable을 체크합니다.
                core.TakeDamageIfAttackable(1);
            }

            Destroy(gameObject); // 총알 제거
        }
        // 💡 CloudCore에 닿았을 때도 총알은 제거됩니다.
    }
}