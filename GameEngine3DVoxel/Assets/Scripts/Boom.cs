using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boom : MonoBehaviour
{
    public float speed = 40f;  // 이동 속도
    public float lifeTime = 2f;    // 생존 시간 (초)
    private int damageAmount = 3; // 💥 폭탄의 고정 데미지

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
        // 📢 충돌한 오브젝트에서 IDamageable 컴포넌트(신분증)를 찾습니다.
        IDamageable damageable = other.GetComponent<IDamageable>();

        // 1. IDamageable을 가지고 있는 오브젝트라면 (적이든, CloudCore든)
        if (damageable != null)
        {
            // 📢 태그가 "Enemy" 이거나 "CloudCore" 인지 확인
            if (other.CompareTag("Enemy") || other.CompareTag("CloudCore"))
            {
                // TakeDamage 함수 호출 (고정 데미지 3 사용)
                damageable.TakeDamage(damageAmount);

                // 폭탄 제거
                Destroy(gameObject);
            }
        }
        // 2. IDamageable이 없는 다른 오브젝트와 충돌했을 경우 (예: 벽)
        //    필요하다면 여기에 벽에 부딪히는 이펙트 등을 추가할 수 있습니다.
        // else if (!other.CompareTag("Player")) // 플레이어는 통과시키고 싶다면
        // {
        //      Destroy(gameObject); // 벽 등에 부딪혀도 제거
        // }

        // 📢 IDamageable이 없는 오브젝트와 부딪혀도 폭탄이 사라지게 하려면
        //    Destroy(gameObject); 를 if문 바깥으로 빼냅니다.
    }
}