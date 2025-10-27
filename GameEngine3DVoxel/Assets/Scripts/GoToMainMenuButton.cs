using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

public class GoToMainMenuButton : MonoBehaviour
{
    // 버튼 클릭 시 호출될 함수
    public void LoadMainMenu()
    {
        // "Main" 씬을 로드합니다.
        // GameManager가 있다면 레벨 관련 데이터 초기화 가능
        if (GameManager.Instance != null)
        {
            // 필요하다면 메인 메뉴로 돌아갈 때 레벨 초기화
            GameManager.Instance.currentLevel = 1;
            GameManager.Instance.CalculateCurrentCollapseDelay();
            // GameManager.Instance.InitializePlayerStats(); // 플레이어 스탯 초기화 (선택적)
        }
        SceneManager.LoadScene("Main");
        Debug.Log("Loading Main Menu...");
    }
}