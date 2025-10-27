using UnityEngine;
using System.Collections;

public class VoxelCollapse : MonoBehaviour
{
    // [삭제] public float baseCollapseDelay = 5.0f; // GameManager로 이동됨
    public float fadeOutDuration = 1.0f;
    [Header("Fall Settings")]
    public float fallDistance = 1.0f;
    public float fallDuration = 0.5f;
    // [삭제] private float calculatedCollapseDelay; // 사용 안 함
    // [삭제] public float minCollapseDelay = 0.1f; // GameManager로 이동됨

    private Coroutine collapseCoroutine;
    public bool IsCollapseStarted => collapseCoroutine != null;
    private Renderer tileRenderer;
    // --- (Rigidbody 관련 주석은 그대로 둠) ---

    void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer == null || tileRenderer.material == null)
        {
            Debug.LogError($"Awake: Renderer 또는 Material을 찾을 수 없습니다! 오브젝트: {gameObject.name}", this.gameObject);
        }
        // 🔻 [삭제] Awake에서 붕괴 시간 계산 로직 제거 🔻
    }

    // 플레이어가 밟았을 때 호출 (기본 딜레이 사용)
    public void StartDelayedCollapse()
    {
        if (collapseCoroutine == null)
        {
            if (tileRenderer == null || tileRenderer.material == null)
            {
                Debug.LogError("StartDelayedCollapse: Renderer 또는 Material이 null입니다!", this.gameObject);
                return;
            }
            // 🔻 [수정] 코루틴 호출 시 매개변수 없이 호출 (GameManager 값 사용)
            collapseCoroutine = StartCoroutine(StartCollapseWithDelay());
        }
    }

    // 붕괴 취소
    public void CancelCollapse()
    {
        if (collapseCoroutine != null)
        {
            StopCoroutine(collapseCoroutine);
            collapseCoroutine = null;
        }
    }

    // Q 스킬 등 외부에서 딜레이 강제 설정 및 즉시 시작
    public void SetTemporaryDelay(float newDelay)
    {
        // 1. 이미 붕괴 중이면 취소
        CancelCollapse();

        // 2. 시각적 피드백 (선택적)
        // if(tileRenderer != null) tileRenderer.material.color = Color.blue;

        // 3. 🔻 [수정] 새로운 딜레이 값으로 코루틴 즉시 시작 🔻
        if (tileRenderer != null && tileRenderer.material != null) // 시작 전 확인
        {
            collapseCoroutine = StartCoroutine(StartCollapseWithDelay(newDelay)); // newDelay 값을 매개변수로 전달
        }
        else
        {
            Debug.LogError("SetTemporaryDelay: Renderer 또는 Material이 null이라 붕괴 시작 불가!", this.gameObject);
        }
    }

    // 🔻 [수정] 코루틴이 float 매개변수를 받도록 변경 (기본값 -1) 🔻
    IEnumerator StartCollapseWithDelay(float delayToUse = -1f)
    {
        float waitTime;

        // 매개변수로 유효한 값(0 이상)이 들어왔는지 확인
        if (delayToUse >= 0)
        {
            // SetTemporaryDelay에서 전달한 값 사용
            waitTime = delayToUse;
            // Debug.Log($"Using temporary delay: {waitTime}s for {gameObject.name}");
        }
        else // 매개변수가 없거나 음수면 GameManager 값 사용
        {
            if (GameManager.Instance != null)
            {
                waitTime = GameManager.Instance.currentCollapseDelay;
            }
            else
            {
                // GameManager 없을 때 사용할 기본값 (GameManager의 base 값과 맞추는 것이 좋음)
                waitTime = 5.0f;
                Debug.LogWarning($"VoxelCollapse ({gameObject.name}): GameManager not found, using default delay {waitTime}s.");
            }
            // Debug.Log($"Using GameManager delay: {waitTime}s for {gameObject.name}");
        }

        yield return new WaitForSeconds(waitTime); // 계산된/설정된 시간만큼 기다림

        // --- (이하 코루틴 내용은 동일) ---
        if (tileRenderer != null && tileRenderer.material != null)
        {
            StartCoroutine(ControlledFallAndFade());
        }
        else
        {
            Debug.LogError("StartCollapseWithDelay: ControlledFallAndFade 시작 불가 - Renderer 또는 Material이 null!", this.gameObject);
        }
        collapseCoroutine = null; // 코루틴 완료
    }

    // --- (ControlledFallAndFade 코루틴은 동일) ---
    IEnumerator ControlledFallAndFade()
    {
        if (tileRenderer == null || tileRenderer.material == null)
        {
            Debug.LogError($"ControlledFallAndFade: Renderer or Material null on {gameObject.name}. Destroying.", this.gameObject);
            Destroy(gameObject.transform.parent != null ? gameObject.transform.parent.gameObject : gameObject);
            yield break;
        }

        Transform rootTransform = gameObject.transform.parent ?? gameObject.transform;
        Vector3 startPos = rootTransform.position;
        Vector3 endPos = startPos + Vector3.down * fallDistance;
        Color startColor = Color.white;
        string colorPropertyName = GetColorPropertyName();

        if (colorPropertyName != null)
        {
            startColor = tileRenderer.material.GetColor(colorPropertyName);
        }

        float timer = 0f;
        while (timer < fallDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fallDuration);
            rootTransform.position = Vector3.Lerp(startPos, endPos, t);

            if (colorPropertyName != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, t);
                Color newColor = startColor;
                newColor.a = alpha;
                tileRenderer.material.SetColor(colorPropertyName, newColor);
            }
            yield return null;
        }
        Destroy(rootTransform.gameObject);
    }

    // Material의 색상 속성 이름 찾는 Helper 함수
    string GetColorPropertyName()
    {
        if (tileRenderer == null || tileRenderer.material == null) return null;

        if (tileRenderer.material.HasProperty("_BaseColor")) return "_BaseColor";
        if (tileRenderer.material.HasProperty("_Color")) return "_Color";

        Debug.LogError("Material에 '_BaseColor' 또는 '_Color' 속성이 없습니다! Fade 효과 실패.", gameObject);
        return null;
    }
}