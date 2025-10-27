using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour, IDamageable
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, RunAway, Suicide }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;
    public float suicideRange = 3f;
    public float explosionRadius = 3f;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f;
    public float groundOffset = 0.1f;

    // === 자폭 및 경고 설정 ===
    public float suicideDelay = 3f; // (Inspector에서 3초로 설정 권장)
    public Color warningColor = Color.white;
    public int baseExplosionDamage = 10; // 기본 자폭 데미지

    // === 자폭 연출 변수 ===
    public float blinkInterval = 0.2f; // 자폭 시 깜빡임 간격
    public float maxSuicideScale = 2.0f; // 자폭 시 최대 크기 배율
    private Vector3 originalScale; // 원래 크기

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    // === 체력 설정 ===
    public int baseMaxHP = 10; // 기본 체력
    public int currentHP;
    public int experienceValue = 5; // 처치 시 경험치

    // === 레벨별 최종 스탯 ===
    private int calculatedMaxHP;
    private int calculatedDamage;

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Coroutine suicideCoroutine;
    private Rigidbody enemyRigidbody;
    private Coroutine blinkCoroutine;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;

        // GameManager에서 현재 레벨을 가져와 스탯 계산
        int level = 1;
        if (GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;
            calculatedMaxHP = baseMaxHP + (level - 1) * GameManager.Instance.hpBonusPerLevel;
            calculatedDamage = baseExplosionDamage + (level - 1) * GameManager.Instance.damageBonusPerLevel;
        }
        else
        {
            calculatedMaxHP = baseMaxHP;
            calculatedDamage = baseExplosionDamage;
            Debug.LogWarning("Enemy (Suicide): GameManager Instance not found. Using base stats.");
        }

        // 계산된 체력으로 초기화
        currentHP = calculatedMaxHP; // 👈 currentHP를 먼저 설정!

        if (hpSlider != null)
        {
            hpSlider.maxValue = calculatedMaxHP;
            // 🔻 [수정] 1.0f 대신 실제 체력 값(currentHP)으로 설정 🔻
            hpSlider.value = currentHP;
        }

        // 자식 포함 Renderer 검색
        enemyRenderer = GetComponentInChildren<Renderer>();

        // Rigidbody 설정
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("Enemy(자폭병)가 Renderer를 찾지 못했습니다! (연출 효과 실패)", this.gameObject);
        }

        // 원래 크기 저장
        originalScale = transform.localScale;

        // EnemyManager에 자신을 등록
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy();
        }
    }

    void Update()
    {
        if (player == null) return;
        if (enemyRigidbody != null && !enemyRigidbody.isKinematic) return; // 떨어지는 중이면 로직 중지
        if (state == EnemyState.Suicide) return; // 자폭 중에는 이동/추적 로직 중지

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (currentHP <= calculatedMaxHP * 0.2f) state = EnemyState.RunAway;
                else if (dist < traceRange) state = EnemyState.Trace;
                break;
            case EnemyState.Trace:
                if (currentHP <= calculatedMaxHP * 0.2f) state = EnemyState.RunAway;
                else if (dist < suicideRange) { state = EnemyState.Suicide; if (suicideCoroutine == null) StartSuicideCountdown(); }
                else if (dist < attackRange) state = EnemyState.Attack;
                else TracePlayer();
                break;
            case EnemyState.Attack:
                if (currentHP <= calculatedMaxHP * 0.2f) state = EnemyState.RunAway;
                else if (dist < suicideRange) { state = EnemyState.Suicide; if (suicideCoroutine == null) StartSuicideCountdown(); }
                else if (dist > attackRange) state = EnemyState.Trace;
                else AttackPlayer(); // 근접 대기
                break;
            case EnemyState.Suicide: break; // 위에서 처리
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

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;

        // HP 슬라이더 업데이트
        if (hpSlider != null) // && calculatedMaxHP > 0 조건 제거
        {
            // 🔻 [수정] 비율 계산 대신 실제 체력 값(currentHP)으로 설정 🔻
            hpSlider.value = currentHP;
        }

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
        enemyRenderer.material.color = (state == EnemyState.Suicide) ? warningColor : originalColor;
        blinkCoroutine = null;
    }

    void Die()
    {
        currentHP = 0;
        if (EnemyManager.Instance != null) EnemyManager.Instance.UnregisterEnemy();
        StopAllCoroutines(); // 자폭, 깜빡임 모두 중지
        Destroy(gameObject);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;
        if (CheckGround(nextPosition)) { transform.position = nextPosition; SnapToGround(); }
        Vector3 lookTarget = player.position; lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
    }

    void AttackPlayer() // 근접 대기 함수
    {
        SnapToGround();
        Vector3 lookTarget = player.position; lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
    }

    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;
        float runSpeed = movespeed * 2f;
        Vector3 movement = new Vector3(runDirection.x, 0, runDirection.z) * runSpeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;
        if (CheckGround(nextPosition)) { transform.position = nextPosition; SnapToGround(); }
        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            return hit.collider.GetComponent<VoxelCollapse>() != null;
        }
        return false;
    }

    void SnapToGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            if (hit.collider.GetComponent<VoxelCollapse>() != null)
            {
                transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
            }
        }
    }

    private void StartSuicideCountdown()
    {
        suicideCoroutine = StartCoroutine(SuicideCountdown());
    }

    IEnumerator SuicideCountdown()
    {
        float elapsedTime = 0f;
        float blinkTimer = 0f;
        bool isBlinkOn = true;
        if (enemyRenderer != null && blinkCoroutine == null) enemyRenderer.material.color = warningColor;

        while (elapsedTime < suicideDelay)
        {
            elapsedTime += Time.deltaTime;
            blinkTimer += Time.deltaTime;
            float progress = elapsedTime / suicideDelay;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxSuicideScale, progress);

            if (blinkTimer >= blinkInterval)
            {
                blinkTimer -= blinkInterval;
                isBlinkOn = !isBlinkOn;
                if (enemyRenderer != null && blinkCoroutine == null)
                {
                    enemyRenderer.material.color = isBlinkOn ? warningColor : originalColor;
                }
            }
            yield return null;
        }
        suicideCoroutine = null;
        ExplodeAndDestroyTiles();
    }

    void ExplodeAndDestroyTiles()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                PlayerController playerScript = hitCollider.GetComponent<PlayerController>();
                if (playerScript != null) playerScript.TakeDamage(calculatedDamage);
            }
            VoxelCollapse tileScript = hitCollider.GetComponent<VoxelCollapse>();
            if (tileScript != null)
            {
                if (tileScript.IsCollapseStarted) tileScript.CancelCollapse();
                tileScript.collapseDelay = 0.001f;
                tileScript.StartDelayedCollapse();
            }
        }
        Die();
    }

    // DeadZone 처리
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone")) Die();
    }
}