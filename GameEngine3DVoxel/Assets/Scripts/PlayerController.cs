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

    // --- 레벨 및 경험치 변수 (GameManager에서 동기화됨) ---
    public int currentLevel = 1; // 씬 로드 시 GameManager 값으로 덮어쓰기
    public int currentEXP = 0; // 씬 로드 시 GameManager 값으로 덮어쓰기
    private int requiredEXP; // Start에서 계산됨
    private const int BASE_EXP_TO_NEXT_LEVEL = 25;
    private const int EXP_INCREASE_PER_LEVEL = 10;


    // --- 능력치 변수 (GameManager에서 동기화됨) ---
    public int attackDamage = 1; // 씬 로드 시 GameManager 값으로 덮어쓰기
    public int hpUpgradeLevelCost = 1; // 씬 로드 시 GameManager 값으로 덮어쓰기
    public int attackUpgradeLevelCost = 1; // 씬 로드 시 GameManager 값으로 덮어쓰기
    public const int HP_UPGRADE_AMOUNT = 10;
    public const int ATTACK_UPGRADE_AMOUNT = 1;


    // --- 카메라 및 컨트롤러 ---
    public CinemachineSwitcher cinemachineSwitcher;
    public float gravity = -9.81f;
    public CinemachineVirtualCamera virtualCam;
    public float rotationSpeed = 10f;
    private CinemachinePOV pov;
    private CharacterController controller;
    private Vector3 velocity;
    public bool isGrounded;

    // --- HP 관련 변수 (GameManager에서 동기화됨) ---
    public int maxHP = 100; // 씬 로드 시 GameManager 값으로 덮어쓰기
    public int currentHP; // 씬 로드 시 GameManager 값으로 덮어쓰기
    public Slider hpSlider;

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
    // private Vector3 initialSpawnPosition; // 📢 <<< GameManager에서 초기 위치를 저장하지 않으므로, 이 변수는 로컬로 유지하거나 제거 가능. Start()에서 현재 위치를 사용합니다.
    private Animator anim; // 애니메이터

    // 🔻 1. [수정] 변수를 단수(Renderer)에서 '배열(Renderer[])'로 변경 🔻
    private Renderer[] playerRenderers;
    private Color[] originalPlayerColors;
    private Coroutine blinkCoroutine;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        pov = virtualCam.GetCinemachineComponent<CinemachinePOV>();
        anim = GetComponentInChildren<Animator>(); // 자식 포함 애니메이터 찾기

        // 🟢 1. [핵심 수정] GameManager로부터 최신 능력치 로드 및 동기화
        SyncStatsFromGameManager(); // 씬 로드 시 능력치가 유지되도록 함

        // 초기화 (이젠 GameManager에서 로드된 값들을 기반으로 UI 초기화)
        // currentHP는 이미 SyncStatsFromGameManager에서 로드되었으므로 별도로 설정 불필요

        if (hpSlider != null) // null 체크 추가
        {
            hpSlider.maxValue = maxHP;
            hpSlider.value = currentHP;
        }

        // startPosition은 현재 씬의 스폰 위치로 설정됩니다.
        startPosition = transform.position;

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

        // 🔻 2. [수정] GetComponents(복수)로 모든 Renderer를 찾고, 반복문으로 색상 저장 🔻
        playerRenderers = GetComponentsInChildren<Renderer>(true);

        if (playerRenderers != null && playerRenderers.Length > 0)
        {
            // 색상 배열을 랜더러 개수만큼 초기화
            originalPlayerColors = new Color[playerRenderers.Length];

            // 반복문으로 각 파츠의 원래 색상을 저장
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null) // 혹시 모를 null 체크
                {
                    // 재질의 색상을 저장합니다. (SharedMaterial 대신 material 사용)
                    originalPlayerColors[i] = playerRenderers[i].material.color;
                }
            }
        }
        else
        {
            Debug.LogWarning("Player Renderer(s)를 찾을 수 없습니다 (깜빡임 효과 실패)", this.gameObject);
        }
    }

    // 🟢 [추가] GameManager로부터 최신 능력치를 가져와 로컬 변수에 동기화하는 함수
    void SyncStatsFromGameManager()
    {
        if (GameManager.Instance != null)
        {
            // GameManager에서 최신 능력치를 가져와 PlayerController의 로컬 변수에 덮어씁니다.
            maxHP = GameManager.Instance.playerMaxHP;
            currentHP = GameManager.Instance.playerCurrentHP; // 현재 HP 유지
            currentLevel = GameManager.Instance.playerCurrentLevel;
            currentEXP = GameManager.Instance.playerCurrentEXP;
            attackDamage = GameManager.Instance.playerAttackDamage;
            hpUpgradeLevelCost = GameManager.Instance.playerHpUpgradeCost;
            attackUpgradeLevelCost = GameManager.Instance.playerAttackUpgradeCost;

            Debug.Log($"PlayerController stats synced from GameManager. HP: {currentHP}/{maxHP}, Level: {currentLevel}, EXP: {currentEXP}");
        }
        else
        {
            Debug.LogError("GameManager.Instance is null! Cannot sync player stats. Ensure GameManager object exists and has the script.");
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

        // 🔻 3. [수정] 피격 시 코루틴 호출 🔻
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkEffect());

        currentHP -= damage;
        // 🟢 GameManager에도 변경된 HP 값 업데이트
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerHP(currentHP);
        }

        if (hpSlider != null) hpSlider.value = currentHP; // null 체크 후 값 설정

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // 🔻 4. [수정] 모든 파츠가 깜빡이도록 반복문(loop) 사용 🔻
    private IEnumerator BlinkEffect()
    {
        // 배열이 비어있는지 확인
        if (playerRenderers == null || playerRenderers.Length == 0) yield break;

        float blinkDuration = 0.1f;

        // 1. 모든 파츠를 빨간색으로 변경
        foreach (Renderer rend in playerRenderers)
        {
            if (rend != null) // null 체크
            {
                rend.material.color = Color.red;
            }
        }

        // 0.1초 대기
        yield return new WaitForSeconds(blinkDuration);

        // 2. 모든 파츠를 원래 색상으로 복구 (저장해둔 색상 배열 사용)
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] != null) // null 체크
            {
                playerRenderers[i].material.color = originalPlayerColors[i];
            }
        }

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

        // 🔻 5. [수정] 리스폰 시 모든 파츠의 색상을 되돌림 🔻
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // 반복문으로 모든 파츠 색상 강제 복구
        if (playerRenderers != null && originalPlayerColors != null && playerRenderers.Length == originalPlayerColors.Length)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                {
                    playerRenderers[i].material.color = originalPlayerColors[i];
                }
            }
        }

        // 🟢 GameManager의 능력치를 초기값으로 리셋
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetPlayerStatsToInitial();
        }

        // 🟢 GameManager의 리셋된 값들을 PlayerController에 동기화
        SyncStatsFromGameManager();


        // 위치 이동 (startPosition 사용)
        controller.enabled = false;
        // startPosition은 씬 로드 시 현재 위치로 설정되지만, SafeZone 등으로 갱신될 수 있습니다.
        // 리스폰 시 능력치는 초기화되지만, 스폰 위치는 현재 씬의 스폰 위치(startPosition)를 유지하는 것이 일반적입니다.
        // 만약 게임 시작 위치로 돌아가야 한다면, GameManager에서 initialSpawnPosition을 관리해야 합니다.
        // 현재는 startPosition (가장 최근의 SafeZone)을 사용합니다.
        transform.position = startPosition;
        controller.enabled = true;
        velocity = Vector3.zero;

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
        // 🟢 GameManager에도 변경된 HP 값 업데이트
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerHP(currentHP);
        }
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
        // 🟢 GameManager에 변경된 EXP 값 업데이트
        if (GameManager.Instance != null)
        {
            // GameManager는 EXP를 누적하고, 레벨업 체크는 PlayerController가 담당합니다.
            // 여기서는 EXP 증가만 알립니다.
            GameManager.Instance.AddPlayerExperience(amount);
        }

        UpdateEXPSlider();
        CheckForLevelUp();
        if (inventoryShopManager != null && inventoryShopManager.IsPanelOpen)
        {
            inventoryShopManager.UpdateStats(this);
        }
    }

    private void CheckForLevelUp()
    {
        bool leveledUp = false;
        while (currentEXP >= requiredEXP && requiredEXP > 0) // requiredEXP가 0보다 클 때만 실행
        {
            currentLevel++;
            currentEXP -= requiredEXP;
            CalculateRequiredEXP();
            leveledUp = true;
        }

        // 🟢 레벨업이 발생했으면 GameManager에 최종 값 업데이트
        if (leveledUp && GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerLevelData(currentLevel, currentEXP, requiredEXP);
        }

        if (leveledUp)
        {
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
            int levelUsed = hpUpgradeLevelCost;
            currentLevel -= levelUsed;
            maxHP += HP_UPGRADE_AMOUNT;
            hpUpgradeLevelCost++;

            // 🟢 GameManager에 업그레이드 결과 업데이트
            if (GameManager.Instance != null)
            {
                // newCurrentHP에 현재 HP를 그대로 전달하여 HP가 차지 않도록 함
                GameManager.Instance.UpgradePlayerHPStat(maxHP, currentHP, hpUpgradeLevelCost, levelUsed);
            }

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
            int levelUsed = attackUpgradeLevelCost;
            currentLevel -= levelUsed;
            attackDamage += ATTACK_UPGRADE_AMOUNT;
            attackUpgradeLevelCost++;

            // 🟢 GameManager에 업그레이드 결과 업데이트
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpgradePlayerAttackStat(attackDamage, attackUpgradeLevelCost, levelUsed);
            }

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
