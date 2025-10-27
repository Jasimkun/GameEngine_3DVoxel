using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireProjectile : MonoBehaviour
{
    // 🔻 [수정] 기본 데미지 값 (Inspector에서 설정)
    public int baseInitialDamage = 3; // 기본 초기 충돌 데미지
    public int baseDotDamage = 2;     // 기본 틱당 지속 데미지 (DoT)

    public float dotDuration = 2f;    // 지속 데미지 유지 시간 (초)
    public float dotInterval = 0.5f;  // 지속 데미지 틱 간격 (초)

    public float speed = 10f;       // 이동 속도
    public float lifeTime = 3f;

    // 🔻 [추가] 레벨에 따라 계산된 최종 데미지
    private int calculatedInitialDamage;
    private int calculatedDotDamage;

    private Vector3 moveDir;

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

            // 레벨에 맞춰 최종 초기 데미지와 지속 데미지 계산
            calculatedInitialDamage = baseInitialDamage + (level - 1) * GameManager.Instance.damageBonusPerLevel;
            calculatedDotDamage = baseDotDamage + (level - 1) * GameManager.Instance.damageBonusPerLevel;
        }
        else
        {
            // GameManager가 없을 경우 기본 데미지로
            calculatedInitialDamage = baseInitialDamage;
            calculatedDotDamage = baseDotDamage;
            Debug.LogWarning("FireProjectile: GameManager Instance not found. Using base damage.");
        }

        Destroy(gameObject, lifeTime); // 설정된 시간 후 자동 파괴
    }

    void Update()
    {
        // 설정된 방향으로 계속 이동
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
                // 🔻 [수정] 계산된 초기 데미지 적용
                pc.TakeDamage(calculatedInitialDamage);

                // 🔻 [수정] 계산된 지속 데미지로 DoT 코루틴 시작
                pc.StartDamageOverTime(calculatedDotDamage, dotDuration, dotInterval);
            }

            // 플레이어와 충돌했으니 투사체 파괴
            Destroy(gameObject);
        }
        // 2. 플레이어가 아닌 다른 오브젝트와 충돌했을 때 (예: 벽, 타일 등)
        else if (!other.CompareTag("Enemy")) // 몬스터나 다른 몬스터 투사체가 아닐 때
        {
            // VoxelCollapse 타일과 충돌 시 붕괴 로직 (필요하다면)
            VoxelCollapse tileScript = other.GetComponent<VoxelCollapse>();
            if (tileScript != null)
            {
                // 타일 즉시 붕괴 시작
                tileScript.collapseDelay = 0.001f;
                if (!tileScript.IsCollapseStarted)
                {
                    tileScript.StartDelayedCollapse();
                }
            }

            // 벽이나 타일 등 다른 것에 닿으면 투사체 파괴
            Destroy(gameObject);
        }
    }

    // 🔻 [추가] 외부(예: Fire.cs)에서 데미지를 설정할 수 있는 함수 (선택적 사용)
    // 이 스크립트가 Start에서 데미지를 직접 계산하므로, 보통은 호출할 필요 없음
    public void SetDamage(int initialDmg, int dotDmg)
    {
        // baseInitialDamage = initialDmg; // 필요시 기본값을 외부에서 덮어쓰게 할 수 있음
        // baseDotDamage = dotDmg;
        Debug.LogWarning("SetDamage(initial, dot) was called, but FireProjectile now calculates its own damage.");
    }

    // 🔻 [추가] 외부(예: Fire.cs)에서 데미지를 설정할 수 있는 함수 (단일 값 버전, 호환성용)
    // 이전 버전 Fire.cs에서 호출할 경우 대비
    public void SetDamage(int damageValue)
    {
        // baseInitialDamage = damageValue; // 초기 데미지만 덮어쓰거나
        // baseDotDamage = damageValue;   // 지속 데미지만 덮어쓰거나 선택
        Debug.LogWarning("SetDamage(single value) was called, but FireProjectile now calculates its own damage.");
    }
}