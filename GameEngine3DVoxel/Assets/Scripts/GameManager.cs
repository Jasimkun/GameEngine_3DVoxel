using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Level Settings")]
    public int currentLevel = 1; // 현재 스테이지/난이도 레벨
    public int hpBonusPerLevel = 5;       // 몬스터 레벨당 체력 보너스
    public int damageBonusPerLevel = 1;  // 몬스터 레벨당 공격력 보너스

    [Header("Tile Collapse Settings")]
    public float baseCollapseDelay = 5.0f;
    public float delayReductionPerLevel = 0.5f;
    public float minCollapseDelay = 0.1f;
    public float currentCollapseDelay { get; private set; }

    // 🔻🔻🔻 [추가] 플레이어 능력치 저장 변수 🔻🔻🔻
    [Header("Player Stats")]
    public int playerCurrentHP = 100;
    public int playerMaxHP = 100;
    public int playerCurrentEXP = 0;
    public int playerCurrentLevel = 1;
    public int playerAttackDamage = 1;
    public int playerHpUpgradeCost = 1;
    public int playerAttackUpgradeCost = 1;

    // 🔻 [추가] 초기 능력치 저장용 (리스폰 시 사용)
    private int initialPlayerMaxHP;
    private int initialPlayerAttackDamage;
    private int initialPlayerLevel;
    private int initialPlayerEXP;
    private int initialHpUpgradeCost;
    private int initialAttackUpgradeCost;
    private bool playerStatsInitialized = false; // 딱 한 번만 초기값 저장하기 위한 플래그
    // 🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CalculateCurrentCollapseDelay();

            // 🔻 [추가] 게임 시작 시 딱 한 번만 플레이어 초기 능력치 저장
            if (!playerStatsInitialized)
            {
                InitializePlayerStats();
                playerStatsInitialized = true;
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 🔻 [추가] 플레이어 초기 능력치 저장 함수
    public void InitializePlayerStats()
    {
        // 현재 Inspector에 설정된 값을 초기값으로 저장
        initialPlayerMaxHP = playerMaxHP;
        initialPlayerAttackDamage = playerAttackDamage;
        initialPlayerLevel = playerCurrentLevel;
        initialPlayerEXP = playerCurrentEXP;
        initialHpUpgradeCost = playerHpUpgradeCost;
        initialAttackUpgradeCost = playerAttackUpgradeCost;
        // 시작 시 현재 HP는 최대 HP와 같게
        playerCurrentHP = playerMaxHP;
        Debug.Log("Player initial stats saved.");
    }

    // 🟢 [수정] 레벨 전환 전 준비 함수 호출
    public void LoadNextLevel(string sceneName)
    {
        PrepareForNextLevel(); // ⬅️ 다음 레벨 진입 전 준비 (모든 상태 유지)
        currentLevel++; // 스테이지 레벨 증가
        CalculateCurrentCollapseDelay();
        SceneManager.LoadScene(sceneName);
    }

    // 🟢 [수정] 다음 레벨 진입 시 모든 능력치와 현재 HP를 그대로 유지하는 함수
    public void PrepareForNextLevel()
    {
        // 레벨을 넘어갈 때 업그레이드된 능력치(Max HP, 공격력, 레벨, EXP)와 현재 HP를 모두 그대로 유지합니다.
        // HP 회복 로직은 제거되었습니다. (playerCurrentHP 값 변경 없음)
        Debug.Log("Player successfully prepared for next level. All current stats and HP preserved.");
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.name + " Load Complete. Stage Level: " + currentLevel + ", Collapse Delay: " + currentCollapseDelay + "s");

        LevelDisplay levelDisplay = FindObjectOfType<LevelDisplay>();
        if (levelDisplay != null)
        {
            levelDisplay.ShowLevel(currentLevel);
        }
        else { Debug.LogWarning("LevelDisplay object not found in the loaded scene!"); }
    }

    public void CalculateCurrentCollapseDelay()
    {
        currentCollapseDelay = baseCollapseDelay - (currentLevel - 1) * delayReductionPerLevel;
        if (currentCollapseDelay < minCollapseDelay) currentCollapseDelay = minCollapseDelay;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            RestartCurrentScene();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            // 현재 씬에 있는 PlayerController를 찾습니다.
            PlayerController player = FindObjectOfType<PlayerController>();

            // PlayerController를 찾았으면 AddExperience 함수 호출
            if (player != null)
            {
                int expAmount = 50;
                player.AddExperience(expAmount); // PlayerController의 함수를 호출해야 UI 갱신 및 레벨업 체크가 됩니다.
                Debug.Log($"CHEAT: Added {expAmount} EXP!"); // 콘솔에 치트 사용 로그 출력
            }
            else
            {
                Debug.LogWarning("CHEAT: PlayerController not found in the current scene. Cannot add EXP.");
            }
        }
    }

    void RestartCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 🚨 중요: 씬을 재시작해도 능력치를 유지하려면, 여기서 ResetPlayerStatsToInitial()을 호출하면 안 됩니다.
        // 만약 '1'을 누르는 것이 죽고 나서 리스폰을 의미한다면, 아래 주석을 해제하세요.
        // ResetPlayerStatsToInitial(); 

        Debug.Log("Restarting current scene: '" + currentSceneName + "'");
        SceneManager.LoadScene(currentSceneName);
    }

    // 🔻🔻🔻 [추가] 플레이어 능력치 업데이트 함수들 🔻🔻🔻

    public void UpdatePlayerHP(int newHP)
    {
        playerCurrentHP = Mathf.Clamp(newHP, 0, playerMaxHP); // HP가 0 미만 또는 최대 HP 초과 방지
    }

    public void AddPlayerExperience(int amount)
    {
        playerCurrentEXP += amount;
        // 레벨업 체크 로직은 PlayerController에서 처리 후 GameManager 값 업데이트 요청
    }

    public void UpdatePlayerLevelData(int newLevel, int newEXP, int newRequiredEXP) // 레벨업 시 PlayerController가 호출
    {
        playerCurrentLevel = newLevel;
        playerCurrentEXP = newEXP;
        // requiredEXP는 PlayerController가 계산하므로 GameManager는 저장 안 함
    }

    public void UpgradePlayerHPStat(int newMaxHP, int newCurrentHP, int newCost, int levelUsed)
    {
        playerMaxHP = newMaxHP;
        playerCurrentHP = newCurrentHP; // 업글 시 HP 안 차는 로직 반영
        playerHpUpgradeCost = newCost;
        playerCurrentLevel -= levelUsed; // 사용한 레벨 반영
    }

    public void UpgradePlayerAttackStat(int newAttack, int newCost, int levelUsed)
    {
        playerAttackDamage = newAttack;
        playerAttackUpgradeCost = newCost;
        playerCurrentLevel -= levelUsed; // 사용한 레벨 반영
    }

    // 리스폰 시 호출될 함수 (PlayerController에서 호출) - 이 함수는 게임 오버 또는 완전한 리스폰 시에만 호출되어야 합니다.
    public void ResetPlayerStatsToInitial()
    {
        playerCurrentHP = initialPlayerMaxHP;
        playerMaxHP = initialPlayerMaxHP;
        playerCurrentEXP = initialPlayerEXP;
        playerCurrentLevel = initialPlayerLevel;
        playerAttackDamage = initialPlayerAttackDamage;
        playerHpUpgradeCost = initialHpUpgradeCost;
        playerAttackUpgradeCost = initialAttackUpgradeCost;
        Debug.Log("Player stats have been reset to initial values in GameManager.");
    }
    // 🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺🔺
}
