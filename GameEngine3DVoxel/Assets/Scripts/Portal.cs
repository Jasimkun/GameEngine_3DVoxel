using UnityEngine;
using UnityEngine.SceneManagement; // �� ������ ���� �ʼ�!

public class Portal : MonoBehaviour
{
    // 1. Inspector���� "Level_2" (���� �� �̸�)�� ���� �Է����ݴϴ�.
    public string nextSceneName;

    // 2. �÷��̾ ��Ҵ��� Ȯ�� (Collider �ʿ�)
    private void OnTriggerEnter(Collider other)
    {
        // 3. ���� ������Ʈ�� "Player" �±׸� �������� Ȯ��
        if (other.CompareTag("Player"))
        {
            // 4. �� �̸��� ������� ������ Ȯ��
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("Portal�� 'Next Scene Name'�� �������� �ʾҽ��ϴ�!");
                return;
            }

            // 5. GameManager�� ����Ͽ� ���� ������ �̵� (���� + 1)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadNextLevel(nextSceneName);
            }
            else
            {
                // GameManager�� ���� ��� (�׽�Ʈ��)
                Debug.LogWarning("GameManager�� ã�� �� �����ϴ�! ���� �ε��մϴ�.");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}