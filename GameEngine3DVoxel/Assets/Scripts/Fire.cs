using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Fire : MonoBehaviour
{
    // === 상태 열거형 (TNT와 동일하게 유지) ===
    public enum EnemyState { Idle, Trace, Attack, RunAway }
    public EnemyState state = EnemyState.Idle;

    // === 이동 및 추적 설정 (TNT와 동일하게 수정) ===
    public float movespeed = 2f;    // TNT: 2f
    public float traceRange = 15f;  // TNT: 15f
    public float attackRange = 6f;   // TNT: 6f (이 거리가 추적을 멈추는 거리입니다.)

    // === 공격 설정 (FireProjectile 로직은 유지, 쿨타임만 5초로 유지) ===
    public float attackCooldown = 5.0f; // 5초에 한 번 공격
    public GameObject fireProjectilePrefab;
    public Transform firePoint;

    private float lastAttackTime;

    // === 체력 설정 (TNT와 동일하게 수정) ===
    public int maxHP = 10;          // TNT: 10
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
        // 비행 유닛: Kinematic으로 고정하여 3D 이동을 제어합니다. (유지)
        enemyRigidbody.isKinematic = true;
        enemyRigidbody.useGravity = false;

        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }

        
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
                // TNT와 동일한 로직
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < traceRange)
                    state = EnemyState.Trace;
                break;

            case EnemyState.Trace:
                // TNT와 동일한 로직
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist < attackRange)
                    state = EnemyState.Attack;
                else
                    TracePlayer();
                break;

            case EnemyState.Attack:
                // TNT와 동일한 로직
                if (currentHP <= maxHP * 0.2f)
                    state = EnemyState.RunAway;
                else if (dist > attackRange)
                    state = EnemyState.Trace;
                else
                    AttackPlayer();
                break;

            case EnemyState.RunAway:
                RunAwayFromPlayer();
                // TNT와 동일한 로직
                float runawayDistance = 15f;
                if (Vector3.Distance(player.position, transform.position) > runawayDistance)
                    state = EnemyState.Idle;
                break;
        }
    }

    // === 함수 정의 (TNT와 동일한 로직) ===

    void TracePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 movement = dir * movespeed * Time.deltaTime;
        transform.position += movement;
        transform.LookAt(player.position);
    }

    void AttackPlayer()
    {
        // 쿨타임만 5초로 유지하고 발사 함수 호출
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            ShootFireProjectile();
        }
        transform.LookAt(player.position);
    }

    void RunAwayFromPlayer()
    {
        Vector3 traceDirection = (player.position - transform.position).normalized;
        Vector3 runDirection = -traceDirection;

        float runSpeed = movespeed * 2f;

        Vector3 movement = runDirection * runSpeed * Time.deltaTime;
        transform.position += movement;

        transform.rotation = Quaternion.LookRotation(runDirection);
    }

    void ShootFireProjectile()
    {
        if (fireProjectilePrefab != null && firePoint != null)
        {
            transform.LookAt(player.position);

            // FireProjectile을 생성하고 방향 설정
            GameObject proj = Instantiate(fireProjectilePrefab, firePoint.position, firePoint.rotation);
            FireProjectile fp = proj.GetComponent<FireProjectile>();
            if (fp != null)
            {
                Vector3 dir = (player.position - firePoint.position).normalized;
                fp.SetDirection(dir);
            }
        }
    }

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
        EnemyManager.Instance.UnregisterEnemy();
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Debug.Log("FireEnemy가 DeadZone에 진입! 사망 처리합니다.");
            Die();
            return;
        }

        if (other.GetComponent<Projectile>() != null)
        {
            TakeDamage(1);
            Destroy(other.gameObject);
        }
    }
}