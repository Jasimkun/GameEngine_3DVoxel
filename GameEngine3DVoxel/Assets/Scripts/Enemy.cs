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
    public float explosionRadius = 3f;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f; // 땅을 체크할 거리
    public float groundOffset = 0.1f;        // 땅 표면에서 적이 떠있는 높이

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
    private Rigidbody enemyRigidbody; // Rigidbody 변수 추가


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        currentHP = maxHP;
        hpSlider.value = 1f;

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
            // 💡 범용적인 .color 속성을 사용하여 원래 색상을 저장
            originalColor = enemyRenderer.material.color;
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

    // 💡 Die 함수 수정: 중력으로 낙하 로직 추가
    void Die()
    {
        if (suicideCoroutine != null)
        {
            StopCoroutine(suicideCoroutine);
            suicideCoroutine = null;
        }

        if (enemyRigidbody != null)
        {
            // 1. Kinematic 해제: 이제 물리 엔진의 힘(중력)을 받게 됩니다.
            enemyRigidbody.isKinematic = false;
            enemyRigidbody.useGravity = true;

            // 2. 렌더러와 콜라이더 비활성화 (더 이상 상호작용 및 추적 금지)
            if (enemyRenderer != null) enemyRenderer.enabled = false;
            if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;

            // 3. 2초 후 최종 파괴 (떨어지는 시간)
            StartCoroutine(DelayedDestroy(2.0f));
        }
        else
        {
            Destroy(gameObject); // Rigidbody가 없으면 즉시 파괴
        }
    }

    // 💡 지연 파괴 코루틴
    IEnumerator DelayedDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    // 💡 TracePlayer 함수 수정: 이동 전 지면 검사 추가
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

        transform.LookAt(lookTarget); // 이제 적은 수평으로만 플레이어를 바라봅니다.
    }

    // 💡 AttackPlayer 함수 수정: Y축 고정 로직 유지
    void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootProjectile();
        }

        SnapToGround();

        // 💡 회전 로직: 플레이어의 Y 좌표를 적의 Y 좌표로 고정 (수직 회전 방지)
        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget); // 수평으로만 회전합니다.
    }

    // 💡 RunAwayFromPlayer 함수 수정: 이동 전 지면 검사 추가
    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;

        float runSpeed = movespeed * 2f;

        // 이동할 거리 계산 (X, Z축만)
        Vector3 movement = new Vector3(runDirection.x, 0, runDirection.z) * runSpeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        // 💡 다음 위치에 지면이 있는지 확인
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


    void ShootProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            // 💡 회전 로직: 플레이어의 Y 좌표를 적의 Y 좌표로 고정 (수직 회전 방지)
            Vector3 lookTarget = player.position;
            lookTarget.y = transform.position.y;

            transform.LookAt(lookTarget); // 수평으로만 회전합니다.

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                // 투사체의 발사 방향은 플레이어의 실제 위치(Y축 포함)를 향해야 합니다.
                Vector3 dir = (player.position - firePoint.position).normalized;
                ep.SetDirection(dir);
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
        // 💡 이 코루틴은 깜빡임 로직이 제거되고 1초 대기 로직만 남아있습니다.
        //    (이전 단계에서 깜빡임 기능을 포기하셨기 때문에 이대로 유지합니다.)

        if (enemyRenderer != null)
        {
            // 빛나는 시각 효과 적용 (1회)
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

        // 마지막으로, 적을 제거합니다. (Die()가 호출되어 낙하 로직이 실행됨)
        Die();
    }
}