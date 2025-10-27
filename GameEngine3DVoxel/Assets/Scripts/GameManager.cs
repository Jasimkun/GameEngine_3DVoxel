using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요

public class GameManager : MonoBehaviour
{
    // 1. 싱글톤 인스턴스
    public static GameManager Instance;

    // 2. 현재 레벨 및 레벨 당 보너스 설정
    public int currentLevel = 1;
    public int hpBonusPerLevel = 5;
    public int damageBonusPerLevel = 1;

    [Header("Tile Collapse Settings")]
    public float baseCollapseDelay = 5.0f;     // 기본 붕괴 지연 시간 (VoxelCollapse의 기본값과 일치시키세요)
    public float delayReductionPerLevel = 0.5f; // 레벨당 감소할 시간
    public float minCollapseDelay = 0.1f;      // 최소 붕괴 지연 시간
    public float currentCollapseDelay { get; private set; } // 현재 레벨의 계산된 붕괴 지연 시간 (읽기 전용)

    void Awake()
    {
        // 3. 싱글톤 설정 (씬이 바뀌어도 파괴되지 않음)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 이 오브젝트를 파괴하지 않음
            CalculateCurrentCollapseDelay();
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // 이미 인스턴스가 있으면 자신을 파괴
        }
    }

    // 4. 다음 레벨(씬)로 이동할 때 호출할 함수 (예시)
    // (예: "Level_2" 씬으로 이동)
    public void LoadNextLevel(string sceneName)
    {
        currentLevel++; // 레벨을 1 올립니다.
        CalculateCurrentCollapseDelay();
        SceneManager.LoadScene(sceneName); // 다음 씬을 로드합니다.
    }

    // 스크립트가 활성화될 때 이벤트 구독
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 스크립트가 비활성화될 때 이벤트 구독 해제 (메모리 누수 방지)
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 로드되었을 때 호출될 함수
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.name + " 씬 로드 완료. 현재 레벨: " + currentLevel);

        // 로드된 씬에서 LevelDisplay 컴포넌트를 찾습니다.
        LevelDisplay levelDisplay = FindObjectOfType<LevelDisplay>();

        // LevelDisplay를 찾았다면 ShowLevel 함수 호출
        if (levelDisplay != null)
        {
            levelDisplay.ShowLevel(currentLevel);
        }
        else
        {
            // (선택적) 만약 특정 씬(예: 메인 메뉴)에 LevelDisplay가 없는 것이 정상이라면
            // if (!scene.name.Contains("MainMenu")) // 씬 이름으로 구분
            // {
            Debug.LogWarning("로드된 씬에 LevelDisplay 오브젝트가 없습니다!");
            // }
        }
    }
    void CalculateCurrentCollapseDelay()
    {
        currentCollapseDelay = baseCollapseDelay - (currentLevel - 1) * delayReductionPerLevel;
        if (currentCollapseDelay < minCollapseDelay)
        {
            currentCollapseDelay = minCollapseDelay;
        }
    }
}