using System.Collections;
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

    // === 충돌 설정 ===
    public float pushForce = 5f;
    // 🔻 [수정] 기본 데미지 (Inspector에서 설정)
    public int baseContactDamage = 5;
    public float pushCooldown = 3f;
    private float lastPushTime;

    // Wait 상태 설정
    public float waitDuration = 3f;
    private float waitEndTime;

    // === 지면 부착 설정 ===
    public float groundCheckDistance = 1.0f;
    public float groundOffset = 0.1f;

    // === 체력 및 경험치 설정 ===
    // 🔻 [수정] 기본 체력 (Inspector에서 설정)
    public int baseMaxHP = 10;
    public int currentHP;
    public int experienceValue = 5; // 처치 시 지급할 경험치

    // 🔻 [추가] 레벨에 따라 계산된 최종 스탯
    private int calculatedMaxHP;
    private int calculatedDamage;

    // === 컴포넌트 및 상태 변수 ===
    public Slider hpSlider;
    private Transform player;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;
    private Coroutine blinkCoroutine;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 🔻 [수정] GameManager에서 현재 레벨을 가져와 스탯 계산
        int level = 1; // 기본 레벨
        if (GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;

            // 레벨에 맞춰 체력과 데미지 계산
            calculatedMaxHP = baseMaxHP + (level - 1) * GameManager.Instance.hpBonusPerLevel;
            calculatedDamage = baseContactDamage + (level - 1) * GameManager.Instance.damageBonusPerLevel;
        }
        else
        {
            // GameManager가 없을 경우(테스트 씬 등) 기본 스탯으로
            calculatedMaxHP = baseMaxHP;
            calculatedDamage = baseContactDamage;
        }

        // 계산된 체력으로 초기화
        currentHP = calculatedMaxHP;

        // HP 슬라이더 초기화
        if (hpSlider != null)
        {
            hpSlider.maxValue = calculatedMaxHP; // [수정]
            hpSlider.value = currentHP;
        }

        // [수정] Renderer 자식 포함 검색
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("Dash 몬스터가 Renderer를 찾지 못했습니다!", this.gameObject);
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
                TryFallCheck();
                if (dist < chargeRange) state = EnemyState.Charge;
                else TracePlayer();
                break;
            case EnemyState.Charge:
                TryFallCheck();
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

                // 🔻 [수정] 기본 데미지(contactDamage) 대신 계산된 데미지(calculatedDamage) 사용
                if (playerScript != null) { playerScript.TakeDamage(calculatedDamage); }

                Rigidbody playerRb = hit.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 pushDirection = (hit.gameObject.transform.position - transform.position).normalized;
                    playerRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                }

                state = EnemyState.Wait;
                waitEndTime = Time.time + waitDuration;
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

    void TryFallCheck()
    {
        if (!CheckGround(transform.position))
        {
            Fall();
        }
        else
        {
            SnapToGround(); // 땅 위에 있으면 isKinematic = true로 유지
        }
    }

    void Fall()
    {
        // isKinematic을 false로 바꿔 물리 엔진의 영향을 받도록 함
        enemyRigidbody.isKinematic = false;
        // 회전은 계속 고정
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        // 중력 사용 시작
        enemyRigidbody.useGravity = true;
        // 상태를 Idle로 변경 (떨어지는 동안 특별한 행동 X)
        state = EnemyState.Idle;
        Debug.Log(gameObject.name + " is falling!"); // 떨어짐 로그 (확인용)
    }
}