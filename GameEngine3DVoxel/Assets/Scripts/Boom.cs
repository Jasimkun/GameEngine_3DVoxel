using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boom : MonoBehaviour
{
    public float speed = 40f;  //이동 속도

    public float lifeTime = 2f;    //생존 시간 (초)

    // Start is called before the first frame update
    void Start()
    {
        //일정 시간 후 자동 삭제 (메모리 관리)
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        //로컬의 forward 방향(앞)으로 이동
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Enemy 태그와 충돌했을 때 (3 피해)
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(3); // 체력 3 감소 (기존 코드 유지)
            }

            Destroy(gameObject); // 무기 제거
        }
        // 💡 2. CloudCore 태그와 충돌했을 때 (3 피해)
        else if (other.CompareTag("CloudCore"))
        {
            CloudCore core = other.GetComponent<CloudCore>();

            if (core != null)
            {
                // 💡 CloudCore에 3의 피해를 입히는 함수를 호출합니다.
                // 이 함수는 CloudCore 내부에서 isAttackable을 체크합니다.
                core.TakeDamageIfAttackable(3);
            }

            Destroy(gameObject); // 무기 제거
        }
    }
}