using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TNT : MonoBehaviour, IDamageable
{
    // === 상태 열거형 ===
    public enum EnemyState { Idle, Trace, Attack, RunAway }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 ===
    public float movespeed = 2f;
    public float traceRange = 15f;
    public float attackRange = 6f;

    // === 공격 설정 ===
    public float attackCooldown = 1.5f;
    public GameObject projectilePrefab;
    public Transform firePoint;

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

        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        // 비행 유닛: Kinematic으로 고정하여 3D 이동을 제어합니다.
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
                    TracePlayer();
                break;

            case EnemyState.Attack:
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist > attackRange)
                    state = EnemyState.Trace;
                else
                    AttackPlayer();
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer();
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

    // 💡 TracePlayer 함수: 3D 이동으로 변경
    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        Vector3 movement = dir * movespeed * Time.deltaTime;
        transform.position += movement;

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

        transform.LookAt(player.position);
    }

    // 💡 RunAwayFromPlayer 함수 수정: 3D 도망으로 변경
    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;

        float runSpeed = movespeed * 2f;

        Vector3 movement = runDirection * runSpeed * Time.deltaTime;
        transform.position += movement;

        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    void ShootProjectile()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            transform.LookAt(player.position);

            GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                Vector3 dir = (player.position - firePoint.position).normalized;
                ep.SetDirection(dir);
            }
        }
    }

    // 💡 DeadZone에 닿았는지 확인하는 Trigger 함수
    private void OnTriggerEnter(Collider other)
    {
        // 1. DeadZone에 닿았는지 확인
        if (other.CompareTag("DeadZone"))
        {
            //Debug.Log("적이 DeadZone에 진입! 사망 처리합니다.");
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