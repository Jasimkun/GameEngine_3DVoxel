using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수!

public class Portal : MonoBehaviour
{
    // 1. Inspector에서 "Level_2" (다음 씬 이름)를 직접 입력해줍니다.
    public string nextSceneName;

    // 2. 플레이어가 닿았는지 확인 (Collider 필요)
    private void OnTriggerEnter(Collider other)
    {
        // 3. 닿은 오브젝트가 "Player" 태그를 가졌는지 확인
        if (other.CompareTag("Player"))
        {
            // 4. 씬 이름이 비어있지 않은지 확인
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("Portal에 'Next Scene Name'이 설정되지 않았습니다!");
                return;
            }

            // 5. GameManager를 사용하여 다음 레벨로 이동 (레벨 + 1)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadNextLevel(nextSceneName);
            }
            else
            {
                // GameManager가 없을 경우 (테스트용)
                Debug.LogWarning("GameManager를 찾을 수 없습니다! 씬만 로드합니다.");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}