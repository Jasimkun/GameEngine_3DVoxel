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

    // 🔻 1. [추가] 도망 관련 변수
    [Header("RunAway Settings")]
    public float runAwayDuration = 4f; // 도망 지속 시간 (Inspector에서 조절)
    private float runAwayTimer = 0f;   // 도망간 시간 측정용

    // === 공격 설정 ===
    [Header("Attack Settings")] // 헤더 추가 (가독성)
    public float attackCooldown = 1.5f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    private float lastAttackTime;
    public int baseAttackDamage = 1; // 기본 공격력

    // === 체력 및 경험치 설정 ===
    [Header("Stats")] // 헤더 추가
    public int baseMaxHP = 10; // 기본 체력
    public int currentHP;
    public int experienceValue = 10; // 경험치 10으로 수정

    // === 레벨별 최종 스탯 ===
    private int calculatedMaxHP;
    private int calculatedDamage;

    // === 컴포넌트 ===
    [Header("Components")] // 헤더 추가
    public Slider hpSlider; // HP 슬라이더 (UI)
    private Transform player;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;
    private Coroutine blinkCoroutine;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;

        // GameManager 연동 스탯 계산
        int level = 1;
        if (GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;
            calculatedMaxHP = baseMaxHP + (level - 1) * GameManager.Instance.hpBonusPerLevel;
            calculatedDamage = baseAttackDamage + (level - 1) * GameManager.Instance.damageBonusPerLevel;
        }
        else
        {
            calculatedMaxHP = baseMaxHP;
            calculatedDamage = baseAttackDamage;
            Debug.LogWarning("TNT: GameManager Instance not found. Using base stats.");
        }
        currentHP = calculatedMaxHP;

        // HP 슬라이더 초기화
        if (hpSlider != null)
        {
            hpSlider.maxValue = calculatedMaxHP;
            hpSlider.value = currentHP;
        }

        // Renderer 초기화 (자식 포함)
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null) originalColor = enemyRenderer.material.color;
        else Debug.LogWarning("TNT 몬스터가 Renderer를 찾지 못했습니다!", this.gameObject);

        // Rigidbody 설정
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null) enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        // EnemyManager 등록
        if (EnemyManager.Instance != null) EnemyManager.Instance.RegisterEnemy();
    }

    void Update()
    {
        if (player == null) return;
        if (enemyRigidbody != null && !enemyRigidbody.isKinematic) return; // 떨어지는 중이면 중지

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                // 체력이 낮으면 도망 (타이머 리셋)
                if (currentHP <= calculatedMaxHP * 0.2f)
                {
                    state = EnemyState.RunAway;
                    runAwayTimer = 0f; // 🔻 2. 타이머 초기화
                }
                else if (dist < traceRange) state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                // 체력이 낮으면 도망 (타이머 리셋)
                if (currentHP <= calculatedMaxHP * 0.2f)
                {
                    state = EnemyState.RunAway;
                    runAwayTimer = 0f; // 🔻 2. 타이머 초기화
                }
                else if (dist < attackRange) state = EnemyState.Attack;
                else TracePlayer();
                break;

            case EnemyState.Attack:
                // 체력이 낮으면 도망 (타이머 리셋)
                if (currentHP <= calculatedMaxHP * 0.2f)
                {
                    state = EnemyState.RunAway;
                    runAwayTimer = 0f; // 🔻 2. 타이머 초기화
                }
                else if (dist > attackRange) state = EnemyState.Trace;
                else AttackPlayer();
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer(); // 계속 도망가는 동작
                runAwayTimer += Time.deltaTime; // 🔻 3. 시간 측정

                float runawayStopDistance = 5f; // 도망 멈추는 거리 (이전 수정)

                // 🔻 3. [수정] 일정 시간이 지나거나, 플레이어가 멀어지면 Idle로 복귀
                if (runAwayTimer >= runAwayDuration || dist > runawayStopDistance)
                {
                    state = EnemyState.Idle;
                    Debug.Log("TNT stopped running away."); // 로그 추가 (확인용)
                }
                break;
        }
    }

    // === 함수 정의 ===

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return;

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;

        if (hpSlider != null) hpSlider.value = currentHP; // 슬라이더 값 업데이트

        if (currentHP <= 0)
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.EnemyDefeated(experienceValue);
            Die();
        }
    }

    private IEnumerator BlinkEffect()
    {
        if (enemyRenderer == null) yield break;
        float blinkDuration = 0.1f;
        enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);
        enemyRenderer.material.color = originalColor;
        blinkCoroutine = null;
    }

    void Die()
    {
        currentHP = 0; // 확실히 0으로
        if (EnemyManager.Instance != null) EnemyManager.Instance.UnregisterEnemy();
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine); // 코루틴 중지
        Destroy(gameObject);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = dir * movespeed * Time.deltaTime;
        transform.position += movement;
        Vector3 lookTarget = player.position; lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
    }

    void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootProjectile();
        }
        Vector3 lookTarget = player.position; lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
    }

    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;
        float runSpeed = movespeed * 2f; // 도망 속도는 2배
        Vector3 movement = runDirection * runSpeed * Time.deltaTime;
        transform.position += movement;
        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    void ShootProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            CharacterController playerController = player.GetComponent<CharacterController>();
            Vector3 targetPosition = player.position;
            if (playerController != null) targetPosition += playerController.center;

            transform.LookAt(targetPosition); // 몸통 조준
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                Vector3 dir = (targetPosition - firePoint.position).normalized;
                ep.SetDirection(dir);
                // ep.SetDamage(calculatedDamage); // EnemyProjectile이 스스로 계산하므로 주석 처리
            }
        }
    }

    // DeadZone 처리
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Die();
            return;
        }
        // 플레이어 투사체 충돌은 Projectile.cs에서 처리
    }
}