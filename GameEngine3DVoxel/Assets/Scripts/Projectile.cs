using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;  // 이동 속도
    public float lifeTime = 2f;    // 생존 시간 (초)

    private int damageAmount = 1; // 플레이어로부터 받아올 데미지 값

    // 데미지 값을 설정하는 함수
    public void SetDamage(int damage)
    {
        damageAmount = damage;
    }

    // 데미지 값을 가져오는 함수 (필요시 사용)
    // public int GetDamage()
    // {
    //     return damageAmount;
    // }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 📢 충돌한 오브젝트에서 IDamageable 컴포넌트(신분증)를 찾습니다.
        IDamageable damageable = other.GetComponent<IDamageable>();

        // 1. IDamageable을 가지고 있는 오브젝트라면 (적이든, CloudCore든)
        if (damageable != null)
        {
            // 📢 태그가 "Enemy" 이거나 "CloudCore" 인지 추가로 확인 (선택 사항)
            //    만약 다른 IDamageable 오브젝트(예: 파괴 가능한 상자)도 총알에 맞게 하려면 이 if문 제거
            if (other.CompareTag("Enemy") || other.CompareTag("CloudCore"))
            {
                // TakeDamage 함수 호출 (CloudCore는 내부에서 isAttackable을 체크함)
                damageable.TakeDamage(damageAmount);

                // 총알 제거
                Destroy(gameObject);
            }
        }
        // 2. IDamageable이 없는 다른 오브젝트와 충돌했을 경우 (예: 벽)
        //    필요하다면 여기에 벽에 부딪히는 이펙트 등을 추가할 수 있습니다.
        // else if (other.CompareTag("Wall")) { /* ... */ }

        // 📢 IDamageable이 없는 오브젝트와 부딪혀도 총알이 사라지게 하려면
        //    Destroy(gameObject); 를 if문 바깥으로 빼냅니다.
        //    (현재는 Enemy나 CloudCore에 맞았을 때만 사라집니다)
    }
}