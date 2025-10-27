using UnityEngine;
using System.Collections; // 👈 1. IEnumerator를 위해 추가!

// 📢 MonoBehaviour 뒤에 , IDamageable 추가!
public class CloudCore : MonoBehaviour, IDamageable
{
    // 체력 변수: 최대 체력을 10으로 설정
    public int maxHP = 10;
    private int currentHP;

    public int experienceValue = 15;

    private bool isAttackable = false; // 공격 가능 상태

    // 🔻 2. [추가] 생성할 포탈 프리팹 (Inspector에서 연결)
    public GameObject portalPrefab;

    // 🔻 3. [추가] 깜빡임 효과를 위한 변수
    private Renderer rend;
    private Color originalColor;
    private Coroutine blinkCoroutine;


    void Start()
    {
        currentHP = maxHP;

        // 🔻 4. [추가] Renderer 및 색상 초기화 (자식 포함)
        // (만약 CloudCore 모델이 자식 오브젝트에 있다면 InChildren을 사용하세요)
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
        else
        {
            Debug.LogWarning("CloudCore가 Renderer를 찾지 못했습니다!", this.gameObject);
        }
    }

    // EnemyManager 스크립트에서 호출하여 공격 가능 상태로 만듭니다.
    public void ActivateAttackability()
    {
        isAttackable = true;
        Debug.Log("[SYSTEM] 구름 핵이 활성화되었습니다. 체력: " + currentHP);

        // 시각적 피드백 (예: 노란색으로 변경)
        if (rend != null) // Renderer가 있는지 확인
        {
            rend.material.color = Color.yellow;
            originalColor = Color.yellow; // 👈 [수정] 깜빡임이 노란색으로 돌아오도록
        }
    }

    // (OnTriggerEnter 주석 처리 부분은 삭제해도 됩니다)

    // 📢 IDamageable 인터페이스의 TakeDamage 함수
    public void TakeDamage(int damage)
    {
        // 공격 가능한 상태인지 먼저 확인합니다.
        if (!isAttackable || currentHP <= 0) return; // [수정] 공격 불가능하거나 이미 죽었으면 반환

        // 🔻 5. [추가] 피격 시 깜빡임
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;
        // Debug.Log("구름 핵이 피해를 입었습니다. 남은 체력: " + currentHP);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 🔻 6. [추가] 깜빡임 코루틴
    private IEnumerator BlinkEffect()
    {
        if (rend == null) yield break;
        float blinkDuration = 0.1f;

        rend.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);

        // originalColor는 ActivateAttackability에서 노란색으로 갱신되었음
        rend.material.color = originalColor;

        blinkCoroutine = null;
    }


    // 파괴 로직
    void Die()
    {
        Debug.Log("구름 핵을 파괴했어!");

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.EnemyDefeated(experienceValue);
            Debug.Log($"플레이어가 구름 핵 경험치 {experienceValue}를 획득했습니다!");
        }
        else
        {
            Debug.LogError("EnemyManager Instance not found! Cannot award EXP for Cloud Core.");
        }

        // 🔻 7. [추가] 포탈 생성 로직
        if (portalPrefab != null)
        {
            // CloudCore의 현재 위치/회전값으로 포탈을 생성
            Instantiate(portalPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("CloudCore에 Portal Prefab이 연결되지 않았습니다!");
        }

        Destroy(gameObject); // CloudCore 자신은 파괴
    }
}