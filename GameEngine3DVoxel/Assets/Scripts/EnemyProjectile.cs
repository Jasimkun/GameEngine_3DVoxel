using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 2;

    public float speed = 8f;

    public float lifeTime = 3f;

    Vector3 moveDir;

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
            if (pc != null) pc.TakeDamage(damage);

            Destroy(gameObject);
        }
        // 💡 2. VoxelCollapse 타일과 충돌했을 때 (땅 붕괴)
        else
        {
            VoxelCollapse tileScript = other.GetComponent<VoxelCollapse>();

            if (tileScript != null)
            {
                // 타일 붕괴 시작 (지연 시간 0.001초로 즉시 붕괴)
                tileScript.collapseDelay = 0.001f;

                // 만약 붕괴가 이미 시작되었다면 취소 후 재시작 (공격이 붕괴 시간을 초기화하지 않도록 주의)
                if (tileScript.IsCollapseStarted)
                {
                    tileScript.CancelCollapse();
                }

                tileScript.StartDelayedCollapse();

                // 투사체는 타일에 닿아도 파괴되어야 합니다.
                Destroy(gameObject);
            }
        }

        // 💡 주의: 만약 다른 오브젝트(예: 벽)에 닿았을 때도 사라지게 하려면, 
        //    모든 충돌에서 파괴되도록 로직을 조정해야 합니다.
    }
}