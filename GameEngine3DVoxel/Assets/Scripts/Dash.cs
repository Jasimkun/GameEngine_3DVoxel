using System.Collections; // 👈 IEnumerator를 위한 네임스페이스
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// IDamageable 인터페이스 구현
public class Dash : MonoBehaviour, IDamageable
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Charge, RunAway, Wait }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 4f;
    public float traceRange = 10f;
    public float chargeRange = 2f;
    public float chargeSpeed = 10f;

    // 충돌 설정
    public float pushForce = 5f;
    public int contactDamage = 5;
    public float pushCooldown = 3f;
    private float lastPushTime;

    // Wait 상태 설정
    public float waitDuration = 3f;
    private float waitEndTime;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f;
    public float groundOffset = 0.1f;

    // === 체력 및 경험치 설정 ===
    public int maxHP = 10;
    public int currentHP;
    public int experienceValue = 5; // 처치 시 지급할 경험치

    // === 컴포넌트 및 상태 변수 ===
    public Slider hpSlider;
    private Transform player;
    private Renderer enemyRenderer; // 돌진 시 색상 변경용
    private Color originalColor;   // 돌진 시 색상 변경용
    private Rigidbody enemyRigidbody;

    private Coroutine blinkCoroutine; // 👈 깜빡임 코루틴 변수


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        currentHP = maxHP;

        // HP 슬라이더 초기화
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        // Renderer 및 색상 초기화 (돌진 시 색상 변경용)
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        // Rigidbody 설정
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null) { enemyRigidbody = gameObject.AddComponent<Rigidbody>(); }
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        lastPushTime = -pushCooldown;

        // EnemyManager 등록
        if (EnemyManager.Instance != null) { EnemyManager.Instance.RegisterEnemy(); }
    }

    void Update()
    {
        if (player == null || enemyRigidbody == null || !enemyRigidbody.isKinematic) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange) state = EnemyState.Trace;
                break;
            case EnemyState.Trace:
                if (dist < chargeRange) state = EnemyState.Charge;
                else TracePlayer();
                break;
            case EnemyState.Charge:
                if (dist > chargeRange) state = EnemyState.Trace;
                else ChargePlayer();
                break;
            case EnemyState.Wait:
                if (Time.time >= waitEndTime) state = EnemyState.Trace;
                break;
            case EnemyState.RunAway: // 현재 사용되지 않음
                RunAwayFromPlayer();
                float runawayDistance = 15f;
                if (Vector3.Distance(player.position, transform.position) > runawayDistance) state = EnemyState.Idle;
                break;
        }
    }

    // === 상태 함수 ===

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return;

        // 👈 피격 시 코루틴 호출
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;

        if (hpSlider != null)
        {
            hpSlider.value = currentHP;
        }

        if (currentHP <= 0)
        {
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.EnemyDefeated(experienceValue);
            }
            Die();
        }
    }

    // 👈 단순화된 BlinkEffect 코루틴
    private IEnumerator BlinkEffect()
    {
        if (enemyRenderer == null) yield break;

        float blinkDuration = 0.1f;

        // 빨간색으로 변경
        enemyRenderer.material.color = Color.red;

        // 0.1초 대기
        yield return new WaitForSeconds(blinkDuration);

        // ⭐️ 무조건 원래 색상으로 복구
        enemyRenderer.material.color = originalColor;

        // 코루틴 참조 제거
        blinkCoroutine = null;
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

        // 🔻 색상 변경 로직 주석 처리 🔻
        /*
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.red;
        }
        */
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

        // 🔻 색상 변경 로직 주석 처리 🔻
        /*
        if (enemyRenderer != null && state != EnemyState.Charge)
        {
            enemyRenderer.material.color = originalColor;
        }
        */
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

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Die();
            return;
        }

        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Time.time >= lastPushTime + pushCooldown)
        {
            if (hit.gameObject.CompareTag("Player"))
            {
                lastPushTime = Time.time;

                PlayerController playerScript = hit.gameObject.GetComponent<PlayerController>();
                if (playerScript != null) { playerScript.TakeDamage(contactDamage); }

                Rigidbody playerRb = hit.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 pushDirection = (hit.gameObject.transform.position - transform.position).normalized;
                    playerRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                }

                state = EnemyState.Wait;
                waitEndTime = Time.time + waitDuration;

                // 🔻 색상 변경 로직 주석 처리 🔻
                /*
                if (enemyRenderer != null) { enemyRenderer.material.color = originalColor; }
                */
            }
        }
    }

    bool CheckGround(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            if (hit.collider.GetComponent<VoxelCollapse>() != null) { return true; }
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