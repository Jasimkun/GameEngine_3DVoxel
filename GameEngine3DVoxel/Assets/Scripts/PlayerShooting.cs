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
    public Transform firePoint;        // 발사 지점
    // 🔻 1. [추가] 총 공격 쿨타임 및 상태 변수
    public float gunCooldown = 1.5f;   // 총 공격 쿨타임 (1.5초)
    private bool canShootGun = true;   // 총 발사 가능 여부

    // 📢 UI 이미지 변수 및 스프라이트 추가
    [Header("UI Settings")]
    public Image weaponImageUI;      // 현재 무기 상태를 표시할 Image UI 컴포넌트
    public Sprite gunSprite;         // 총 모드 (쿨타임 X) 스프라이트
    // 🔻 2. [추가] 총 쿨타임 스프라이트 변수
    public Sprite gunCooldownSprite; // 총 모드 (쿨타임 O) 스프라이트
    public Sprite swordReadySprite;  // 칼 모드 (쿨타임 X) 스프라이트
    public Sprite swordCooldownSprite; // 칼 모드 (쿨타임 O) 스프라이트

    private bool isMeleeMode = false; // 현재 칼 모드인지 추적 (false = 총, true = 칼)

    // 📢 인벤토리 UI에 연결할 무기 아이콘 추가
    [Header("Inventory UI")]
    public GameObject inventoryGunIcon;   // 인벤토리의 총 아이콘 (GameObject)
    public GameObject inventorySwordIcon; // 인벤토리의 칼 아이콘 (GameObject)

    // --- 칼 (Melee/Sword) 공격 변수 ---
    [Header("Sword Settings")]
    // 🔻 3. [수정] 칼 쿨타임 0.5초로 변경
    public float meleeCooldown = 0.5f;
    private bool canMeleeAttack = true; // 칼 공격 가능 여부
    public float meleeRange = 2.0f;     // 근접 공격 범위
    public GameObject swordEffectPrefab; // 이펙트 프리팹

    // --- 무기 모델 관리 변수 ---
    [Header("Weapon Models")]
    public GameObject gunModelPrefab;    // 총 모델 프리팹
    public GameObject swordModelPrefab;  // 칼 모델 프리팹
    private GameObject gunModelInstance;  // 생성된 총 모델 인스턴스
    private GameObject swordModelInstance; // 생성된 칼 모델 인스턴스

    // 📢 무기 모델 회전 오프셋 추가
    [Header("Weapon Model Rotation")]
    public Vector3 gunRotationOffset = new Vector3(0, 90, 0);
    public Vector3 swordRotationOffset = new Vector3(0, 0, 0);

    Camera cam;

    void Start()
    {
        cam = Camera.main;

        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerController == null) Debug.LogError("PlayerController 스크립트를 찾을 수 없습니다.");

        // 무기 모델 인스턴스화
        if (firePoint != null)
        {
            if (gunModelPrefab != null) gunModelInstance = Instantiate(gunModelPrefab, firePoint.position, Quaternion.identity, firePoint);
            if (swordModelPrefab != null) swordModelInstance = Instantiate(swordModelPrefab, firePoint.position, Quaternion.identity, firePoint);
        }

        ApplyModelRotation(); // 로컬 회전 적용

        // 🔻 4. [추가] 총 발사 가능 상태로 초기화
        canShootGun = true;

        // 시작 시 무기 UI 및 모델 초기화
        UpdateWeaponUI();
        UpdateWeaponModel();
        EnsureInventoryIconsActive();
    }

    void EnsureInventoryIconsActive()
    {
        if (inventoryGunIcon != null) inventoryGunIcon.SetActive(true);
        if (inventorySwordIcon != null) inventorySwordIcon.SetActive(true);
    }

    void ApplyModelRotation()
    {
        if (gunModelInstance != null) gunModelInstance.transform.localRotation = Quaternion.Euler(gunRotationOffset);
        if (swordModelInstance != null) swordModelInstance.transform.localRotation = Quaternion.Euler(swordRotationOffset);
    }

    void Update()
    {
        // Z 키 무기 전환
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isMeleeMode = !isMeleeMode;
            UpdateWeaponUI();
            UpdateWeaponModel();
        }

        // 마우스 좌클릭 공격 (게임 시간 정지 아닐 때)
        if (Time.timeScale > 0f && Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    private void UpdateWeaponModel()
    {
        if (gunModelInstance == null && swordModelInstance == null) return;
        bool showSword = isMeleeMode;
        if (swordModelInstance != null) swordModelInstance.SetActive(showSword);
        if (gunModelInstance != null) gunModelInstance.SetActive(!showSword);
    }

    // 🔻 5. [수정] UI 업데이트 함수 수정 (총 쿨타임 반영)
    private void UpdateWeaponUI()
    {
        if (weaponImageUI == null) return;

        if (isMeleeMode) // 칼 모드일 때
        {
            weaponImageUI.sprite = canMeleeAttack ? swordReadySprite : swordCooldownSprite;
        }
        else // 총 모드일 때
        {
            weaponImageUI.sprite = canShootGun ? gunSprite : gunCooldownSprite; // 쿨타임 상태에 따라 스프라이트 변경
        }
    }

    public void UpdateInventoryWeaponIcons()
    {
        if (inventoryGunIcon == null || inventorySwordIcon == null)
        {
            Debug.LogWarning("인벤토리 무기 아이콘이 연결되지 않았습니다.");
            return;
        }
        inventoryGunIcon.SetActive(true); // 항상 활성화
        inventorySwordIcon.SetActive(true); // 항상 활성화
    }

    // ===========================================
    // 통합 공격 로직 (좌클릭 시 호출)
    // ===========================================
    void Attack()
    {
        if (isMeleeMode) // 칼 모드
        {
            if (canMeleeAttack) MeleeAttack();
            // else Debug.Log("칼 쿨타임 중"); // 쿨타임 로그
        }
        else // 총 모드
        {
            // 🔻 6. [수정] 총 발사 가능 여부 확인
            if (canShootGun) ShootGun();
            // else Debug.Log("총 쿨타임 중"); // 쿨타임 로그
        }
    }

    // ===========================================
    // 근접 공격 (Melee/Sword) 로직
    // ===========================================
    void MeleeAttack()
    {
        if (playerController == null) return;

        canMeleeAttack = false;
        StartCoroutine(MeleeCooldownCoroutine()); // 쿨타임 시작 및 UI 업데이트

        // 이펙트 생성
        if (swordEffectPrefab != null)
        {
            Destroy(Instantiate(swordEffectPrefab, firePoint.position, firePoint.rotation), 2f);
        }

        // 범위 내 IDamageable 찾아서 공격
        Collider[] hitColliders = Physics.OverlapSphere(firePoint.position, meleeRange);
        foreach (var hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null && hitCollider.gameObject != this.gameObject) // 자기 자신 제외
            {
                damageable.TakeDamage(playerController.attackDamage);
            }
        }
    }

    IEnumerator MeleeCooldownCoroutine()
    {
        UpdateWeaponUI(); // 쿨타임 시작 UI 표시
        yield return new WaitForSeconds(meleeCooldown); // 0.5초 대기
        canMeleeAttack = true; // 공격 가능
        UpdateWeaponUI(); // 쿨타임 종료 UI 표시
    }


    // ===========================================
    // 원거리 공격 (Projectile/Gun) 로직
    // ===========================================

    // 🔻 7. [수정] 총 발사 함수 수정 (쿨타임 시작)
    void ShootGun()
    {
        if (playerController == null || projectilePrefab == null || firePoint == null || cam == null) return;

        canShootGun = false; // 발사 불가 상태로 변경
        StartCoroutine(GunCooldownCoroutine()); // 쿨타임 시작 및 UI 업데이트

        Vector3 direction = cam.transform.forward; // 카메라 정면 방향
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));

        Projectile projectileComponent = proj.GetComponent<Projectile>();
        if (projectileComponent != null)
        {
            projectileComponent.SetDamage(playerController.attackDamage);
        }
    }

    // 🔻 8. [추가] 총 쿨타임 코루틴
    IEnumerator GunCooldownCoroutine()
    {
        UpdateWeaponUI(); // 쿨타임 시작 UI 표시
        yield return new WaitForSeconds(gunCooldown); // 1.5초 대기
        canShootGun = true; // 발사 가능
        UpdateWeaponUI(); // 쿨타임 종료 UI 표시
    }


    // ===========================================
    // 근접 공격 범위 시각화 (유니티 에디터 전용)
    // ===========================================
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (firePoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(firePoint.position, meleeRange);
    }
#endif
}