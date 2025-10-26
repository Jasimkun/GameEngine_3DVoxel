using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro; // TMP 사용

public class PlayerController : MonoBehaviour
{
    private float speed;
    private float walkSpeed = 5f;
    private float runSpeed = 12f;
    private float stopSpeed = 0f;
    private float jumpPower = 7f;
    private float stopJumpPower = 0f;

    // 🔥 경험치 및 레벨 관련 변수 (public: InventoryShopManager 접근용)
    public int currentLevel = 1;
    public int currentEXP = 0;
    private int requiredEXP = 25;

    // 🔥 레벨업에 필요한 기본 경험치 및 증가량 상수
    private const int BASE_EXP_TO_NEXT_LEVEL = 25;
    private const int EXP_INCREASE_PER_LEVEL = 10;

    // 📢 플레이어가 적에게 주는 데미지/공격력 (public: InventoryShopManager 접근용)
    public int attackDamage = 1; // 변수명을 attackPower에서 attackDamage로 변경하여 역할 명확화

    // 📢 업그레이드 비용: 다음 업그레이드에 필요한 레벨 (public: InventoryShopManager 접근용)
    public int hpUpgradeLevelCost = 1;
    public int attackUpgradeLevelCost = 1;

    // 📢 업그레이드 효과량 상수
    public const int HP_UPGRADE_AMOUNT = 10;
    public const int ATTACK_UPGRADE_AMOUNT = 1;

    public CinemachineSwitcher cinemachineSwitcher;
    public float gravity = -9.81f;
    public CinemachineVirtualCamera virtualCam;
    public float rotationSpeed = 10f;
    private CinemachinePOV pov;
    private CharacterController controller;
    private Vector3 velocity;
    public bool isGrounded;

    // 📢 최대 HP 및 현재 HP (public: InventoryShopManager 접근용)
    public int maxHP = 100;
    public int currentHP;
    public Slider hpSlider;

    // 📢 UI 연결 변수
    [Header("UI")]
    public Slider expSlider;
    public Image expFillImage;

    // 📢 시스템 참조: 인벤토리/상점 관리자
    [Header("System References")]
    public InventoryShopManager inventoryShopManager;

    // === DOT (Damage Over Time) 설정 변수 ===
    private Coroutine fireDotCoroutine;

    // === 리스폰 설정 변수 ===
    [Header("Respawn Settings")]
    private Vector3 startPosition;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        pov = virtualCam.GetCinemachineComponent<CinemachinePOV>();

        // 📢 시작 시 현재 HP를 최대 HP로 설정 (요청하신 내용, 이미 구현되어 있었습니다!)
        currentHP = maxHP;
        hpSlider.maxValue = maxHP;
        hpSlider.value = currentHP; // hpSlider.value = 1f; 대신 currentHP를 사용하면 명확합니다.

        startPosition = transform.position;

