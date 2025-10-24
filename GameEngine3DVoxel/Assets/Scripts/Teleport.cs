using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Teleport : MonoBehaviour
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 0.5f; // 💡 변경: 아주 작게 설정하여 거의 붙을 때까지 추적

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f; // 땅을 체크할 거리
    public float groundOffset = 0.1f;        // 땅 표면에서 적이 떠있는 높이

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    public int attackDamage = 3; // 💡 요청에 따라 공격 피해량을 3으로 설정

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
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        // EnemyManager에 자신을 등록
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

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange)
                    state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (dist < attackRange)
                    state = EnemyState.Attack;
                else if (dist > traceRange) // 추적 범위를 벗어나면 Idle
                    state = EnemyState.Idle;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
                // 💡 플레이어가 AttackRange 밖으로 벗어나면 Trace 상태로 복귀
                if (dist > attackRange)
                    state = EnemyState.Trace;
                else
                {
                    // 💡 변경: AttackRange 내에 있더라도 계속 추적 (붙어서 공격)
                    TracePlayer();
                    AttackPlayer();
                }
                break;
        }
    }

    // === 함수 정의 ===

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
        // 적이 파괴될 때 EnemyManager에 알립니다.
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
        Vector3 dir = (player.position - transform.position).normalized;

        // 이동 로직 (지면 검사 후 X, Z 이동)
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround();
        }

        // 회전 로직: 플레이어의 Y 좌표를 적의 Y 좌표로 고정
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

            // PlayerController가 플레이어 오브젝트에 있다고 가정합니다.
            PlayerController playerScript = player.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(attackDamage);
                Debug.Log($"근접 공격! 플레이어에게 {attackDamage} 데미지를 입혔습니다.");
            }
        }
    }

    // Raycast를 사용하여 특정 위치에 지면이 있는지 확인
    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
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

        // 💡 2. 투사체 충돌 감지 및 피해 적용 (Boom 로직 제거됨)
        if (other.GetComponent<Projectile>() != null)
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
}