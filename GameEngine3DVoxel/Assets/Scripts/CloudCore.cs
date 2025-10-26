using UnityEngine;

// 📢 1. MonoBehaviour 뒤에 , IDamageable 추가!
public class CloudCore : MonoBehaviour, IDamageable
{
    // 체력 변수: 최대 체력을 10으로 설정
    public int maxHP = 10;
    private int currentHP;

    private bool isAttackable = false; // 공격 가능 상태

    void Start()
    {
        currentHP = maxHP;
    }

    // EnemyManager 스크립트에서 호출하여 공격 가능 상태로 만듭니다.
    public void ActivateAttackability()
    {
        isAttackable = true;
        Debug.Log("[SYSTEM] 구름 핵이 활성화되었습니다. 체력: " + currentHP);
        // 시각적 피드백 (예: 노란색으로 변경)
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) // Renderer가 있는지 확인
        {
            rend.material.color = Color.yellow;
        }
    }

    // 💡 OnTriggerEnter는 이제 Projectile.cs에서 처리하므로 주석 처리하거나 삭제해도 됩니다.
    /*
    void OnTriggerEnter(Collider other)
    {
        // (Projectile.cs에서 TakeDamage를 직접 호출하므로 이 로직은 불필요)
    }
    */

    // 📢 2. 함수 이름을 TakeDamageIfAttackable에서 TakeDamage로 변경! (IDamageable 인터페이스 요구사항)
    // 이 함수는 Projectile.cs와 PlayerShooting.cs (근접 공격) 양쪽에서 호출됩니다.
    public void TakeDamage(int damage)
    {
        // 공격 가능한 상태인지 먼저 확인합니다.
        if (isAttackable)
        {
            currentHP -= damage;
            // Debug.Log("구름 핵이 피해를 입었습니다. 남은 체력: " + currentHP);

            if (currentHP <= 0)
            {
                Die();
            }
        }
        else
        {
            Debug.Log("[SYSTEM] 공격 불가능! 모든 적이 파괴되지 않았습니다.");
        }
    }

    // 파괴 로직
    void Die()
    {
        Debug.Log("구름 핵을 파괴했어!");
        // 파괴 이펙트, 게임 승리 로직 등 추가
        Destroy(gameObject);
    }
}