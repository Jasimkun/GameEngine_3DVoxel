using UnityEngine;
using System.Collections;

public class VoxelCollapse : MonoBehaviour
{
    // 🔻 [수정] 기본 붕괴 지연 시간 (Inspector에서 설정)
    public float baseCollapseDelay = 5.0f;
    public float fadeOutDuration = 1.0f; // 페이드 아웃 시간 (현재 로직에선 fallDuration과 같음)

    [Header("Fall Settings")]
    public float fallDistance = 1.0f;
    public float fallDuration = 0.5f;

    // 🔻 [추가] 레벨에 따라 계산된 최종 붕괴 지연 시간
    private float calculatedCollapseDelay;
    // 🔻 [추가] 최소 붕괴 지연 시간 (음수 방지)
    public float minCollapseDelay = 0.1f;

    // === 상태 추적 변수 ===
    private Coroutine collapseCoroutine;
    public bool IsCollapseStarted => collapseCoroutine != null;

    // === 내부 컴포넌트 ===
    private Renderer tileRenderer;
    // Rigidbody 관련 코드는 주석 처리 유지 (현재 사용 안 함)
    // private Rigidbody tileRigidbody;

    void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer == null || tileRenderer.material == null)
        {
            Debug.LogError($"Awake: Renderer 또는 Material을 찾을 수 없습니다! 오브젝트: {gameObject.name}", this.gameObject);
        }

        // 🔻 [수정] GameManager에서 현재 레벨을 가져와 붕괴 지연 시간 계산
        int level = 1; // 기본 레벨
        float delayReductionPerLevel = 0.5f; // 레벨당 감소할 시간

        if (GameManager.Instance != null)
        {
            level = GameManager.Instance.currentLevel;
            // 레벨에 맞춰 붕괴 지연 시간 계산 (레벨당 0.5초 감소)
            calculatedCollapseDelay = baseCollapseDelay - (level - 1) * delayReductionPerLevel;

            // 최소 지연 시간보다 작아지지 않도록 제한
            if (calculatedCollapseDelay < minCollapseDelay)
            {
                calculatedCollapseDelay = minCollapseDelay;
            }
            // Debug.Log($"VoxelCollapse ({gameObject.name}): Level {level}, Calculated Delay: {calculatedCollapseDelay}");
        }
        else
        {
            // GameManager가 없을 경우 기본 지연 시간 사용
            calculatedCollapseDelay = baseCollapseDelay;
            Debug.LogWarning($"VoxelCollapse ({gameObject.name}): GameManager Instance not found. Using base collapse delay.");
        }
    }

    public void StartDelayedCollapse()
    {
        if (collapseCoroutine == null)
        {
            // 코루틴 시작 전 Renderer/Material 최종 확인
            if (tileRenderer == null || tileRenderer.material == null)
            {
                Debug.LogError("StartDelayedCollapse: Renderer 또는 Material이 null입니다!", this.gameObject);
                return;
            }
            collapseCoroutine = StartCoroutine(StartCollapseWithDelay());
        }
    }

    public void CancelCollapse()
    {
        if (collapseCoroutine != null)
        {
            StopCoroutine(collapseCoroutine);
            collapseCoroutine = null;
        }
    }

    public void SetTemporaryDelay(float newDelay)
    {
        // 1. 이미 붕괴 중이면 취소
        CancelCollapse();

        // 2. 계산된 지연 시간 대신 새로운 지연 시간을 임시로 사용
        calculatedCollapseDelay = newDelay;

        // 3. (선택적) 시각적 피드백
        // if(tileRenderer != null) tileRenderer.material.color = Color.blue;

        // 참고: 이 함수는 붕괴를 '시작'하지는 않습니다.
        // StartDelayedCollapse()가 호출되어야 붕괴가 시작됩니다.
        // 만약 이 함수 호출 즉시 붕괴를 시작하고 싶다면 아래 주석 해제
        // StartDelayedCollapse();
    }

    IEnumerator StartCollapseWithDelay()
    {
        // 🔻 [수정] collapseDelay 대신 calculatedCollapseDelay 사용
        yield return new WaitForSeconds(calculatedCollapseDelay);

        if (tileRenderer != null && tileRenderer.material != null)
        {
            StartCoroutine(ControlledFallAndFade());
        }
        else
        {
            Debug.LogError("StartCollapseWithDelay: ControlledFallAndFade 시작 불가 - Renderer 또는 Material이 null입니다!", this.gameObject);
        }
        collapseCoroutine = null; // 코루틴이 끝났음을 표시 (Start 호출 후 바로 실행됨)
    }

    IEnumerator ControlledFallAndFade()
    {
        if (tileRenderer == null || tileRenderer.material == null)
        {
            Debug.LogError("ControlledFallAndFade: 코루틴 시작 시 Renderer 또는 Material이 null입니다! 종료.", this.gameObject);
            // 부모 오브젝트 파괴 시도 (타일이 부모 아래 자식으로 구성된 경우)
            Destroy(gameObject.transform.parent != null ? gameObject.transform.parent.gameObject : gameObject);
            yield break;
        }

        float startTime = Time.time;
        // 부모 Transform 가져오기 (없으면 자신) - 타일 구조에 맞게 조정 필요
        Transform rootTransform = gameObject.transform.parent ?? gameObject.transform;
        Vector3 startPos = rootTransform.position; // 부모의 현재 위치
        Vector3 endPos = startPos + Vector3.down * fallDistance; // 목표 위치
        Color startColor = Color.white; // 기본값

        // Material의 색상 속성 이름 확인 및 가져오기 (URP/HDRP 호환성)
        string colorPropertyName = "_BaseColor"; // URP/HDRP Lit 기본
        if (!tileRenderer.material.HasProperty(colorPropertyName))
        {
            colorPropertyName = "_Color"; // Built-in Standard 또는 다른 셰이더
            if (!tileRenderer.material.HasProperty(colorPropertyName))
            {
                Debug.LogError("Material에 '_BaseColor' 또는 '_Color' 속성이 없습니다! Fade 효과 실패.", gameObject);
                colorPropertyName = null; // 색상 변경 불가 표시
            }
        }

        if (colorPropertyName != null)
        {
            startColor = tileRenderer.material.GetColor(colorPropertyName);
        }

        float timer = 0f;

        // 지정된 시간(fallDuration) 동안 아래로 이동하며 투명해짐
        while (timer < fallDuration)
        {
            timer += Time.deltaTime;
            // 진행도 계산 (0.0 ~ 1.0)
            float t = Mathf.Clamp01(timer / fallDuration);

            // 부모 오브젝트의 위치를 부드럽게 변경 (Lerp 사용)
            rootTransform.position = Vector3.Lerp(startPos, endPos, t);

            // 색상 변경이 가능한 경우에만 알파값 조절
            if (colorPropertyName != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, t); // 알파값 계산 (1 -> 0)
                Color newColor = startColor;
                newColor.a = alpha;
                tileRenderer.material.SetColor(colorPropertyName, newColor); // 색상 적용
            }

            yield return null; // 다음 프레임까지 대기
        }

        // 루프 종료 후 부모 오브젝트 파괴
        Destroy(rootTransform.gameObject);
    }
}