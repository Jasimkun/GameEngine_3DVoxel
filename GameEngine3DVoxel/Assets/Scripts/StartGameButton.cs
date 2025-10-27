using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수

public class StartGameButton : MonoBehaviour
{
    // 버튼 클릭 시 호출될 함수
    public void LoadLevel1()
    {
        // "Level_1" 씬을 로드합니다.
        // GameManager가 있다면 레벨을 1로 초기화하거나 로직 추가 가능
        if (GameManager.Instance != null)
        {
            // 필요하다면 게임 시작 시 레벨을 1로 강제 설정
            GameManager.Instance.currentLevel = 1;
            GameManager.Instance.CalculateCurrentCollapseDelay(); // 1레벨 붕괴 속도 계산
            GameManager.Instance.InitializePlayerStats(); // 플레이어 스탯 초기화 (선택적)
        }
        SceneManager.LoadScene("Level_1");
        Debug.Log("Loading Level_1...");
    }
}