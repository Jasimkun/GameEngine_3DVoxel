using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dash : MonoBehaviour
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Charge, RunAway, Wait }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 4f;
    public float traceRange = 10f;
    public float chargeRange = 2f;
    public float chargeSpeed = 10f;

    // 💡 충돌 설정
    public float pushForce = 5f;
    public int contactDamage = 5;
    public float pushCooldown = 3f;
    private float lastPushTime;

    // 💡 Wait 상태 설정
    public float waitDuration = 3f;
    private float waitEndTime;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f;
    public float groundOffset = 0.1f;

    // === 컴포넌트 및 상태 변수 ===
    public Slider hpSlider;
    private Transform player;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;

    public int maxHP = 10;
    public int currentHP;

    private float lastAttackTime;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentHP = maxHP;
        hpSlider.value = 1f;

        enemyRenderer = GetComponent<Renderer>();

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

        lastPushTime = -pushCooldown;

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy();
        }
    }

    void Update()
    {
        if (player == null || enemyRigidbody == null || !enemyRigidbody.isKinematic) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange)
                    state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (dist < chargeRange)
                    state = EnemyState.Charge;
                else
                    TracePlayer();
                break;

            case EnemyState.Charge:
                if (dist > chargeRange)
                    state = EnemyState.Trace;
                else
                    ChargePlayer();
                break;

            case EnemyState.Wait:
                if (Time.time >= waitEndTime)
                {
                    state = EnemyState.Trace;
                }
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer();
                float runawayDistance = 15f;
                if (Vector3.Distance(player.position, transform.position) > runawayDistance)
                    state = EnemyState.Idle;
                break;
        }
    }

    // === 상태 함수 ===

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
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
        }

        Destroy(gameObject);
    }

    void ChargePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * chargeSpeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround();
        }

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);

        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.red;
        }
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

        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = originalColor;
        }
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

    // === 지면 및 충돌 로직 ===

    // 💡 투사체 감지 및 피해 로직 추가
    void OnTriggerEnter(Collider other)
    {
        // 1. 투사체 충돌 감지: Projectile 스크립트를 가진 오브젝트와 충돌했는지 확인
        //    (Boom.cs도 투사체라면 || other.GetComponent<Boom>() != null 로 추가할 수 있습니다.)
        if (other.GetComponent<Projectile>() != null)
        {
            // Projectile 스크립트에서 데미지 값을 가져옵니다. (기존 Projectile 스크립트에는 damage 변수가 없으므로 1로 고정)
            // Projectile projectile = other.GetComponent<Projectile>();
            // int damage = projectile.damage; // 만약 Projectile.cs에 damage 변수가 있다면 이렇게 사용

            TakeDamage(1);

            // 💡 투사체는 충돌 후 파괴되어야 합니다.
            Destroy(other.gameObject);
        }

        // 2. DeadZone 충돌 처리 (기존 로직 유지)
        if (other.CompareTag("DeadZone"))
        {
            Die();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 💡 1. 쿨타임 확인 (근접 충돌 쿨타임)
        if (Time.time >= lastPushTime + pushCooldown)
        {
            if (hit.gameObject.CompareTag("Player"))
            {
                lastPushTime = Time.time;

                // 1. 플레이어에게 피해를 줍니다.
                PlayerController playerScript = hit.gameObject.GetComponent<PlayerController>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(contactDamage);
                }

                // 2. 플레이어를 밀쳐냅니다.
                Rigidbody playerRb = hit.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 pushDirection = (hit.gameObject.transform.position - transform.position).normalized;
                    playerRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                }

                // 3. 공격 성공 후 Wait 상태로 전환 및 종료 시간 설정
                state = EnemyState.Wait;
                waitEndTime = Time.time + waitDuration;

                // 4. 색상을 원래대로 돌려놓음 (돌진 종료 시각적 피드백)
                if (enemyRenderer != null)
                {
                    enemyRenderer.material.color = originalColor;
                }
            }
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
}