using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 💡 스크립트 이름: TNT.cs
public class TNT : MonoBehaviour
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, RunAway }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 25f;       // 플레이어를 감지하는 최대 범위
    public float maxAttackRange = 20f;  // 폭탄을 던질 수 있는 최대 거리
    public float minAttackRange = 0.5f;   // 💡 수정: 플레이어가 이 거리(0.5m) 안으로 오면 도망침

    // === 폭탄 공격 설정 ===
    public GameObject bombVoxelPrefab;   // 던질 폭탄 Voxel 프리팹
    public Transform firePoint;          // 폭탄이 생성될 위치
    public float attackCooldown = 3f;    // 폭탄 투척 재사용 대기시간
    public int bombDamage = 5;           // 폭탄 피해량 5
    public float launchSpeed = 15f;      // 폭탄의 초기 투척 속도
    public float launchAngle = 45f;      // 폭탄이 날아갈 고정 각도 (포물선)

    private float lastAttackTime;

    // === 지면 부착 설정 (사용 안 함) ===
    public float groundCheckDistance = 1.0f;
    public float groundOffset = 0.1f;

    // === 체력 설정 ===
    public int maxHP = 10; // 최대 체력 10으로 고정
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
        hpSlider.value = 1f;

        enemyRenderer = GetComponent<Renderer>();

        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        // 공중에 떠 있도록 Kinematic 유지 및 중력 무시
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        // EnemyManager 연동 (있다면)
        // if (EnemyManager.Instance != null) { EnemyManager.Instance.RegisterEnemy(); }
    }

    void Update()
    {
        if (player == null || enemyRigidbody == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        switch (state)
        {
            case EnemyState.Idle:
                if (dist < traceRange)
                    state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                if (dist > maxAttackRange)
                {
                    TracePlayer(); // 너무 멀면 플레이어에게 다가감
                }
                else if (dist < minAttackRange)
                {
                    state = EnemyState.RunAway; // 너무 가까우면 도망감
                }
                else
                {
                    state = EnemyState.Attack; // 공격 범위 안 -> 공격
                }
                break;

            case EnemyState.Attack:
                if (dist < minAttackRange)
                    state = EnemyState.RunAway;
                else if (dist > maxAttackRange)
                    state = EnemyState.Trace;
                else
                    ThrowBomb(); // 폭탄 투척 함수 호출
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer();
                if (dist > maxAttackRange)
                    state = EnemyState.Attack; // 안전 거리 확보 후 공격 재개
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

    void Die()
    {
        // if (EnemyManager.Instance != null) { EnemyManager.Instance.UnregisterEnemy(); }
        Destroy(gameObject);
    }

    // 💡 포물선 폭탄 투척 로직
    void ThrowBomb()
    {
        // 쿨타임 체크
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return;
        }

        lastAttackTime = Time.time;

        // 1. 회전 로직: 플레이어 방향으로 회전
        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);

        if (bombVoxelPrefab != null && firePoint != null)
        {
            // 2. 폭탄 생성
            GameObject bomb = Instantiate(bombVoxelPrefab, firePoint.position, Quaternion.identity);

            Rigidbody bombRb = bomb.GetComponent<Rigidbody>();
            if (bombRb == null)
            {
                Debug.LogError("BombVoxel Prefab requires a Rigidbody component.");
                Destroy(bomb);
                return;
            }

            // 4. 투척 속도 계산 (포물선)
            Vector3 velocity = CalculateParabolicLaunchVelocity(firePoint.position, player.position, launchSpeed, launchAngle);

            // 5. 폭탄 발사
            bombRb.velocity = velocity;
        }
    }

    // 💡 포물선 투척 속도 계산 함수 (로직 유지)
    Vector3 CalculateParabolicLaunchVelocity(Vector3 startPos, Vector3 targetPos, float speed, float angleDegrees)
    {
        Vector3 horizontalTarget = new Vector3(targetPos.x, startPos.y, targetPos.z);
        float distance = Vector3.Distance(startPos, horizontalTarget);
        float heightDifference = targetPos.y - startPos.y;
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        float gravity = Physics.gravity.magnitude;
        float v0 = speed;
        float timeToTarget = distance / (v0 * Mathf.Cos(angleRad));

        if (timeToTarget < 0.01f || float.IsNaN(timeToTarget))
        {
            return (targetPos - startPos).normalized * v0;
        }

        Vector3 horizontalVelocity = (horizontalTarget - startPos).normalized * (v0 * Mathf.Cos(angleRad));
        float verticalVelocity = v0 * Mathf.Sin(angleRad) + 0.5f * gravity * timeToTarget;

        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    // 💡 TracePlayer 함수: Y축만 제외하고 이동
    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        // Y축을 제외하고 수평 이동만 합니다.
        Vector3 movement = new Vector3(dir.x, 0, dir.z) * movespeed * Time.deltaTime;
        transform.position += movement;

        Vector3 lookTarget = player.position;
        lookTarget.y = transform.position.y;
        transform.LookAt(lookTarget);
    }

    // 💡 RunAwayFromPlayer 함수: Y축만 제외하고 이동
    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;

        float runSpeed = movespeed * 2f;

        // Y축을 제외하고 수평 이동만 합니다.
        Vector3 movement = new Vector3(runDirection.x, 0, runDirection.z) * runSpeed * Time.deltaTime;
        transform.position += movement;

        transform.rotation = Quaternion.LookRotation(runDirection);
    }
}