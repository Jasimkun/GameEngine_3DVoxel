using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dash : MonoBehaviour
{
    // === 상태 열거형 ===
    // 💡 Wait 상태 추가: 공격 후 재탐색 대기 시간
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
    public float pushCooldown = 3f;     // 밀치기 재사용 대기시간 (쿨타임 체크용)
    private float lastPushTime;          // 마지막으로 밀친 시간

    // 💡 Wait 상태 설정
    public float waitDuration = 3f;     // 공격 후 대기/탐색 시간 (3초)
    private float waitEndTime;          // 대기 종료 시간

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f;
    public float groundOffset = 0.1f;

    // === 컴포넌트 및 상태 변수 ===
    public Slider hpSlider;
    private Transform player;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;

    public int maxHP = 5;
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

        lastPushTime = -pushCooldown; // 게임 시작 시 즉시 공격 가능

        // EnemyManager 연동 (있다면)
        // if (EnemyManager.Instance != null) { EnemyManager.Instance.RegisterEnemy(); }
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
                // 💡 Wait 상태로 전환하는 로직이 충돌 감지에서 처리되므로, 여기서는 Charge만 확인
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

            case EnemyState.Wait: // 💡 Wait 상태 처리
                if (Time.time >= waitEndTime)
                {
                    state = EnemyState.Trace; // 대기 시간 종료 -> 재탐색(Trace) 시작
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
        // EnemyManager 연동 (있다면)
        // if (EnemyManager.Instance != null) { EnemyManager.Instance.UnregisterEnemy(); }
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

        // 돌진 시 시각적 피드백
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

        // 추적 시 원래 색상 복구
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

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 💡 1. 쿨타임 확인: 현재 시간이 마지막 밀치기 시간 + 쿨타임보다 크거나 같을 때만 공격
        if (Time.time >= lastPushTime + pushCooldown)
        {
            if (hit.gameObject.CompareTag("Player"))
            {
                // 쿨타임 갱신
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

                // 💡 3. 공격 성공 후 Wait 상태로 전환 및 종료 시간 설정
                state = EnemyState.Wait;
                waitEndTime = Time.time + waitDuration;

                // 💡 4. 색상을 원래대로 돌려놓음 (돌진 종료 시각적 피드백)
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