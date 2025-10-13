using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, RunAway, Suicide }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;
    public float suicideRange = 3f;
    public float explosionRadius = 3f;   // 💡 수정: 자폭 시 타일 파괴 반경을 3m로 변경

    // === 자폭 및 경고 설정 ===
    public float suicideDelay = 1f;
    public Color warningColor = Color.white;

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float lastAttackTime;

    // === 체력 설정 ===
    public int maxHP = 5;
    public int currentHP;

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;

    private Coroutine suicideCoroutine;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        currentHP = maxHP;
        hpSlider.value = 1f;

        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.GetColor("_BaseColor");
        }
    }

    void Update()
    {
        if (player == null) return;

        if (state == EnemyState.Suicide && suicideCoroutine != null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < traceRange)
                    state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < suicideRange)
                    state = EnemyState.Suicide;
                else if (dist < attackRange)
                    state = EnemyState.Attack;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < suicideRange)
                    state = EnemyState.Suicide;
                else if (dist > attackRange)
                    state = EnemyState.Trace;
                else
                    AttackPlayer();
                break;

            case EnemyState.Suicide:
                StartSuicideCountdown();
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer();
                float runawayDistance = 15f;
                if (Vector3.Distance(player.position, transform.position) > runawayDistance)
                    state = EnemyState.Idle;
                break;
        }
    }

    // === 함수 정의 ===

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        hpSlider.value = (float)currentHP / maxHP;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (suicideCoroutine != null)
        {
            StopCoroutine(suicideCoroutine);
            suicideCoroutine = null;
        }
        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }
        Destroy(gameObject);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * movespeed * Time.deltaTime;
        transform.LookAt(player.position);
    }

    void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootProjectile();
        }
        transform.LookAt(player.position);
    }

    void ShootProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            transform.LookAt(player.position);
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                Vector3 dir = (player.position - firePoint.position).normalized;
                ep.SetDirection(dir);
            }
        }
    }

    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;

        float runSpeed = movespeed * 2f;

        transform.position += runDirection * runSpeed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    private void StartSuicideCountdown()
    {
        if (suicideCoroutine == null)
        {
            suicideCoroutine = StartCoroutine(SuicideCountdown());
        }
    }

    IEnumerator SuicideCountdown()
    {
        if (enemyRenderer != null)
        {
            // 빛나는 시각 효과 적용
            enemyRenderer.material.SetColor("_BaseColor", warningColor);
        }

        // 1초 대기 (자폭 딜레이)
        yield return new WaitForSeconds(suicideDelay);

        // 자폭 실행 전에 원래 색상 복구
        if (enemyRenderer != null)
        {
            enemyRenderer.material.SetColor("_BaseColor", originalColor);
        }

        suicideCoroutine = null;

        // 자폭 실행
        ExplodeAndDestroyTiles();
    }

    void ExplodeAndDestroyTiles()
    {
        // 1. 적 주변의 콜라이더 검색 (이제 3m 반경입니다)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        int tilesDestroyed = 0;

        // 2. 검색된 모든 콜라이더를 순회하며 타일 파괴
        foreach (var hitCollider in hitColliders)
        {
            VoxelCollapse tileScript = hitCollider.GetComponent<VoxelCollapse>();

            if (tileScript != null)
            {
                if (tileScript.IsCollapseStarted)
                {
                    tileScript.CancelCollapse();
                }

                tileScript.collapseDelay = 0.001f;

                tileScript.StartDelayedCollapse();
                tilesDestroyed++;
            }
        }

        Debug.Log($"자폭: 주변 {explosionRadius}m 내 {tilesDestroyed}개 타일을 즉시 파괴했습니다.");

        // 마지막으로, 적을 제거합니다.
        Die();
    }
}