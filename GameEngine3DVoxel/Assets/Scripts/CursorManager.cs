using UnityEngine;

public class CursorManager : MonoBehaviour
{
    // 1. 싱글톤 인스턴스
    public static CursorManager Instance;

    private bool isCursorVisible = false; // 현재 커서 상태 추적

    void Awake()
    {
        // 2. 싱글톤 설정 (씬이 바뀌어도 파괴되지 않음)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 게임 시작 시 커서 숨기기 및 잠금
            SetCursorState(false);
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // 이미 인스턴스가 있으면 자신을 파괴
        }
    }

    void Update()
    {
        // 3. Escape 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 현재 커서 상태를 반전시킴
            isCursorVisible = !isCursorVisible;
            SetCursorState(isCursorVisible);
        }
    }

    // 4. 커서 상태 설정 함수 (외부에서도 호출 가능하도록 public으로)
    public void SetCursorState(bool isVisible)
    {
        Cursor.visible = isVisible;
        // 커서가 보일 때는 잠금 해제, 숨길 때는 잠금
        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
        isCursorVisible = isVisible; // 현재 상태 업데이트
        Debug.Log("Cursor Visible: " + isVisible + ", LockState: " + Cursor.lockState); // 상태 로그 (확인용)
    }
}