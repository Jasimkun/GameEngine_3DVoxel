using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// IDamageable 인터페이스 구현
public class Fire : MonoBehaviour, IDamageable
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, RunAway }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;

    // === 공격 설정 ===
    public float attackCooldown = 5.0f;
    public GameObject fireProjectilePrefab;
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

        // 🔻 1. [수정] InChildren을 추가하여 자식 오브젝트까지 검색
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("Fire 몬스터가 Renderer를 찾지 못했습니다!", this.gameObject);
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
        if (currentHP <= 0) return;

        // 피격 시 코루틴 호출
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        // 🔻 2. [추가] 🚨 체력 깎는 코드가 빠져있었습니다! 🚨
        currentHP -= damage;

        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        if (currentHP <= 0)
        {
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.EnemyDefeated(experienceValue);
            }
            Die();
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
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
        }
        Destroy(gameObject);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = dir * movespeed * Time.deltaTime;
        transform.position += movement;
        transform.LookAt(player.position);
    }

    void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootFireProjectile();
        }
        transform.LookAt(player.position);
    }

    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;
        float runSpeed = movespeed * 2f;
        Vector3 movement = runDirection * runSpeed * Time.deltaTime;
        transform.position += movement;
        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    void ShootFireProjectile()
    {
        if (fireProjectilePrefab != null && firePoint != null)
        {
            transform.LookAt(player.position);
            GameObject proj = Instantiate(fireProjectilePrefab, firePoint.position, firePoint.rotation);
            FireProjectile fp = proj.GetComponent<FireProjectile>();
            if (fp != null)
            {
                Vector3 dir = (player.position - firePoint.position).normalized;
                fp.SetDirection(dir);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Die();
            return;
        }

        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            TakeDamage(1); // 임시 데미지
            Destroy(other.gameObject);
        }
    }
}