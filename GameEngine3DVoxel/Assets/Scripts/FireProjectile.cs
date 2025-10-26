using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireProjectile : MonoBehaviour
{
    public int initialDamage = 3; // 초기 충돌 데미지
    public int dotDamage = 2; // 틱당 지속 데미지 (Damage Over Time)
    public float dotDuration = 2f; // 지속 데미지가 유지되는 시간 (초)
    public float dotInterval = 0.5f; // 지속 데미지 틱 간격 (초)

    public float speed = 10f; // 이동 속도
    public float lifeTime = 3f;

    private Vector3 moveDir;

    public void SetDirection(Vector3 dir)
    {
        moveDir = dir.normalized;
    }

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += moveDir * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // 1. 플레이어와 충돌했을 때
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();

            if (pc != null)
            {
                // 1. 초기 충돌 데미지 적용
                pc.TakeDamage(initialDamage);

                // 2. 지속 데미지 코루틴 시작
                // FireProjectile은 파괴되지만, 플레이어 스크립트에서 코루틴이 실행되어야 함
                // 플레이어에 'DoT(Damage Over Time)' 적용 메서드가 있다고 가정하고 호출
                // 만약 PlayerController에 직접 코루틴을 구현해야 한다면 다음과 같이 호출
                pc.StartDamageOverTime(dotDamage, dotDuration, dotInterval);
            }

            // 플레이어와 충돌했으니 투사체 파괴
            Destroy(gameObject);
        }
        // 2. 플레이어가 아닌 다른 오브젝트와 충돌했을 때
        else
        {
            // 플레이어가 아닌 다른 오브젝트에 닿으면 투사체 파괴 (예: 벽, 일반 타일 등)
            Destroy(gameObject);
        }
    }
}