        CalculateRequiredEXP();
        UpdateEXPSlider();
    }

    void Update()
    {
        // === Inventory/Shop Toggle 로직 ===
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inventoryShopManager != null)
            {
                inventoryShopManager.ToggleInventoryShop(this);
            }
            else
            {
                Debug.LogError("InventoryShopManager reference is missing on PlayerController!");
            }
        }

        if (inventoryShopManager != null && inventoryShopManager.IsPanelOpen)
        {
            return;
        }

        // === 입력 및 속도 제어 ===
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            pov.m_HorizontalAxis.Value = transform.eulerAngles.y;
            pov.m_VerticalAxis.Value = 0f;
        }

        if (cinemachineSwitcher.usingFreeLook == true)
        {
            speed = stopSpeed;
            jumpPower = stopJumpPower;
        }
        else
        {
            jumpPower = 7f;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed = runSpeed;
                virtualCam.m_Lens.FieldOfView = 80f;
            }
            else
            {
                speed = walkSpeed;
                virtualCam.m_Lens.FieldOfView = 60f;
            }
        }

        // === 땅 확인 및 점프 로직 ===
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = jumpPower;
            }
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // === 이동 및 회전 로직 ===
        Vector3 camForward = virtualCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = virtualCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 move = (camForward * z + camRight * x).normalized;
        controller.Move(move * speed * Time.deltaTime);

        float cameraYaw = pov.m_HorizontalAxis.Value;
        Quaternion targetRot = Quaternion.Euler(0f, cameraYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);


        // === 중력 적용 ===
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Debug.Log("DeadZone에 진입! 즉시 리스폰합니다.");
            Respawn();
        }
    }


    // === 리스폰 함수 ===
    void Respawn()
    {
        if (fireDotCoroutine != null)
        {
            StopCoroutine(fireDotCoroutine);
            fireDotCoroutine = null;
        }

        controller.enabled = false;
        transform.position = startPosition;
        controller.enabled = true;

        velocity = Vector3.zero;

        currentHP = maxHP;
        hpSlider.value = currentHP; // maxHP 대신 currentHP를 사용하면 명확합니다.
    }

    // === 피해 및 사망 로직 (📢 연쇄 오류의 원인이었던 함수 복구) ===
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
        Respawn();
    }

    // === DOT 로직 (📢 연쇄 오류의 원인이었던 함수 복구) ===
    public void StartDamageOverTime(int damage, float duration, float interval)
    {
        if (fireDotCoroutine != null)
        {
            StopCoroutine(fireDotCoroutine);
        }
        fireDotCoroutine = StartCoroutine(DamageOverTimeCoroutine(damage, duration, interval));
    }

    private IEnumerator DamageOverTimeCoroutine(int damage, float duration, float interval)
    {
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            TakeDamage(damage);
            yield return new WaitForSeconds(interval);
        }
        fireDotCoroutine = null;
    }

    // 🔥 경험치 획득 메서드
    public void AddExperience(int amount)
    {
        currentEXP += amount;

        UpdateEXPSlider();
        CheckForLevelUp();

        if (inventoryShopManager != null && inventoryShopManager.IsPanelOpen)
        {
            inventoryShopManager.UpdateStats(this);
        }
    }

    // 🔥 레벨업 확인 및 처리
    private void CheckForLevelUp()
    {
        while (currentEXP >= requiredEXP)
        {
            currentLevel++;
            currentEXP -= requiredEXP;
            CalculateRequiredEXP();

            UpdateEXPSlider();
        }
    }

    // 🔥 다음 레벨업에 필요한 경험치를 계산하는 메서드
    private void CalculateRequiredEXP()
    {
        requiredEXP = BASE_EXP_TO_NEXT_LEVEL + (currentLevel - 1) * EXP_INCREASE_PER_LEVEL;
    }

    // 📢 경험치 슬라이더 업데이트
    private void UpdateEXPSlider()
    {
        if (expSlider == null) return;

        float expPercentage = (float)currentEXP / requiredEXP;
        expSlider.value = expPercentage;

        if (expFillImage != null)
        {
            expFillImage.enabled = currentEXP > 0;
        }
    }

    // ===========================================
    // 📢 상점 업그레이드 메서드 (Level Cost 증가 로직 적용)
    // ===========================================

    // 📢 최대 체력 업그레이드
    public bool TryUpgradeMaxHP()
    {
        if (currentLevel >= hpUpgradeLevelCost)
        {
            currentLevel -= hpUpgradeLevelCost;

            maxHP += HP_UPGRADE_AMOUNT;
            currentHP = maxHP; // 체력 업그레이드 시 현재 체력도 최대치로 회복

            hpUpgradeLevelCost++; // 📢 다음 업그레이드 비용 1 증가

            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;

            UpdateEXPSlider();
            inventoryShopManager.UpdateStats(this);

            return true;
        }
        return false;
    }

    // 📢 공격력 업그레이드
    public bool TryUpgradeAttackPower()
    {
        if (currentLevel >= attackUpgradeLevelCost)
        {
            currentLevel -= attackUpgradeLevelCost;

            // 플레이어가 주는 데미지/공격력 변수 증가
            attackDamage += ATTACK_UPGRADE_AMOUNT;

            attackUpgradeLevelCost++; // 📢 다음 업그레이드 비용 1 증가

            UpdateEXPSlider();
            inventoryShopManager.UpdateStats(this);

            return true;
        }
        return false;
    }
}