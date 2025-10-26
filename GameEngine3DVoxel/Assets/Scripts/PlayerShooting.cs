using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트를 사용하기 위해 추가

public class PlayerShooting : MonoBehaviour
{
    // 📢 PlayerController 참조 (데미지 정보를 가져오기 위함)
    public PlayerController playerController;

    // --- 총 (Projectile/Gun) 공격 변수 ---
    [Header("Gun Settings")]
    public GameObject projectilePrefab; // 총알 프리팹
    public Transform firePoint;        // 발사 지점

    // 📢 UI 이미지 변수 및 스프라이트 추가
    [Header("UI Settings")]
    public Image weaponImageUI;      // 현재 무기 상태를 표시할 Image UI 컴포넌트
    public Sprite gunSprite;         // 총 모드일 때 사용할 스프라이트
    public Sprite swordReadySprite;  // 칼 모드 (쿨타임 X)일 때 사용할 스프라이트
    public Sprite swordCooldownSprite; // 칼 모드 (쿨타임 O)일 때 사용할 스프라이트

    private bool isMeleeMode = false; // 📢 현재 칼 모드인지 추적 (false = 총, true = 칼)

    // --- 칼 (Melee/Sword) 공격 변수 ---
    [Header("Sword Settings")]
    public float meleeCooldown = 1f;    // 칼 공격 쿨타임 (1초)
    private bool canMeleeAttack = true; // 칼 공격 가능 여부
    public float meleeRange = 2.0f;     // 근접 공격 범위 (유니티에서 조정 가능)

    // --- 무기 모델 관리 변수 ---
    [Header("Weapon Models")]
    public GameObject gunModelPrefab;    // 총 모델 프리팹
    public GameObject swordModelPrefab;  // 칼 모델 프리팹
    private GameObject gunModelInstance;  // 생성된 총 모델 인스턴스
    private GameObject swordModelInstance; // 생성된 칼 모델 인스턴스

    Camera cam;

    void Start()
    {
        cam = Camera.main;

        // PlayerController 참조를 찾습니다.
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogError("PlayerController 스크립트를 찾을 수 없습니다. 데미지 정보를 가져올 수 없습니다.");
        }

        // 📢 무기 모델 인스턴스화 및 초기 위치 설정
        if (firePoint != null)
        {
            if (gunModelPrefab != null)
            {
                // 총 모델을 FirePoint의 자식으로 생성
                gunModelInstance = Instantiate(gunModelPrefab, firePoint.position, firePoint.rotation, firePoint);
            }
            if (swordModelPrefab != null)
            {
                // 칼 모델을 FirePoint의 자식으로 생성
                swordModelInstance = Instantiate(swordModelPrefab, firePoint.position, firePoint.rotation, firePoint);
            }
        }

        // 📢 시작 시 무기 UI 및 모델을 총 모드로 초기화합니다.
        UpdateWeaponUI();
        UpdateWeaponModel();
    }

    void Update()
    {
        // 📢 Z 키로 무기 전환 로직 (칼 <-> 총)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isMeleeMode = !isMeleeMode;
            UpdateWeaponUI();
            UpdateWeaponModel(); // 📢 모델 전환 함수 호출
        }

        // 📢 마우스 좌클릭 (0) - 현재 모드에 따라 공격 실행 (총 또는 칼)
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    // 📢 무기 모델 활성화/비활성화 함수
    private void UpdateWeaponModel()
    {
        // 널 체크
        if (gunModelInstance == null && swordModelInstance == null) return;

        if (isMeleeMode)
        {
            // 칼 모드: 칼 모델 활성화, 총 모델 비활성화
            if (swordModelInstance != null) swordModelInstance.SetActive(true);
            if (gunModelInstance != null) gunModelInstance.SetActive(false);
        }
        else
        {
            // 총 모드: 총 모델 활성화, 칼 모델 비활성화
            if (gunModelInstance != null) gunModelInstance.SetActive(true);
            if (swordModelInstance != null) swordModelInstance.SetActive(false);
        }
    }

    // 📢 무기 전환 및 쿨타임 상태를 반영하여 UI 업데이트
    private void UpdateWeaponUI()
    {
        if (weaponImageUI == null) return;

        if (isMeleeMode)
        {
            // 칼 모드: 쿨타임 상태에 따라 다른 스프라이트 사용
            weaponImageUI.sprite = canMeleeAttack ? swordReadySprite : swordCooldownSprite;
        }
        else
        {
            // 총 모드: 총 스프라이트 사용
            weaponImageUI.sprite = gunSprite;
        }
    }

    // ===========================================
    // 통합 공격 로직 (좌클릭 시 호출)
    // ===========================================
    void Attack()
    {
        if (isMeleeMode)
        {
            if (canMeleeAttack)
            {
                MeleeAttack();
            }
            else
            {
                Debug.Log("칼 공격 쿨타임 중입니다.");
            }
        }
        else
        {
            ShootGun();
        }
    }


    // ===========================================
    // 근접 공격 (Melee/Sword) 로직 - 📢 디버그 추가됨!
    // ===========================================

    void MeleeAttack()
    {
        if (playerController == null) return;

        canMeleeAttack = false;
        StartCoroutine(MeleeCooldownCoroutine());

        Vector3 origin = firePoint.position;

        // 📢 디버그 1: 공격 시도 확인
        Debug.Log("📢 칼 공격 시도! 위치: " + origin + ", 범위: " + meleeRange);


        Collider[] hitColliders = Physics.OverlapSphere(origin, meleeRange);

        // 📢 디버그 2: 감지된 오브젝트 수 확인
        Debug.Log("📢 감지된 콜라이더 수: " + hitColliders.Length);

        foreach (var hitCollider in hitColliders)
        {
            // 📢 디버그 3: 감지된 오브젝트 이름과 태그 확인
            Debug.Log("    - 감지된 오브젝트: " + hitCollider.name + ", 태그: " + hitCollider.tag);

            if (hitCollider.CompareTag("Enemy"))
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(playerController.attackDamage);
                    Debug.Log("✅ 칼 공격 성공: " + hitCollider.name + "에게 " + playerController.attackDamage + " 피해를 입혔습니다.");
                }
                else
                {
                    // 📢 디버그 4: Enemy 태그는 있지만 Enemy 스크립트가 없는 경우
                    Debug.LogWarning("❌ Enemy 태그는 있지만 Enemy 스크립트가 없습니다: " + hitCollider.name);
                }
            }
        }
    }

    IEnumerator MeleeCooldownCoroutine()
    {
        UpdateWeaponUI();
        yield return new WaitForSeconds(meleeCooldown);
        canMeleeAttack = true;
        UpdateWeaponUI();
    }


    // ===========================================
    // 원거리 공격 (Projectile/Gun) 로직
    // ===========================================

    void ShootGun()
    {
        if (playerController == null) return;

        Vector3 direction = cam.transform.forward;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        Projectile projectileComponent = proj.GetComponent<Projectile>();
        if (projectileComponent != null)
        {
            projectileComponent.SetDamage(playerController.attackDamage);
            Debug.Log("총 공격: 투사체에 " + playerController.attackDamage + " 데미지를 설정했습니다.");
        }
    }

    // ===========================================
    // 📢 근접 공격 범위 시각화 (유니티 에디터 전용)
    // ===========================================
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (firePoint == null) return;

        // 빨간색 와이어 구체로 근접 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(firePoint.position, meleeRange);
    }
#endif
}