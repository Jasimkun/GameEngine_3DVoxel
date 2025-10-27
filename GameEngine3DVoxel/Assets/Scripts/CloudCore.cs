using System.Collections;

using UnityEngine;

// 📢 1. MonoBehaviour 뒤에 , IDamageable 추가!
public class CloudCore : MonoBehaviour, IDamageable
{
    // 체력 변수: 최대 체력을 10으로 설정
    public int maxHP = 10;
    private int currentHP;

    private bool isAttackable = false; // 공격 가능 상태

    private Renderer rend;
    private Color originalColor;
    private Coroutine blinkCoroutine;

    void Start()
    {
        currentHP = maxHP;

        // 🔻 2. Renderer 컴포넌트를 찾고, 원본 색상을 저장합니다 🔻
        rend = GetComponent<Renderer>();
        if (rend != null) // Renderer가 있는지 확인
        {
            originalColor = rend.material.color; // 맨 처음 색상 저장
        }
    }

    // EnemyManager 스크립트에서 호출하여 공격 가능 상태로 만듭니다.
    public void ActivateAttackability()
    {
        isAttackable = true;
        Debug.Log("[SYSTEM] 구름 핵이 활성화되었습니다. 체력: " + currentHP);

        // 🔻 3. 이 부분을 수정합니다 🔻
        // Renderer rend = GetComponent<Renderer>(); // <- 이 줄 삭제 (Start에서 이미 찾음)
        if (rend != null) // 클래스 변수 rend 사용
        {
            rend.material.color = Color.yellow;
            originalColor = rend.material.color; // 👈 "원본 색상"을 노란색으로 갱신!
        }
    }

   
    // 이 함수는 Projectile.cs와 PlayerShooting.cs (근접 공격) 양쪽에서 호출됩니다.
    public void TakeDamage(int damage)
    {
        if (isAttackable)
        {
            // 🔻 4. 피격 시 코루틴 호출 (이 3줄을 추가하세요) 🔻
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkEffect());

            // --- 기존 코드 (이하 동일) ---
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

    private IEnumerator BlinkEffect()
    {
        if (rend == null) yield break;

        float blinkDuration = 0.1f;

        // 빨간색으로 변경
        rend.material.color = Color.red;

        // 0.1초 대기
        yield return new WaitForSeconds(blinkDuration);

        // 갱신된 originalColor (노란색)로 복구
        rend.material.color = originalColor;

        // 코루틴 참조 제거
        blinkCoroutine = null;
    }

    // 파괴 로직
    void Die()
    {
        Debug.Log("구름 핵을 파괴했어!");
        // 파괴 이펙트, 게임 승리 로직 등 추가
        Destroy(gameObject);
    }
}