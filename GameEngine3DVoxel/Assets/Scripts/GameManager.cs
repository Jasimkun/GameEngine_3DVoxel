using UnityEngine;
using UnityEngine.SceneManagement; // �� ������ ���� �ʿ�

public class GameManager : MonoBehaviour
{
    // 1. �̱��� �ν��Ͻ�
    public static GameManager Instance;

    // 2. ���� ���� �� ���� �� ���ʽ� ����
    public int currentLevel = 1;
    public int hpBonusPerLevel = 5;
    public int damageBonusPerLevel = 1;

    void Awake()
    {
        // 3. �̱��� ���� (���� �ٲ� �ı����� ����)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // �� ������Ʈ�� �ı����� ����
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // �̹� �ν��Ͻ��� ������ �ڽ��� �ı�
        }
    }

    // 4. ���� ����(��)�� �̵��� �� ȣ���� �Լ� (����)
    // (��: "Level_2" ������ �̵�)
    public void LoadNextLevel(string sceneName)
    {
        currentLevel++; // ������ 1 �ø��ϴ�.
        SceneManager.LoadScene(sceneName); // ���� ���� �ε��մϴ�.
    }
}