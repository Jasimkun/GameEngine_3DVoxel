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
    public int experienceValue = 5; // 처치 시 경험치 (GameManager 연동 불필요)

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
        int level = 1; // 기본 레벨
        if (GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;

            // 레벨에 맞춰 체력과 데미지 계산
            calculatedMaxHP = baseMaxHP + (level - 1) * GameManager.Instance.hpBonusPerLevel;
            calculatedDamage = baseExplosionDamage + (level - 1) * GameManager.Instance.damageBonusPerLevel;
        }
        else
        {
            // GameManager가 없을 경우(테스트 씬 등) 기본 스탯으로
            calculatedMaxHP = baseMaxHP;
            calculatedDamage = baseExplosionDamage;
            Debug.LogWarning("GameManager Instance not found. Using base stats.");
        }

        // 계산된 체력으로 초기화
        currentHP = calculatedMaxHP;

        if (hpSlider != null)
        {
            hpSlider.maxValue = calculatedMaxHP;
            // 초기 슬라이더 값 설정 (1.0 = 꽉 찬 상태)
            hpSlider.value = 1.0f;
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

        // 자폭 중에는 이동/추적 로직 중지
        if (state == EnemyState.Suicide) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (currentHP <= calculatedMaxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < traceRange)
                    state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (currentHP <= calculatedMaxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < suicideRange)
                {
                    state = EnemyState.Suicide;
                    if (suicideCoroutine == null) StartSuicideCountdown(); // 상태 변경 시 즉시 자폭 시작
                }
                else if (dist < attackRange)
                    state = EnemyState.Attack;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
                if (currentHP <= calculatedMaxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < suicideRange)
                {
                    state = EnemyState.Suicide;
                    if (suicideCoroutine == null) StartSuicideCountdown(); // 상태 변경 시 즉시 자폭 시작
                }
                else if (dist > attackRange)
                    state = EnemyState.Trace;
                else
                    AttackPlayer(); // 근접 대기
                break;

            case EnemyState.Suicide:
                // 이미 위에서 처리하므로 여기서는 아무것도 안 함
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
        if (currentHP <= 0) return; // 중복 사망 방지

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;

        // HP 슬라이더 업데이트 (0 ~ 1 사이 값으로)
        if (hpSlider != null && calculatedMaxHP > 0)
        {
            hpSlider.value = (float)currentHP / calculatedMaxHP;
        }

        if (currentHP <= 0)
        {
            // 경험치 지급 (Die 함수보다 먼저 호출)
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.EnemyDefeated(experienceValue);
            }
            Die();
        }
    }

    private IEnumerator BlinkEffect()
    {
        if (enemyRenderer == null) yield break;

        float blinkDuration = 0.1f;
        enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);

        // 현재 상태에 맞는 색으로 복구
        if (state == EnemyState.Suicide)
        {
            enemyRenderer.material.color = warningColor; // 자폭 중이면 경고색
        }
        else
        {
            enemyRenderer.material.color = originalColor; // 아니면 원래색
        }

        blinkCoroutine = null;
    }

    void Die()
    {
        currentHP = 0; // 확실하게 0으로

        // EnemyManager에 사망 보고
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
        }

        // 실행 중인 코루틴들 정지
        if (suicideCoroutine != null)
        {
            StopCoroutine(suicideCoroutine);
            suicideCoroutine = null;
        }
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // 오브젝트 파괴
        Destroy(gameObject);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround();
        }

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y; // Y축 회전 고정
        transform.LookAt(lookTarget);
    }

    void AttackPlayer() // 근접 대기 함수
    {
        SnapToGround(); // 땅에 붙어있도록
        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y; // Y축 회전 고정
        transform.LookAt(lookTarget);
    }

    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;
        float runSpeed = movespeed * 2f;
        Vector3 movement = new Vector3(runDirection.x, 0, runDirection.z) * runSpeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround();
        }

        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            // VoxelCollapse 스크립트가 붙은 타일만 지면으로 인식
            if (hit.collider.GetComponent<VoxelCollapse>() != null)
            {
                return true;
            }
        }
        return false;
    }

    void SnapToGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            VoxelCollapse tileScript = hit.collider.GetComponent<VoxelCollapse>();
            if (tileScript != null)
            {
                // 적의 Y 좌표를 땅 + 오프셋으로 설정
                transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
            }
        }
    }


    private void StartSuicideCountdown()
    {
        // Update에서 한 번만 호출되도록 변경했으므로 if문 불필요
        suicideCoroutine = StartCoroutine(SuicideCountdown());
    }

    IEnumerator SuicideCountdown()
    {
        float elapsedTime = 0f;
        float blinkTimer = 0f;
        bool isBlinkOn = true; // true = warningColor

        // 1. 자폭 시작 시 경고색으로 즉시 변경 (피격 중 아닐 때)
        if (enemyRenderer != null && blinkCoroutine == null)
        {
            enemyRenderer.material.color = warningColor;
        }

        // 2. suicideDelay 시간 동안 반복
        while (elapsedTime < suicideDelay)
        {
            elapsedTime += Time.deltaTime;
            blinkTimer += Time.deltaTime;

            // 3. 진행도 (0.0 ~ 1.0) 계산
            float progress = elapsedTime / suicideDelay;

            // 4. 크기 변경 (Lerp 사용)
            transform.localScale = Vector3.Lerp(originalScale, originalScale * maxSuicideScale, progress);

            // 5. 깜빡임 처리
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer -= blinkInterval;
                isBlinkOn = !isBlinkOn; // 상태 반전

                // 피격 코루틴 실행 중 아닐 때만 색 변경
                if (enemyRenderer != null && blinkCoroutine == null)
                {
                    enemyRenderer.material.color = isBlinkOn ? warningColor : originalColor;
                }
            }

            yield return null; // 다음 프레임까지 대기
        }

        // 6. 루프 종료 (시간 경과)
        suicideCoroutine = null;

        // 7. 자폭 실행
        ExplodeAndDestroyTiles();
    }

    void ExplodeAndDestroyTiles()
    {
        // 1. 주변 콜라이더 검색
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        // 2. 순회하며 타일 파괴 및 플레이어 공격
        foreach (var hitCollider in hitColliders)
        {
            // 플레이어 검색 및 피해 적용
            if (hitCollider.CompareTag("Player"))
            {
                PlayerController playerScript = hitCollider.GetComponent<PlayerController>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(calculatedDamage); // 계산된 데미지 사용
                }
            }

            // 타일 파괴 로직
            VoxelCollapse tileScript = hitCollider.GetComponent<VoxelCollapse>();
            if (tileScript != null)
            {
                if (tileScript.IsCollapseStarted)
                {
                    tileScript.CancelCollapse();
                }
                tileScript.collapseDelay = 0.001f;
                tileScript.StartDelayedCollapse();
            }
        }

        // 마지막으로, 적을 제거
        Die();
    }

    // DeadZone 처리
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Die();
        }
        // 플레이어 투사체 충돌은 Projectile.cs에서 처리하므로 여기서는 필요 없음
    }
}