using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro; // TMP 사용

public class PlayerController : MonoBehaviour
{
    // --- 이동 관련 변수 ---
    private float speed;
    private float walkSpeed = 5f;
    private float runSpeed = 12f;
    private float stopSpeed = 0f;
    private float jumpPower = 7f;
    private float stopJumpPower = 0f;

    // --- 레벨 및 경험치 변수 ---
    public int currentLevel = 1;
    public int currentEXP = 0;
    private int requiredEXP; // Start에서 계산됨
    private const int BASE_EXP_TO_NEXT_LEVEL = 25;
    private const int EXP_INCREASE_PER_LEVEL = 10;
    // 📢 초기 레벨/경험치 저장용
    private int initialLevel = 1;
    private int initialEXP = 0;


    // --- 능력치 변수 ---
    public int attackDamage = 1;
    public int hpUpgradeLevelCost = 1;
    public int attackUpgradeLevelCost = 1;
    public const int HP_UPGRADE_AMOUNT = 10;
    public const int ATTACK_UPGRADE_AMOUNT = 1;
    // 📢 초기 능력치 저장용
    private int initialAttackDamage = 1;
    private int initialHpUpgradeLevelCost = 1;
    private int initialAttackUpgradeLevelCost = 1;


    // --- 카메라 및 컨트롤러 ---
    public CinemachineSwitcher cinemachineSwitcher;
    public float gravity = -9.81f;
    public CinemachineVirtualCamera virtualCam;
    public float rotationSpeed = 10f;
    private CinemachinePOV pov;
    private CharacterController controller;
    private Vector3 velocity;
    public bool isGrounded;

    // --- HP 관련 변수 ---
    public int maxHP = 100;
    public int currentHP;
    public Slider hpSlider;
    // 📢 초기 HP 저장용
    private int initialMaxHP = 100;

    // --- UI 연결 변수 ---
    [Header("UI")]
    public Slider expSlider;
    public Image expFillImage;
    public GameObject respawnPanel; // 📢 <<< 리스폰 패널 UI 연결

    // --- 시스템 참조 ---
    [Header("System References")]
    public InventoryShopManager inventoryShopManager;

    // --- 기타 변수 ---
    private Coroutine fireDotCoroutine;
    private Vector3 startPosition; // 현재 스폰 위치 (SafeZone 등으로 갱신 가능)
    private Vector3 initialSpawnPosition; // 📢 <<< 게임 시작 시점의 스폰 위치
    private Animator anim; // 애니메이터

    private Renderer playerRenderer;
    private Color originalPlayerColor;
    private Coroutine blinkCoroutine;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        pov = virtualCam.GetCinemachineComponent<CinemachinePOV>();
        anim = GetComponentInChildren<Animator>(); // 자식 포함 애니메이터 찾기

        // 📢 초기 능력치 저장
        initialMaxHP = maxHP;
        initialAttackDamage = attackDamage;
        initialLevel = currentLevel;
        initialEXP = currentEXP;
        initialHpUpgradeLevelCost = hpUpgradeLevelCost;
        initialAttackUpgradeLevelCost = attackUpgradeLevelCost;
        initialSpawnPosition = transform.position; // 게임 시작 위치 저장

        // 초기화
        currentHP = maxHP;
        if (hpSlider != null) // null 체크 추가
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
        startPosition = initialSpawnPosition; // 현재 스폰 위치 초기화
        CalculateRequiredEXP();
        UpdateEXPSlider();

        // 리스폰 패널 비활성화 확인
        if (respawnPanel != null)
        {
            respawnPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("리스폰 패널(Respawn Panel)이 연결되지 않았습니다.", this.gameObject);
        }

