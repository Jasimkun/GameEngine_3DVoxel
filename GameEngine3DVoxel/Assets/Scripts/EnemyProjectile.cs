using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    // 🔻 [수정] 기본 데미지 (Inspector에서 설정)
    public int baseDamage = 1;

    // 🔻 [추가] 레벨에 따라 계산된 최종 데미지
    private int calculatedDamage;

    public float speed = 8f;
    public float lifeTime = 3f;

    Vector3 moveDir;

    public void SetDirection(Vector3 dir)
    {
        moveDir = dir.normalized;
    }

    void Start()
    {
        // 🔻 [수정] GameManager에서 현재 레벨을 가져와 데미지 계산
        int level = 1; // 기본 레벨
        if (GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;

            // 레벨에 맞춰 최종 데미지 계산
            calculatedDamage = baseDamage + (level - 1) * GameManager.Instance.damageBonusPerLevel;
        }
        else
        {
            // GameManager가 없을 경우 기본 데미지로
            calculatedDamage = baseDamage;
            Debug.LogWarning("EnemyProjectile: GameManager Instance not found. Using base damage.");
        }

        Destroy(gameObject, lifeTime);
    }

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
                // 🔻 [수정] 계산된 데미지 사용
                pc.TakeDamage(calculatedDamage);
            }

            Destroy(gameObject);
        }
        // 2. VoxelCollapse 타일과 충돌했을 때 (땅 붕괴)
        else
        {
            VoxelCollapse tileScript = other.GetComponent<VoxelCollapse>();

            if (tileScript != null)
            {
                // 타일 붕괴 시작 (즉시 붕괴)
                tileScript.SetTemporaryDelay(0.001f);

                // 이미 붕괴 중이면 취소 후 재시작 방지 (선택 사항)
                if (!tileScript.IsCollapseStarted)
                {
                    tileScript.StartDelayedCollapse();
                }
                // else { tileScript.CancelCollapse(); tileScript.StartDelayedCollapse(); } // 필요하다면 취소 후 재시작


                // 투사체는 타일에 닿으면 파괴
                Destroy(gameObject);
            }
            // 3. (선택) 그 외 다른 오브젝트와 충돌했을 때 (예: 벽)
            // else if (!other.CompareTag("EnemyProjectile") && !other.CompareTag("Enemy")) // 자기 자신이나 다른 적이 아닐 때
            // {
            //     Destroy(gameObject); // 벽 등에 닿으면 파괴
            // }
        }
    }

    // 🔻 [추가] TNT.cs 에서 호출할 SetDamage 함수 (호출은 안 하지만, 혹시 모르니 남겨둠)
    // (이 스크립트가 데미지를 직접 계산하므로, 외부에서 호출할 필요는 없어졌습니다.)
    public void SetDamage(int newDamage)
    {
        // baseDamage = newDamage; // 필요하다면 기본 데미지를 외부에서 설정하게 할 수도 있음
        // Start()에서 계산되므로 여기서는 calculatedDamage를 직접 바꾸진 않음
        Debug.LogWarning("SetDamage was called, but EnemyProjectile now calculates its own damage based on level.");
    }
}