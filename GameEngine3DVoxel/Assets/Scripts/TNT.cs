using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TNT : MonoBehaviour
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, RunAway } // Suicide 제거됨
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;

    // 💡 지면 부착 설정 변수 제거 (사용 안 함)

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float lastAttackTime;

    // === 체력 설정 ===
    public int maxHP = 10; // maxHP 10으로 고정
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

        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        // 💡 비행 유닛: Kinematic으로 고정하여 3D 이동을 제어합니다.
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

        // Kinematic이 해제될 일은 없지만, 구조 유지를 위해 검사 유지
        if (enemyRigidbody != null && !enemyRigidbody.isKinematic) return;

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
                else if (dist < attackRange)
                    state = EnemyState.Attack;
                else
                    TracePlayer(); // 💡 3D 추적 실행
                break;

            case EnemyState.Attack:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist > attackRange)
                    state = EnemyState.Trace;
                else
                    AttackPlayer(); // 💡 3D 조준 및 공격 실행
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer(); // 💡 3D 도망 실행
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

    void Die()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy();
        }
        Destroy(gameObject);
    }

    // 💡 TracePlayer 함수 수정: 3D 이동으로 변경
    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        // 💡 3D 이동: Y축을 포함한 모든 방향으로 움직입니다.
        Vector3 movement = dir * movespeed * Time.deltaTime;
        transform.position += movement;

        // 💡 3D 회전: 플레이어의 실제 3D 위치를 바라봅니다.
        transform.LookAt(player.position);
    }

    // 💡 AttackPlayer 함수 수정: 3D 조준 및 공격
    void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootProjectile();
        }

        // 💡 3D 회전: 플레이어의 실제 3D 위치를 바라봅니다.
        transform.LookAt(player.position);
    }

    // 💡 RunAwayFromPlayer 함수 수정: 3D 도망으로 변경
    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;

        float runSpeed = movespeed * 2f;

        // 💡 3D 도망: Y축을 포함한 모든 방향으로 도망갑니다.
        Vector3 movement = runDirection * runSpeed * Time.deltaTime;
        transform.position += movement;

        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    // 💡 지상 유닛 전용 함수들은 제거 (CheckGround, SnapToGround)
    /*
    bool CheckGround(Vector3 position) { return false; }
    void SnapToGround() { }
    */


    void ShootProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            // 💡 3D 회전은 AttackPlayer에서 이미 처리됨

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                // 투사체의 발사 방향은 이미 LookAt을 통해 정렬된 firePoint의 forward 방향입니다.
                // 그러나 안전을 위해 직접 계산하여 전달합니다.
                Vector3 dir = (player.position - firePoint.position).normalized;
                ep.SetDirection(dir);
            }
        }
    }

    // 💡 DeadZone에 닿았는지 확인하는 Trigger 함수
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Debug.Log("적이 DeadZone에 진입! 사망 처리합니다.");
            Die();
        }
    }
}