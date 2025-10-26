using UnityEngine;

public class CloudCore : MonoBehaviour
{
    // 💡 체력 변수: 최대 체력을 10으로 설정
    public int maxHP = 10;
    private int currentHP;

    private bool isAttackable = false;

    void Start()
    {
        // 💡 게임 시작 시 체력 초기화
        currentHP = maxHP;
    }

    // 💡 EnemyManager 스크립트에서 호출하여 공격 가능 상태로 만듭니다.
    public void ActivateAttackability()
    {
        isAttackable = true;
        Debug.Log("[SYSTEM] 구름 핵이 활성화되었습니다. 체력: " + currentHP);
        // 시각적/청각적 피드백 추가 (예: 빛나게 하기)
        GetComponent<Renderer>().material.color = Color.yellow; 
    }

    // 💡 공격 감지 로직 (예: 플레이어의 투사체 충돌)
    void OnTriggerEnter(Collider other)
    {
        // 공격이 가능한 상태일 때만 처리
        if (isAttackable)
        {
            // 예시: "PlayerAttack" 태그를 가진 오브젝트와 충돌했을 때
            if (other.CompareTag("PlayerAttack"))
            {
                // 투사체가 닿으면 1의 피해를 줍니다.
                TakeDamage(1);
                // 투사체를 파괴합니다. (필요하다면)
                Destroy(other.gameObject);
            }
        }
        else
        {
            // 공격 불가능 상태일 때 피드백 제공
            Debug.Log("[SYSTEM] 공격 불가능! 먼저 모든 적을 파괴해야 합니다.");
        }
    }

    // 💡 체력 감소 로직 구현
    void TakeDamage(int damage)
    {
        currentHP -= damage;
       // Debug.Log("구름 핵이 피해를 입었습니다. 남은 체력: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // CloudCore.cs 파일 내부에 이 함수를 추가하거나, 기존 TakeDamage를 수정해야 합니다.

    public void TakeDamageIfAttackable(int damage)
    {
        // 💡 Projectile.cs에서 호출될 때 여기서 isAttackable 상태를 확인합니다.
        if (isAttackable)
        {
            currentHP -= damage;
            //Debug.Log("구름 핵이 피해를 입었습니다. 남은 체력: " + currentHP);

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

    // 💡 파괴 로직 구현
    void Die()
    {
        Debug.Log("구름 핵을 파괴했어!");
        // 파괴 이펙트, 게임 승리 또는 다음 씬 로딩 로직 등을 여기에 구현합니다.
        Destroy(gameObject);
    }
}