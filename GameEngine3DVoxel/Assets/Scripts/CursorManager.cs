using UnityEngine;

public class CursorManager : MonoBehaviour
{
    // 1. �̱��� �ν��Ͻ�
    public static CursorManager Instance;

    private bool isCursorVisible = false; // ���� Ŀ�� ���� ����

    void Awake()
    {
        // 2. �̱��� ���� (���� �ٲ� �ı����� ����)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // ���� ���� �� Ŀ�� ����� �� ���
            SetCursorState(false);
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // �̹� �ν��Ͻ��� ������ �ڽ��� �ı�
        }
    }

    void Update()
    {
        // 3. Escape Ű �Է� ����
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ���� Ŀ�� ���¸� ������Ŵ
            isCursorVisible = !isCursorVisible;
            SetCursorState(isCursorVisible);
        }
    }

    // 4. Ŀ�� ���� ���� �Լ� (�ܺο����� ȣ�� �����ϵ��� public����)
    public void SetCursorState(bool isVisible)
    {
        Cursor.visible = isVisible;
        // Ŀ���� ���� ���� ��� ����, ���� ���� ���
        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
        isCursorVisible = isVisible; // ���� ���� ������Ʈ
        Debug.Log("Cursor Visible: " + isVisible + ", LockState: " + Cursor.lockState); // ���� �α� (Ȯ�ο�)
    }
}