using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class Portal : MonoBehaviour
{
    // 🔻 [삭제] Inspector에서 설정하던 변수 제거
    // public string nextSceneName; 

    // 플레이어가 닿았는지 확인 (Collider 필요, Is Trigger 체크 필수)
    private void OnTriggerEnter(Collider other)
    {
        // 닿은 오브젝트가 "Player" 태그를 가졌는지 확인
        if (other.CompareTag("Player"))
        {
            // GameManager가 존재하는지 확인
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager를 찾을 수 없습니다! 레벨을 진행할 수 없습니다.");
                return;
            }

            // 🔻 [수정] GameManager에서 현재 레벨 가져오기
            int currentLevel = GameManager.Instance.currentLevel;
            int nextLevelNumber = currentLevel + 1;

            // 🔻 [수정] 다음 씬 이름 자동 생성 (예: "Level_2", "Level_3")
            string sceneToLoad = "Level_" + nextLevelNumber;


            //만약 Level 5가 마지막이라면, 다음 씬 대신 엔딩 씬을 로드하거나 게임 클리어 처리
             if (nextLevelNumber > 5) // 예: 총 5 레벨까지 있을 경우
            {
                Debug.Log("게임 클리어!");
                SceneManager.LoadScene("EndingScene"); // 엔딩 씬 로드
                // 또는 다른 게임 클리어 로직 실행
                return; // 아래 LoadNextLevel 실행 안 함
            }

           Debug.Log("포탈 활성화! 다음 레벨: " + sceneToLoad);

            // 🔻 [수정] GameManager를 사용하여 '계산된 이름'의 다음 레벨로 이동
            GameManager.Instance.LoadNextLevel(sceneToLoad);
        }
    }
}