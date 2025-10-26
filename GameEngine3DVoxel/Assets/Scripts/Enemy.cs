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
    public float groundCheckDistance = 1.0f; // 땅을 체크할 거리
    public float groundOffset = 0.1f;        // 땅 표면에서 적이 떠있는 높이

    // === 자폭 및 경고 설정 ===
    public float suicideDelay = 1f;
    public Color warningColor = Color.white;
    public int explosionDamage = 10; // 자폭 피해량 10으로 설정

    // === 공격 설정 (투사체 관련 변수 제거) ===
    public float attackCooldown = 1.5f;
    // public GameObject projectilePrefab; // 💡 제거
    // public Transform firePoint;          // 💡 제거

    private float lastAttackTime;

    // === 체력 설정 ===
    public int maxHP = 10; // maxHP 10으로 고정
    public int currentHP;

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Coroutine suicideCoroutine;
    private Rigidbody enemyRigidbody;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.value = (float)currentHP / maxHP;
        }

        enemyRenderer = GetComponent<Renderer>();

        // 💡 Rigidbody 설정: Kinematic으로 고정 (평소 이동)
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

        // 💡 EnemyManager에 자신을 등록
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy();
        }
    }

    void Update()
    {
        if (player == null) return;

        // Die()가 호출되어 Kinematic이 해제되면 더 이상 Update의 로직을 수행하지 않음
        if (enemyRigidbody != null && !enemyRigidbody.isKinematic) return;

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
                    AttackPlayer(); // 💡 공격 실행 (근접 공격/대기 역할)
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
        // 💡 적이 파괴될 때 EnemyManager에 알립니다.
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
        }

        if (suicideCoroutine != null)
        {
            StopCoroutine(suicideCoroutine);
            suicideCoroutine = null;
        }

        Destroy(gameObject);
    }

    IEnumerator DelayedDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        // 이동 로직 (지면 검사 후 X, Z 이동)
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround();
        }

        // 💡 회전 로직: 플레이어의 Y 좌표를 적의 Y 좌표로 고정 (수직 회전 방지)
        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget);
    }

    // 💡 AttackPlayer 함수 수정: 투사체 발사 로직 제거 (근접 대기만 수행)
    void AttackPlayer()
    {
        // 💡 투사체 발사 대신, 쿨타임마다 플레이어를 바라보며 대기만 합니다.

        // if (Time.time >= lastAttackTime + attackCooldown)
        // {
        //     lastAttackTime = Time.time;
        //     // ShootProjectile(); // 투사체 발사 로직 제거
        // }

        SnapToGround();

        // 💡 회전 로직: 플레이어의 Y 좌표를 적의 Y 좌표로 고정 (수직 회전 방지)
        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget);
    }

    // 💡 ShootProjectile 함수 제거
    /*
    void ShootProjectile() {
        // ... (내용 제거)
    }
    */

    // 💡 RunAwayFromPlayer 함수 수정: 이동 전 지면 검사 추가
    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;

        float runSpeed = movespeed * 2f;

        // 이동할 거리 계산 (X, Z축만)
        Vector3 movement = new Vector3(runDirection.x, 0, runDirection.z) * runSpeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            // X, Z축 이동
            transform.position = nextPosition;

            // Y좌표 고정 (Snap to Ground)
            SnapToGround();
        }

        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    // 💡 새로운 함수: Raycast를 사용하여 특정 위치에 지면이 있는지 확인
    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
        // 현재 위치보다 조금 높은 곳에서 Raycast를 아래로 쏩니다.
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            // VoxelCollapse 스크립트가 붙은 타일을 찾았다면
            if (hit.collider.GetComponent<VoxelCollapse>() != null)
            {
                return true;
            }
        }
        return false;
    }

    // 💡 새로운 함수: Raycast를 사용하여 적을 지면에 부착시키는 로직 (Y좌표 조정)
    void SnapToGround()
    {
        RaycastHit hit;
        // 현재 위치보다 조금 높은 곳에서 Raycast를 아래로 쏩니다.
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            VoxelCollapse tileScript = hit.collider.GetComponent<VoxelCollapse>();

            if (tileScript != null)
            {
                // 적의 Y 좌표를 충돌 지점(hit.point.y) + 오프셋으로 설정합니다.
                transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
            }
        }
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
        // 💡 자폭 전 경고 효과 로직 (유지)
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = warningColor;
        }

        // 1초 대기 (자폭 딜레이)
        yield return new WaitForSeconds(suicideDelay);

        // 자폭 실행 전에 원래 색상 복구
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = originalColor;
        }

        suicideCoroutine = null;

        // 자폭 실행
        ExplodeAndDestroyTiles();
    }

    void ExplodeAndDestroyTiles()
    {
        // 1. 적 주변의 콜라이더 검색 (3m 반경)
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        // 2. 검색된 모든 콜라이더를 순회하며 타일 파괴 및 플레이어 공격
        foreach (var hitCollider in hitColliders)
        {
            // 💡 플레이어 검색 및 피해 적용
            if (hitCollider.CompareTag("Player"))
            {
                PlayerController playerScript = hitCollider.GetComponent<PlayerController>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(explosionDamage); // 플레이어에게 10의 피해 적용
                    Debug.Log($"자폭 피해! 플레이어에게 {explosionDamage} 데미지를 입혔습니다.");
                }
            }

            // 3. 타일 파괴 로직
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

        Debug.Log($"자폭: 주변 {explosionRadius}m 내 타일을 즉시 파괴했습니다.");

        // 마지막으로, 적을 제거합니다.
        Die();
    }

    // 💡 DeadZone에 닿았는지 확인하는 Trigger 함수
    private void OnTriggerEnter(Collider other)
    {
        // DeadZone 태그를 가진 오브젝트와 충돌했는지 확인합니다.
        if (other.CompareTag("DeadZone"))
        {
            Debug.Log("적이 DeadZone에 진입! 사망 처리합니다.");
            Die();
        }
    }
}