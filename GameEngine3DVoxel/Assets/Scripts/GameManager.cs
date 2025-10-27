using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Level Settings")]
    public int currentLevel = 1; // 현재 스테이지/난이도 레벨
    public int hpBonusPerLevel = 5;      // 몬스터 레벨당 체력 보너스
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
    void InitializePlayerStats()
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


    public void LoadNextLevel(string sceneName)
    {
        currentLevel++; // 스테이지 레벨 증가
        CalculateCurrentCollapseDelay();
        SceneManager.LoadScene(sceneName);
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

    void CalculateCurrentCollapseDelay()
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
    }

    void RestartCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
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

    // 리스폰 시 호출될 함수 (PlayerController에서 호출)
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