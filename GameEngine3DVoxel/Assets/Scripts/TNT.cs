using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// IDamageable 인터페이스 구현
public class TNT : MonoBehaviour, IDamageable
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, RunAway }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    private float lastAttackTime;

    // === 체력 및 경험치 설정 ===
    public int maxHP = 10;
    public int currentHP;
    public int experienceValue = 5; // 처치 시 지급할 경험치

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;

    private Coroutine blinkCoroutine;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        currentHP = maxHP;

        // HP 슬라이더 초기화
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        // Renderer 초기화 (필요시 색상 저장)
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color; // 필요하다면 유지
        }

        // Rigidbody 설정
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null) { enemyRigidbody = gameObject.AddComponent<Rigidbody>(); }
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        // EnemyManager 등록
        if (EnemyManager.Instance != null) { EnemyManager.Instance.RegisterEnemy(); }
    }

    void Update()
    {
        if (player == null) return;
        if (enemyRigidbody != null && !enemyRigidbody.isKinematic) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (currentHP <= maxHP * 0.2f) state = EnemyState.RunAway;
                else if (dist < traceRange) state = EnemyState.Trace;
                break;
            case EnemyState.Trace:
                if (currentHP <= maxHP * 0.2f) state = EnemyState.RunAway;
                else if (dist < attackRange) state = EnemyState.Attack;
                else TracePlayer();
                break;
            case EnemyState.Attack:
                if (currentHP <= maxHP * 0.2f) state = EnemyState.RunAway;
                else if (dist > attackRange) state = EnemyState.Trace;
                else AttackPlayer();
                break;
            case EnemyState.RunAway:
                RunAwayFromPlayer();
                float runawayDistance = 15f;
                if (Vector3.Distance(player.position, transform.position) > runawayDistance) state = EnemyState.Idle;
                break;
        }
    }

    // === 함수 정의 ===

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return; // 이미 죽었으면 리턴

        // 👈 2. 피격 시 코루틴 호출 (이 3줄을 추가하세요)
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        // --- 기존 코드 (이하 동일) ---
        currentHP -= damage;

        // HP 슬라이더 업데이트
        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        if (currentHP <= 0)
        {
            // 죽기 전에 EnemyManager를 통해 경험치 지급 요청
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.EnemyDefeated(experienceValue);
            }
            Die(); // Die 함수 호출
        }
    }

    private IEnumerator BlinkEffect()
    {
        // Start()에서 찾은 렌더러와 원본 색상을 사용합니다.
        if (enemyRenderer == null) yield break;

        float blinkDuration = 0.1f;

        // 빨간색으로 변경
        enemyRenderer.material.color = Color.red;

        // 0.1초 대기
        yield return new WaitForSeconds(blinkDuration);

        // 원래 색상(originalColor)으로 복구
        enemyRenderer.material.color = originalColor;

        // 코루틴 참조 제거
        blinkCoroutine = null;
    }

    void Die()
    {
        // EnemyManager에 사망 보고
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
        }
        // 오브젝트 파괴
        Destroy(gameObject);
    }

    // TracePlayer 함수: 3D 이동
    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = dir * movespeed * Time.deltaTime;
        transform.position += movement;
        transform.LookAt(player.position);
    }

    // AttackPlayer 함수: 3D 조준 및 공격
    void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootProjectile();
        }
        transform.LookAt(player.position);
    }

    // RunAwayFromPlayer 함수: 3D 도망
    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;
        float runSpeed = movespeed * 2f;
        Vector3 movement = runDirection * runSpeed * Time.deltaTime;
        transform.position += movement;
        transform.rotation = Quaternion.LookRotation(runDirection);
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

    // DeadZone 및 투사체 충돌 처리
    private void OnTriggerEnter(Collider other)
    {
        // DeadZone 충돌 처리
        if (other.CompareTag("DeadZone"))
        {
            // 경험치 없이 Die()만 호출
            Die();
            return;
        }

        // 플레이어 투사체 충돌 처리 (TakeDamage 호출)
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            TakeDamage(1); // 임시 데미지
            Destroy(other.gameObject);
        }
    }
}