        // 초기 커서 상태 설정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerRenderer = GetComponentInChildren<Renderer>(true);
        if (playerRenderer != null)
        {
            originalPlayerColor = playerRenderer.material.color;
        }
        else
        {
            Debug.LogWarning("Player Renderer를 찾을 수 없습니다 (깜빡임 효과용)", this.gameObject);
        }
    }

    void Update()
    {
        // 📢 사망 상태에서는 조작 불가
        if (respawnPanel != null && respawnPanel.activeSelf)
        {
            return;
        }

        // === 인벤토리/상점 로직 ===
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inventoryShopManager != null)
            {
                inventoryShopManager.ToggleInventoryShop(this);
            }
            else { Debug.LogError("InventoryShopManager 참조가 없습니다!"); }
        }
        if (inventoryShopManager != null && inventoryShopManager.IsPanelOpen)
        {
            return; // 인벤토리 열려있으면 아래 로직 실행 안 함
        }

        // === 이동 및 카메라/애니메이션 로직 ===
        HandleMovementInput(); // 이동 관련 로직 함수로 분리 (가독성)
        HandleCameraAndRotation();
        ApplyGravity();
        HandleAnimation();
    }

    // 이동 입력 처리 함수
    void HandleMovementInput()
    {
        if (cinemachineSwitcher.usingFreeLook == true)
        {
            speed = stopSpeed;
            jumpPower = stopJumpPower;
        }
        else
        {
            jumpPower = 7f;
            speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            virtualCam.m_Lens.FieldOfView = Input.GetKey(KeyCode.LeftShift) ? 80f : 60f;
        }

        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 땅에 붙어있도록 살짝 아래로 힘 적용
        }

        // 점프
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = jumpPower;
        }
    }

    // 카메라 방향 기준 이동 및 회전 처리 함수
    void HandleCameraAndRotation()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 camForward = virtualCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();
        Vector3 camRight = virtualCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDirection = (camForward * z + camRight * x).normalized;
        controller.Move(moveDirection * speed * Time.deltaTime);

        // 카메라 방향으로 회전 (FreeLook 아닐 때만)
        if (!cinemachineSwitcher.usingFreeLook)
        {
            float cameraYaw = pov.m_HorizontalAxis.Value;
            Quaternion targetRot = Quaternion.Euler(0f, cameraYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Tab 키 카메라 리셋
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            pov.m_HorizontalAxis.Value = transform.eulerAngles.y;
            pov.m_VerticalAxis.Value = 0f;
        }
    }

    // 중력 적용 함수
    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // 애니메이션 처리 함수
    void HandleAnimation()
    {
        if (anim != null)
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");
            bool isMoving = (x != 0f || z != 0f); // 이동 중인지 확인
            anim.SetInteger("Walk", isMoving ? 1 : 0);
        }
    }


    // === 피해 및 사망 로직 ===
    public void TakeDamage(int damage)
    {
        if (currentHP <= 0 || (respawnPanel != null && respawnPanel.activeSelf)) return;

        // 🔻 3. 피격 시 코루틴 호출 🔻
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;
        if (hpSlider != null) hpSlider.value = currentHP; // null 체크 후 값 설정

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 🔻 4. 깜빡임 코루틴 추가 🔻
    private IEnumerator BlinkEffect()
    {
        if (playerRenderer == null) yield break;

        float blinkDuration = 0.1f;

        playerRenderer.material.color = Color.red;
        yield return new WaitForSeconds(blinkDuration);
        playerRenderer.material.color = originalPlayerColor;

        blinkCoroutine = null;
    }

    void Die()
    {
        //Debug.Log("플레이어가 사망했습니다!");

        // 📢 HP 슬라이더 값을 0으로 설정!
        if (hpSlider != null)
        {
            hpSlider.value = 0;
        }
        // currentHP는 이미 0 이하일 것이므로 따로 설정할 필요는 없습니다.

        // 리스폰 UI 활성화 및 게임 정지
        if (respawnPanel != null)
        {
            respawnPanel.SetActive(true);
            Time.timeScale = 0f; // 게임 시간 정지
            Cursor.lockState = CursorLockMode.None; // 커서 보이기
            Cursor.visible = true;
        }
        else
        {
            Debug.LogError("리스폰 패널(Respawn Panel)이 연결되지 않았습니다!");
        }
    }

    // 📢 리스폰 버튼 클릭 시 호출될 함수
    public void ManualRespawn()
    {
        if (respawnPanel != null)
        {
            respawnPanel.SetActive(false);
        }
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Respawn(); // 실제 리스폰 로직 호출
    }


    // === 리스폰 함수 (능력치 초기화 추가) ===
    void Respawn()
    {
        // DOT 코루틴 중지
        if (fireDotCoroutine != null)
        {
            StopCoroutine(fireDotCoroutine);
            fireDotCoroutine = null;
        }

        // 위치 이동 (startPosition 사용)
        controller.enabled = false;
        transform.position = startPosition; // startPosition은 SafeZone 등으로 갱신될 수 있음
        controller.enabled = true;
        velocity = Vector3.zero;

        // 📢 능력치 초기화!
        maxHP = initialMaxHP;
        currentHP = initialMaxHP;
        attackDamage = initialAttackDamage;
        currentLevel = initialLevel;
        currentEXP = initialEXP;
        hpUpgradeLevelCost = initialHpUpgradeLevelCost;
        attackUpgradeLevelCost = initialAttackUpgradeLevelCost;
        startPosition = initialSpawnPosition; // 스폰 위치도 게임 시작 위치로 초기화

        // UI 업데이트
        if (hpSlider != null) // null 체크
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }
        CalculateRequiredEXP();
        UpdateEXPSlider();

        // 인벤토리 열려있으면 스탯 갱신
        if (inventoryShopManager != null && inventoryShopManager.IsPanelOpen)
        {
            inventoryShopManager.UpdateStats(this);
        }

        Debug.Log("플레이어가 리스폰되었습니다. (능력치 초기화됨)");
    }


    // ===========================================
    // === 나머지 함수들 (SafeZone, 경험치, 업그레이드 등) ===
    // ===========================================
    public void HealToAmount(int targetHP)
    {
        if (currentHP >= targetHP) return;
        currentHP = Mathf.Min(targetHP, maxHP);
        if (hpSlider != null) hpSlider.value = currentHP;
        Debug.Log("체력이 " + currentHP + "까지 회복되었어!");
    }

    public void UpdateSpawnPoint(Vector3 newSpawnPosition)
    {
        startPosition = newSpawnPosition;
        // Debug.Log("새로운 스폰 포인트가 설정되었습니다: " + newSpawnPosition);
    }

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

    private void CheckForLevelUp()
    {
        while (currentEXP >= requiredEXP && requiredEXP > 0) // requiredEXP가 0보다 클 때만 실행
        {
            currentLevel++;
            currentEXP -= requiredEXP;
            CalculateRequiredEXP();
            UpdateEXPSlider(); // 레벨업 후 슬라이더 갱신
        }
    }

    private void CalculateRequiredEXP()
    {
        requiredEXP = BASE_EXP_TO_NEXT_LEVEL + (currentLevel - 1) * EXP_INCREASE_PER_LEVEL;
        if (requiredEXP <= 0) requiredEXP = BASE_EXP_TO_NEXT_LEVEL; // 0 이하 방지
    }

    private void UpdateEXPSlider()
    {
        if (expSlider == null) return;
        // requiredEXP가 0이면 나누기 오류 발생 방지
        expSlider.value = (requiredEXP > 0) ? (float)currentEXP / requiredEXP : 0f;
        if (expFillImage != null)
        {
            expFillImage.enabled = currentEXP > 0;
        }
    }

    public bool TryUpgradeMaxHP()
    {
        if (currentLevel >= hpUpgradeLevelCost)
        {
            currentLevel -= hpUpgradeLevelCost;
            maxHP += HP_UPGRADE_AMOUNT;
            // currentHP = maxHP; // 👈 이 라인을 삭제하거나 주석 처리합니다.
            hpUpgradeLevelCost++;
            // hpSlider.value는 이미 currentHP 값이므로, maxValue만 갱신해주면 됩니다.
            if (hpSlider != null) { hpSlider.maxValue = maxHP; hpSlider.value = currentHP; }
            UpdateEXPSlider(); // 레벨 사용 후 슬라이더 갱신
            if (inventoryShopManager != null) inventoryShopManager.UpdateStats(this);
            return true;
        }
        return false;
    }

    public bool TryUpgradeAttackPower()
    {
        if (currentLevel >= attackUpgradeLevelCost)
        {
            currentLevel -= attackUpgradeLevelCost;
            attackDamage += ATTACK_UPGRADE_AMOUNT;
            attackUpgradeLevelCost++;
            UpdateEXPSlider(); // 레벨 사용 후 슬라이더 갱신
            if (inventoryShopManager != null) inventoryShopManager.UpdateStats(this);
            return true;
        }
        return false;
    }

    // DOT 관련 함수
    public void StartDamageOverTime(int damage, float duration, float interval)
    {
        if (fireDotCoroutine != null) StopCoroutine(fireDotCoroutine);
        fireDotCoroutine = StartCoroutine(DamageOverTimeCoroutine(damage, duration, interval));
    }
    private IEnumerator DamageOverTimeCoroutine(int damage, float duration, float interval)
    {
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            TakeDamage(damage); // TakeDamage 내부에서 currentHP <= 0 체크
            if (currentHP <= 0) yield break; // 죽으면 코루틴 중지
            yield return new WaitForSeconds(interval);
        }
        fireDotCoroutine = null;
    }

    // DeadZone 관련 함수
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Debug.Log("으악!");
            // 📢 DeadZone에서는 즉시 리스폰 및 초기화
            Die();
        }
    }
}