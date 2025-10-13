using UnityEngine;
using System.Collections;

public class VoxelCollapse : MonoBehaviour
{
    // === 설정 변수 ===
    public float collapseDelay = 5.0f;     // 붕괴 시작까지의 대기 시간 (PlayerTileDetector에서 변경됨)
    public float fadeOutDuration = 1.0f;   // 완전히 투명해지는 데 걸리는 시간

    // === 상태 추적 변수 ===
    private Coroutine collapseCoroutine; // 진행 중인 붕괴 대기 코루틴을 추적합니다.

    // 💡 새 속성: 붕괴 카운트다운이 현재 진행 중인지 외부에 알려줍니다.
    public bool IsCollapseStarted => collapseCoroutine != null;

    // === 내부 컴포넌트 ===
    private Renderer tileRenderer;
    private Rigidbody tileRigidbody;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();

        // Rigidbody 설정 (원래 로직 유지)
        tileRigidbody = GetComponent<Rigidbody>();
        if (tileRigidbody == null)
        {
            tileRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        tileRigidbody.isKinematic = true;
        tileRigidbody.useGravity = false;
    }

    // 플레이어 스크립트가 충돌을 감지했을 때 이 함수를 호출합니다.
    public void StartDelayedCollapse()
    {
        // 중복 호출 방지 (이미 카운트다운 중이라면 무시)
        if (collapseCoroutine == null)
        {
            // 붕괴 대기 코루틴을 시작하고 추적합니다.
            // 이 코루틴이 끝날 때까지 IsCollapseStarted는 true입니다.
            collapseCoroutine = StartCoroutine(StartCollapseWithDelay());
        }
    }

    // 💡 새 함수: 외부(PlayerTileDetector)에서 붕괴 카운트다운을 강제로 중지합니다.
    public void CancelCollapse()
    {
        if (collapseCoroutine != null)
        {
            StopCoroutine(collapseCoroutine);
            collapseCoroutine = null; // 코루틴 중지 후 반드시 null로 설정

            // (선택 사항) 색상 복구 등의 초기화 작업
            // tileRenderer.material.SetColor("_BaseColor", Color.white);
        }
    }

    // 5초 (또는 변경된 시간)를 기다리는 코루틴
    IEnumerator StartCollapseWithDelay()
    {
        // 'collapseDelay' 변수에 저장된 시간만큼 대기 (5초 또는 20초)
        yield return new WaitForSeconds(collapseDelay);

        // 대기 후 추락 및 투명화 시작
        StartCoroutine(FallDown());
        StartCoroutine(FadeOut());

        collapseCoroutine = null; // 붕괴가 시작되면 대기 코루틴은 끝났으므로 null로 설정
    }

    // 타일을 추락시키는 코루틴 (원래 로직 유지)
    IEnumerator FallDown()
    {
        tileRigidbody.isKinematic = false;
        tileRigidbody.useGravity = true;
        yield break;
    }

    // 타일을 서서히 투명하게 만드는 코루틴 (원래 로직 유지)
    IEnumerator FadeOut()
    {
        // ... (이전과 동일한 투명화 로직)
        Color startColor = tileRenderer.material.GetColor("_BaseColor");
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
            Color newColor = startColor;
            newColor.a = alpha;
            tileRenderer.material.SetColor("_BaseColor", newColor);
            yield return null;
        }

        Destroy(gameObject);
    }
}