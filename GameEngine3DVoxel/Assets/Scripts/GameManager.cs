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

    void Awake()
    {
        // 3. 싱글톤 설정 (씬이 바뀌어도 파괴되지 않음)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 이 오브젝트를 파괴하지 않음
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
        SceneManager.LoadScene(sceneName); // 다음 씬을 로드합니다.
    }
}