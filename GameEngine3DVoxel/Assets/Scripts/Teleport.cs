using System.Collections; // 👈 [추가] IEnumerator (코루틴) 위해 필요
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
    // 🔻 [수정] 기본 공격력 (Inspector에서 설정)
    public int baseAttackDamage = 3;
    private float lastAttackTime;

    // === 체력 및 경험치 설정 ===
    // 🔻 [수정] 기본 체력 (Inspector에서 설정)
    public int baseMaxHP = 10;
    public int currentHP;
    public int experienceValue = 5; // 처치 시 경험치

    // 🔻 [추가] 레벨별 최종 스탯
    private int calculatedMaxHP;
    private int calculatedDamage;

    // === 컴포넌트 ===
    private Transform player;
    public Slider hpSlider;
    private Renderer enemyRenderer;
    private Color originalColor;
    private Rigidbody enemyRigidbody;
    private Coroutine blinkCoroutine;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastAttackTime = -attackCooldown;
        lastTeleportTime = Time.time; // 순간이동 쿨타임 시작

        // 🔻 [수정] GameManager에서 현재 레벨을 가져와 스탯 계산
        int level = 1; // 기본 레벨
        if (GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;

            // 레벨에 맞춰 체력과 데미지 계산
            calculatedMaxHP = baseMaxHP + (level - 1) * GameManager.Instance.hpBonusPerLevel;
            calculatedDamage = baseAttackDamage + (level - 1) * GameManager.Instance.damageBonusPerLevel;
        }
        else
        {
            // GameManager가 없을 경우 기본 스탯으로
            calculatedMaxHP = baseMaxHP;
            calculatedDamage = baseAttackDamage;
            Debug.LogWarning("GameManager Instance not found. Using base stats for Teleport.");
        }

        // 계산된 체력으로 초기화
        currentHP = calculatedMaxHP;

        // HP 슬라이더 초기화
        if (hpSlider != null)
        {
            hpSlider.maxValue = calculatedMaxHP; // [수정]
            hpSlider.value = currentHP;
        }

        // Renderer 초기화 (자식 포함)
        enemyRenderer = GetComponentInChildren<Renderer>(true);
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("Teleport 몬스터가 Renderer를 찾지 못했습니다!", this.gameObject);
        }

        // Rigidbody 설정 (isKinematic = true 유지)
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null) { enemyRigidbody = gameObject.AddComponent<Rigidbody>(); }
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        // EnemyManager 등록
        if (EnemyManager.Instance != null) { EnemyManager.Instance.RegisterEnemy(); }

        StartCoroutine(CheckForTeleport());
    }

    void Update()
    {
        if (player == null) return;
        // 떨어지는 중이면 로직 중지 (Fall()에서 isKinematic=false, useGravity=true로 바뀜)
        if (enemyRigidbody.useGravity) return;
        if (state == EnemyState.Teleporting) return; // 순간이동 중이면 로직 중지

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange) state = EnemyState.Trace;
                break;
            case EnemyState.Trace:
                TryFallCheck(); // 땅 체크
                if (dist < attackRange) state = EnemyState.Attack;
                else if (dist > traceRange) state = EnemyState.Idle; // 추적 범위 벗어나면 Idle
                else TracePlayer(); // 추적
                break;
            case EnemyState.Attack:
                TryFallCheck(); // 땅 체크
                if (dist > attackRange) state = EnemyState.Trace; // 공격 범위 벗어나면 추적
                else AttackPlayer(); // 공격 (이동은 안 함)
                break;
            case EnemyState.Teleporting:
                // 순간이동 중에는 아무것도 안 함
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
                float dist = Vector3.Distance(player.position, transform.position);
                // 추적 범위(traceRange) 밖에 있을 때만 순간이동
                if (dist > traceRange)
                {
                    TeleportToPlayerSide();
                }
            }
        }
    }

    void TeleportToPlayerSide()
    {
        EnemyState previousState = state; // 원래 상태 저장
        state = EnemyState.Teleporting; // 상태 변경

        // 순간이동 시작 이펙트
        if (teleportEffectPrefab != null)
        {
            GameObject effect = Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // 2초 뒤 파괴
        }

        Vector3 targetPosition = Vector3.zero;
        bool foundGround = false;

        // 플레이어 주변 랜덤 위치 탐색 (땅 위만)
        for (int i = 0; i < maxTeleportAttempts; i++)
        {
            Vector3 randomCircle = Random.insideUnitCircle.normalized * teleportDistance;
            Vector3 potentialPosition = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            // 땅 체크 (VoxelCollapse 타일 위인지)
            if (CheckGround(potentialPosition))
            {
                targetPosition = potentialPosition;
                foundGround = true;
                break;
            }
        }

        if (foundGround)
        {
            // 땅 찾았으면 해당 위치로 이동 후 땅에 붙임
            transform.position = targetPosition;
            SnapToGround(); // 땅 높이에 맞춤
            // 도착 이펙트 (시작과 같은 이펙트 사용)
            if (teleportEffectPrefab != null)
            {
                GameObject effect = Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
        else
        {
            // 땅 못 찾으면 플레이어 위치로 이동 후 떨어짐 (Fall)
            Debug.LogWarning("Teleport: Valid ground not found near player. Falling.");
            transform.position = player.position + Vector3.up * 0.5f; // 약간 위에서 시작
            Fall(); // 떨어지기 시작
        }

        lastTeleportTime = Time.time; // 순간이동 쿨타임 초기화
        state = previousState; // 원래 상태로 복귀
    }

    void Fall() // 떨어지기 시작할 때 호출
    {
        enemyRigidbody.isKinematic = false; // 물리 엔진 적용 시작
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation; // 회전은 계속 고정
        enemyRigidbody.useGravity = true; // 중력 적용 시작
        state = EnemyState.Idle; // 상태 초기화
    }

    void TryFallCheck() // 매 프레임 땅 위에 있는지 확인
    {
        if (!CheckGround(transform.position))
        {
            Fall(); // 땅 없으면 떨어짐
        }
        else
        {
            SnapToGround(); // 땅 있으면 붙음
        }
    }

    bool CheckGround(Vector3 position) // 특정 위치 아래에 땅(VoxelCollapse)이 있는지 확인
    {
        RaycastHit hit;
        // 약간 위에서 아래로 Ray 발사
        if (Physics.Raycast(position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            // VoxelCollapse 컴포넌트가 있으면 true 반환
            if (hit.collider.GetComponent<VoxelCollapse>() != null) { return true; }
        }
        return false;
    }

    void SnapToGround() // 현재 위치 바로 아래 땅 높이에 맞춰 Y좌표 조절
    {
        // 떨어지는 중이었다면 다시 isKinematic 상태로 복귀
        if (!enemyRigidbody.isKinematic)
        {
            enemyRigidbody.isKinematic = true;
            enemyRigidbody.useGravity = false;
            enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance))
        {
            VoxelCollapse tileScript = hit.collider.GetComponent<VoxelCollapse>();
            if (tileScript != null)
            {
                // 땅 높이 + 약간의 오프셋으로 Y좌표 설정
                transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
            }
        }
    }

    // === 함수 정의 ===

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return; // 중복 사망 방지

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;

        if (hpSlider != null)
        {
            // [수정] 값 자체로 업데이트
            hpSlider.value = currentHP;
        }

        if (currentHP <= 0)
        {
            // 경험치 지급 (Die 함수보다 먼저)
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.EnemyDefeated(experienceValue);
            }
            Die();
        }
    }

    private IEnumerator BlinkEffect()
    {
        if (enemyRenderer == null) yield break;
        float blinkDuration = 0.1f;
        enemyRenderer.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);
        enemyRenderer.material.color = originalColor;
        blinkCoroutine = null;
    }

    void Die()
    {
        currentHP = 0; // 확실하게 0으로

        // 실행 중인 모든 코루틴 중지 (CheckForTeleport 포함)
        StopAllCoroutines();

        // EnemyManager에 사망 보고
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
        }

        // 오브젝트 파괴
        Destroy(gameObject);
    }

    void TracePlayer() // 플레이어 추적 함수
    {
        Vector3 dir = (player.position - transform.position).normalized;
        // X, Z 축으로만 이동
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + movement;

        // 다음 위치에 땅이 있을 때만 이동
        if (CheckGround(nextPosition))
        {
            transform.position = nextPosition;
            SnapToGround(); // 땅 높이에 맞춤
        }

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y; // Y축 회전 고정
        transform.LookAt(lookTarget);
    }

    void AttackPlayer() // 플레이어 공격 함수
    {
        SnapToGround(); // 땅에 붙어 있도록

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y; // Y축 회전 고정
        transform.LookAt(lookTarget);

        // 공격 쿨타임 확인
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time; // 쿨타임 초기화
            PlayerController playerScript = player.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                // 공격 이펙트 생성
                if (attackEffectPrefab != null)
                {
                    GameObject effect = Instantiate(attackEffectPrefab, transform.position + transform.forward * 0.5f, Quaternion.identity); // 약간 앞에서 생성
                    Destroy(effect, 1.5f);
                }
                // 🔻 [수정] 계산된 데미지 사용
                playerScript.TakeDamage(calculatedDamage);
            }
        }
    }

    // DeadZone 및 투사체 충돌 처리
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Die();
            return; // DeadZone이면 아래 로직 실행 안 함
        }

        // 플레이어 투사체 충돌은 Projectile.cs에서 처리하므로 주석 처리
        /*
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile != null)
        {
            TakeDamage(1); // 임시 데미지
            Destroy(other.gameObject);
        }
        */
    }
}