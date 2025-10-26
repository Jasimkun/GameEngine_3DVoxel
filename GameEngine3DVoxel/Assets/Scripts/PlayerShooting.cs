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

    // 📢 인벤토리 UI에 연결할 무기 아이콘 추가
    [Header("Inventory UI")]
    public GameObject inventoryGunIcon;   // 인벤토리의 총 아이콘 (GameObject)
    public GameObject inventorySwordIcon; // 인벤토리의 칼 아이콘 (GameObject)

    // --- 칼 (Melee/Sword) 공격 변수 ---
    [Header("Sword Settings")]
    public float meleeCooldown = 1f;    // 칼 공격 쿨타임 (1초)
    private bool canMeleeAttack = true; // 칼 공격 가능 여부
    public float meleeRange = 2.0f;     // 근접 공격 범위 (유니티에서 조정 가능)
    public GameObject swordEffectPrefab; // 📢 이펙트 프리팹 변수

    // --- 무기 모델 관리 변수 ---
    [Header("Weapon Models")]
    public GameObject gunModelPrefab;    // 총 모델 프리팹
    public GameObject swordModelPrefab;  // 칼 모델 프리팹
    private GameObject gunModelInstance;  // 생성된 총 모델 인스턴스
    private GameObject swordModelInstance; // 생성된 칼 모델 인스턴스

    // 📢 무기 모델 회전 오프셋 추가
    [Header("Weapon Model Rotation")]
    public Vector3 gunRotationOffset = new Vector3(0, 90, 0); // 인스펙터에서 조정 가능
    public Vector3 swordRotationOffset = new Vector3(0, 0, 0); // 인스펙터에서 조정 가능

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

        // 무기 모델 인스턴스화 및 로컬 회전 초기화
        if (firePoint != null)
        {
            if (gunModelPrefab != null)
            {
                gunModelInstance = Instantiate(gunModelPrefab, firePoint.position, Quaternion.identity, firePoint);
            }
            if (swordModelPrefab != null)
            {
                swordModelInstance = Instantiate(swordModelPrefab, firePoint.position, Quaternion.identity, firePoint);
            }
        }

        // 로컬 회전 오프셋을 적용하여 무기 기울기 설정
        ApplyModelRotation();

        // 시작 시 무기 UI 및 모델을 총 모드로 초기화합니다.
        UpdateWeaponUI();
        UpdateWeaponModel();
    }

    // 로컬 회전 오프셋 적용 함수
    void ApplyModelRotation()
    {
        if (gunModelInstance != null)
        {
            gunModelInstance.transform.localRotation = Quaternion.Euler(gunRotationOffset);
        }
        if (swordModelInstance != null)
        {
            swordModelInstance.transform.localRotation = Quaternion.Euler(swordRotationOffset);
        }
    }


    void Update()
    {
        // Z 키로 무기 전환 로직 (칼 <-> 총)
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isMeleeMode = !isMeleeMode;
            UpdateWeaponUI();
            UpdateWeaponModel();
        }

        // 마우스 좌클릭 (0) - 현재 모드에 따라 공격 실행 (총 또는 칼)
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    // 무기 모델 활성화/비활성화 함수
    private void UpdateWeaponModel()
    {
        if (gunModelInstance == null && swordModelInstance == null) return;

        if (isMeleeMode)
        {
            if (swordModelInstance != null) swordModelInstance.SetActive(true);
            if (gunModelInstance != null) gunModelInstance.SetActive(false);
        }
        else
        {
            if (gunModelInstance != null) gunModelInstance.SetActive(true);
            if (swordModelInstance != null) swordModelInstance.SetActive(false);
        }
    }

    // 무기 전환 및 쿨타임 상태를 반영하여 UI 업데이트
    private void UpdateWeaponUI()
    {
        if (weaponImageUI == null) return;

        if (isMeleeMode)
        {
            weaponImageUI.sprite = canMeleeAttack ? swordReadySprite : swordCooldownSprite;
        }
        else
        {
            weaponImageUI.sprite = gunSprite;
        }
    }

    // 📢 인벤토리가 열릴 때 호출될 함수 (새로 추가!)
    public void UpdateInventoryWeaponIcons()
    {
        // 널 체크
        if (inventoryGunIcon == null || inventorySwordIcon == null)
        {
            Debug.Log("인벤토리 무기 아이콘이 연결되지 않았습니다.");
            return;
        }

        if (isMeleeMode)
        {
            // 칼 모드일 때: 칼 아이콘 켜기, 총 아이콘 끄기
            inventorySwordIcon.SetActive(true);
            inventoryGunIcon.SetActive(false);
        }
        else
        {
            // 총 모드일 때: 총 아이콘 켜기, 칼 아이콘 끄기
            inventoryGunIcon.SetActive(true);
            inventorySwordIcon.SetActive(false);
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
                //Debug.Log("칼 공격 쿨타임 중입니다.");
            }
        }
        else
        {
            ShootGun();
        }
    }


    // ===========================================
    // 근접 공격 (Melee/Sword) 로직 - 📢 IDamageable로 수정됨!
    // ===========================================
    void MeleeAttack()
    {
        if (playerController == null) return;

        canMeleeAttack = false;
        StartCoroutine(MeleeCooldownCoroutine());

        // 이펙트 생성 로직
        if (swordEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(swordEffectPrefab, firePoint.position, firePoint.rotation);
            Destroy(effectInstance, 2f);
        }

        Vector3 origin = firePoint.position;

        //Debug.Log("📢 칼 공격 시도! 위치: " + origin + ", 범위: " + meleeRange);

        Collider[] hitColliders = Physics.OverlapSphere(origin, meleeRange);

        //Debug.Log("📢 감지된 콜라이더 수: " + hitColliders.Length);

        foreach (var hitCollider in hitColliders)
        {
            //Debug.Log("    - 감지된 오브젝트: " + hitCollider.name + ", 태그: " + hitCollider.tag);

            if (hitCollider.CompareTag("Enemy"))
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    damageable.TakeDamage(playerController.attackDamage);
                    //Debug.Log("✅ 칼 공격 성공: " + hitCollider.name + "에게 " + playerController.attackDamage + " 피해를 입혔습니다.");
                }
                else
                {
                    //Debug.LogWarning("❌ Enemy 태그는 있지만 IDamageable 스크립트가 없습니다: " + hitCollider.name);
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
            //Debug.Log("총 공격: 투사체에 " + playerController.attackDamage + " 데미지를 설정했습니다.");
        }
    }

    // ===========================================
    // 📢 근접 공격 범위 시각화 (유니티 에디터 전용)
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