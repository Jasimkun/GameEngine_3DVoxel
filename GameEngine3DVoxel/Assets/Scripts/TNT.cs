using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TNT : MonoBehaviour
{
    // === 상태 열거형 (Suicide 상태 제거) ===
    public enum EnemyState { Idle, Trace, Attack, RunAway }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;

    // 💡 자폭 관련 변수 제거 (suicideRange, explosionRadius, explosionDamage 등)

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f; // 땅을 체크할 거리
    public float groundOffset = 0.1f;        // 땅 표면에서 적이 떠있는 높이

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float lastAttackTime;

    // === 체력 설정 ===
    public int maxHP = 10; // 💡 maxHP 10으로 고정 (기존 Enemy 스크립트에서는 5였으나, 요청에 따름)
    public int currentHP;

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    // private Coroutine suicideCoroutine; // 💡 제거
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
            // 💡 범용적인 .color 속성을 사용하여 원래 색상을 저장
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

        // 💡 Suicide 코루틴 검사 로직 제거

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
                // 💡 Suicide 관련 상태 전환 제거
                else if (dist < attackRange)
                    state = EnemyState.Attack;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                // 💡 Suicide 관련 상태 전환 제거
                else if (dist > attackRange)
                    state = EnemyState.Trace;
                else
                    AttackPlayer();
                break;

            // 💡 Suicide 상태 처리 로직 제거

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

    // 💡 Die 함수 수정: 즉시 파괴
    void Die()
    {
        // 💡 적이 파괴될 때 EnemyManager에 알립니다.
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
        }

        // 💡 Suicide 코루틴 관련 중지 로직 제거

        // 💡 Rigidbody와 관련된 낙하 로직을 모두 제거하고 즉시 파괴합니다.
        Destroy(gameObject);
    }

    // 💡 DelayedDestroy 코루틴 제거 (사용 안 함)

    // 💡 TracePlayer 함수 수정: 이동 전 지면 검사 및 Y축 고정
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

        transform.LookAt(lookTarget);
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