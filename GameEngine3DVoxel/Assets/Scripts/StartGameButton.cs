using UnityEngine;
using UnityEngine.SceneManagement; // �� ������ ���� �ʼ�

public class StartGameButton : MonoBehaviour
{
    // ��ư Ŭ�� �� ȣ��� �Լ�
    public void LoadLevel1()
    {
        // "Level_1" ���� �ε��մϴ�.
        // GameManager�� �ִٸ� ������ 1�� �ʱ�ȭ�ϰų� ���� �߰� ����
        if (GameManager.Instance != null)
        {
            // �ʿ��ϴٸ� ���� ���� �� ������ 1�� ���� ����
            GameManager.Instance.currentLevel = 1;
            GameManager.Instance.CalculateCurrentCollapseDelay(); // 1���� �ر� �ӵ� ���
            GameManager.Instance.InitializePlayerStats(); // �÷��̾� ���� �ʱ�ȭ (������)
        }
        SceneManager.LoadScene("Level_1");
        Debug.Log("Loading Level_1...");
    }
}