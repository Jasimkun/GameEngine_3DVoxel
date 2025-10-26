using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// IDamageable 인터페이스 구현
public class Teleport : MonoBehaviour, IDamageable
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, Teleporting }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 0.5f;

    // === 순간이동 설정 ===
    public float teleportCooldown = 5.0f;
    public float teleportDistance = 2.0f;
    public int maxTeleportAttempts = 10;
    private float lastTeleportTime;

    // === 이펙트 프리팹 ===
    [Header("Effects")]
    public GameObject attackEffectPrefab;
    public GameObject teleportEffectPrefab;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f;
    public float groundOffset = 0.1f;

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

        // 📢 HP 슬라이더 초기화 수정!
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP; // 최대값 설정
            hpSlider.value = currentHP; // 현재값(실제값) 설정
        }

        enemyRenderer = GetComponent<Renderer>();

        // Rigidbody 설정
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        enemyRigidbody.isKinematic = false;
        enemyRigidbody.useGravity = false;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

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

    // ... (Update 및 다른 함수들은 이전과 동일하게 유지) ...

    void Update()
    {
        if (player == null) return;

        if (enemyRigidbody.useGravity)
        {
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
                TryFallCheck();
                if (dist < attackRange)
                    state = EnemyState.Attack;
                else if (dist > traceRange)
                    state = EnemyState.Idle;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
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

        if (teleportEffectPrefab != null)
        {
            GameObject effect = Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        Vector3 targetPosition = Vector3.zero;
        bool foundGround = false;

        for (int i = 0; i < maxTeleportAttempts; i++)
        {
            Vector3 randomCircle = Random.insideUnitCircle.normalized * teleportDistance;
            Vector3 potentialPosition = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (CheckGround(potentialPosition))
            {
                targetPosition = potentialPosition;
                foundGround = true;
                break;
            }
        }

        if (foundGround)
        {
            enemyRigidbody.useGravity = false;
            enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            transform.position = targetPosition;
            SnapToGround();
        }
        else
        {
            transform.position = player.position;
            Fall();
        }

        lastTeleportTime = Time.time;
        state = previousState;
    }

    void Fall()
    {
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        enemyRigidbody.useGravity = true;
        state = EnemyState.Idle;
    }

    void TryFallCheck()
    {
        if (!CheckGround(transform.position))
        {
            Fall();
        }
        else
        {
            SnapToGround();
        }
    }

    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
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
                transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
            }
        }
    }

    // 데미지 받는 함수
    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        // HP 슬라이더 업데이트 (비율 대신 실제 값 사용!)
        if (hpSlider != null)
        {
            // maxValue가 maxHP로 설정되었으므로 value에는 currentHP를 넣어야 함!
            hpSlider.value = currentHP;
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
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround();
        }

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
    }

    void AttackPlayer()
    {
        SnapToGround();

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            PlayerController playerScript = player.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                if (attackEffectPrefab != null)
                {
                    GameObject effect = Instantiate(attackEffectPrefab, transform.position, Quaternion.identity);
                    Destroy(effect, 1.5f);
                }

                playerScript.TakeDamage(attackDamage);
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
            // Projectile.cs에 GetDamage() 함수 필요 (임시로 1 사용)
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
}