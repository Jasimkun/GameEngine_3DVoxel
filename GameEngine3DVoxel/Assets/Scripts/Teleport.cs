using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Teleport : MonoBehaviour
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, Teleporting }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 0.5f;

    // === 순간이동 설정 ===
    public float teleportCooldown = 5.0f; // 순간이동 쿨타임 (5초)
    public float teleportDistance = 2.0f; // 플레이어 주변 순간이동 거리 (반지름)
    public int maxTeleportAttempts = 10; // 💡 순간이동 시도 최대 횟수
    private float lastTeleportTime;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f; // 땅을 체크할 거리
    public float groundOffset = 0.1f;        // 땅 표면에서 적이 떠있는 높이

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    public int attackDamage = 3;

    private float lastAttackTime;

    // === 체력 설정 ===
    public int maxHP = 10;
    public int currentHP;

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        lastTeleportTime = Time.time;
        currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.value = (float)currentHP / maxHP;
        }

        enemyRenderer = GetComponent<Renderer>();

        // Rigidbody 설정
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        // 💡 텔레포트 이동 중에도 중력을 받을 수 있도록 isKinematic을 false로 설정합니다.
        // 💡 다만, 평소 이동 시에는 물리 기반 움직임이 방해되지 않도록 중력을 비활성화합니다.
        enemyRigidbody.isKinematic = false;
        enemyRigidbody.useGravity = false;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY; // 💡 기본적으로 Y축 이동은 프리즈 (SnapToGround에서만 허용)

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy();
        }

        StartCoroutine(CheckForTeleport());
    }

    void Update()
    {
        if (player == null) return;

        // 💡 리지드바디가 중력을 사용 중이면 (추락 중) Update 로직을 중단합니다.
        if (enemyRigidbody.useGravity)
        {
            // 추락 중 사망 처리 (선택 사항: DeadZone 진입과 동일)
            // if (transform.position.y < -10f) Die(); 
            return;
        }

        if (state == EnemyState.Teleporting) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange)
                    state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                // 💡 Trace 상태에서는 항상 지면에 부착 시도 (이동 전에 추락 방지)
                TryFallCheck();
                if (dist < attackRange)
                    state = EnemyState.Attack;
                else if (dist > traceRange)
                    state = EnemyState.Idle;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
                // 💡 Attack 상태에서는 항상 지면에 부착 시도
                TryFallCheck();
                if (dist > attackRange)
                    state = EnemyState.Trace;
                else
                {
                    TracePlayer();
                    AttackPlayer();
                }
                break;
            case EnemyState.Teleporting:
                break;
        }
    }

    // === 텔레포트 및 추락 로직 ===

    IEnumerator CheckForTeleport()
    {
        while (true)
        {
            yield return new WaitForSeconds(teleportCooldown);

            if (player != null && state != EnemyState.Teleporting && currentHP > 0)
            {
                TeleportToPlayerSide();
            }
        }
    }

    void TeleportToPlayerSide()
    {
        EnemyState previousState = state;
        state = EnemyState.Teleporting;

        Vector3 targetPosition = Vector3.zero;
        bool foundGround = false;

        // 💡 땅이 있는 위치를 찾을 때까지 시도
        for (int i = 0; i < maxTeleportAttempts; i++)
        {
            Vector3 randomCircle = Random.insideUnitCircle.normalized * teleportDistance;
            Vector3 potentialPosition = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // 💡 potentialPosition에 땅이 있는지 확인
            if (CheckGround(potentialPosition))
            {
                targetPosition = potentialPosition;
                foundGround = true;
                break;
            }
        }

        if (foundGround)
        {
            // 💡 지면을 찾았으므로 순간이동 및 지면에 부착
            enemyRigidbody.useGravity = false;
            enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            transform.position = targetPosition;
            SnapToGround();
            Debug.Log("적 (텔레포트): 지면을 찾고 순간이동 성공!");
        }
        else
        {
            // 💡 지면을 찾지 못하면 플레이어 위치로 순간이동 후 추락 로직 실행
            transform.position = player.position;
            Debug.Log("적 (텔레포트): 지면을 찾지 못함. 강제 추락 시작!");
            Fall();
        }

        lastTeleportTime = Time.time;
        // 💡 텔레포트가 완료된 후 이전 상태로 복귀
        state = previousState;
    }

    // 💡 Rigidbody를 kinematic=false, useGravity=true로 설정하여 추락 시작
    void Fall()
    {
        // Y축 Freeze 해제 및 중력 활성화
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        enemyRigidbody.useGravity = true;

        // 💡 추락 중에는 Trace, Attack 상태가 아닌 Idle 상태로 전환 (필요에 따라)
        state = EnemyState.Idle;
    }

    // 💡 현재 위치에 지면이 없으면 추락을 시작하는 함수
    void TryFallCheck()
    {
        // 💡 현재 발 밑에 땅이 없으면
        if (!CheckGround(transform.position))
        {
            Fall();
        }
        else
        {
            // 땅이 있으면 다시 중력/제한을 되돌림 (TeleportToPlayerSide에서 이미 처리됨)
            // SnapToGround()로 높이도 항상 조정
            SnapToGround();
        }
    }

    // Raycast를 사용하여 특정 위치에 지면이 있는지 확인 (VoxelCollapse 타일만)
    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
        // Raycast의 시작 위치를 적의 발보다 약간 위로 설정 (+ Vector3.up * 0.1f)
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            // VoxelCollapse 스크립트가 붙은 '유효한' 타일을 찾았는지 확인
            if (hit.collider.GetComponent<VoxelCollapse>() != null)
            {
                return true;
            }
        }
        return false;
    }

    // Raycast를 사용하여 적을 지면에 부착시키는 로직 (Y좌표 조정)
    void SnapToGround()
    {
        RaycastHit hit;
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

    // === 기존 로직 ===

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (hpSlider != null)
        {
            hpSlider.value = (float)currentHP / maxHP;
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        StopAllCoroutines();

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
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
        // 💡 추적 로직은 SnapToGround()가 포함된 TryFallCheck() 이후에 실행되어야 안전합니다.
        Vector3 dir = (player.position - transform.position).normalized;

        // 이동 로직 (지면 검사 후 X, Z 이동)
        // Note: 이 로직은 Rigidbody를 사용하지 않으므로, CheckGround를 통해 수동으로 지면을 확인합니다.
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround(); // 이동 후 지면에 재부착
        }
        // else: 이동할 곳에 땅이 없으면 움직이지 않습니다. (TryFallCheck에서 추락 처리)

        // 회전 로직
        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget);
    }

    void AttackPlayer()
    {
        SnapToGround();

        // 회전 로직
        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);

        // 공격 쿨타임 체크 후 플레이어에게 피해 적용
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            PlayerController playerScript = player.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(attackDamage);
                Debug.Log($"근접 공격! 플레이어에게 {attackDamage} 데미지를 입혔습니다.");
            }
        }
    }

    // DeadZone에 닿았는지 확인하는 Trigger 함수
    private void OnTriggerEnter(Collider other)
    {
        // 1. DeadZone에 닿았는지 확인
        if (other.CompareTag("DeadZone"))
        {
            Debug.Log("적이 DeadZone에 진입! 사망 처리합니다.");
            Die();
            return;
        }

        // 2. 투사체 충돌 감지 및 피해 적용
        if (other.GetComponent<Projectile>() != null)
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
}