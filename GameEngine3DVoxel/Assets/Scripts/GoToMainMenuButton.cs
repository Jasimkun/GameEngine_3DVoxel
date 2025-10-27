using UnityEngine;
using UnityEngine.SceneManagement; // �� ������ ���� �ʼ�

public class GoToMainMenuButton : MonoBehaviour
{
    // ��ư Ŭ�� �� ȣ��� �Լ�
    public void LoadMainMenu()
    {
        // "Main" ���� �ε��մϴ�.
        // GameManager�� �ִٸ� ���� ���� ������ �ʱ�ȭ ����
        if (GameManager.Instance != null)
        {
            // �ʿ��ϴٸ� ���� �޴��� ���ư� �� ���� �ʱ�ȭ
            GameManager.Instance.currentLevel = 1;
            GameManager.Instance.CalculateCurrentCollapseDelay();
            // GameManager.Instance.InitializePlayerStats(); // �÷��̾� ���� �ʱ�ȭ (������)
        }
        SceneManager.LoadScene("Main");
        Debug.Log("Loading Main Menu...");
    }
}