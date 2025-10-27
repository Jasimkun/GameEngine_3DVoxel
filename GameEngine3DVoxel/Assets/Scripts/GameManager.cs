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

    // ��ũ��Ʈ�� Ȱ��ȭ�� �� �̺�Ʈ ����
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ��ũ��Ʈ�� ��Ȱ��ȭ�� �� �̺�Ʈ ���� ���� (�޸� ���� ����)
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ���� �ε�Ǿ��� �� ȣ��� �Լ�
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.name + " �� �ε� �Ϸ�. ���� ����: " + currentLevel);

        // �ε�� ������ LevelDisplay ������Ʈ�� ã���ϴ�.
        LevelDisplay levelDisplay = FindObjectOfType<LevelDisplay>();

        // LevelDisplay�� ã�Ҵٸ� ShowLevel �Լ� ȣ��
        if (levelDisplay != null)
        {
            levelDisplay.ShowLevel(currentLevel);
        }
        else
        {
            // (������) ���� Ư�� ��(��: ���� �޴�)�� LevelDisplay�� ���� ���� �����̶��
            // if (!scene.name.Contains("MainMenu")) // �� �̸����� ����
            // {
            Debug.LogWarning("�ε�� ���� LevelDisplay ������Ʈ�� �����ϴ�!");
            // }
        }
    }
}