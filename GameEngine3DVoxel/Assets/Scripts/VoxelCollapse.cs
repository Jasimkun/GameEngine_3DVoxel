using UnityEngine;
using System.Collections;

public class VoxelCollapse : MonoBehaviour
{
    // === 설정 변수 ===
    public float collapseDelay = 5.0f;       // 붕괴 시작까지의 대기 시간
    public float fadeOutDuration = 1.0f;     // 완전히 투명해지는 데 걸리는 시간

    [Header("Fall Settings")]
    public float fallDistance = 1.0f;       // 💡 추락할 거리 (1m)
    public float fallDuration = 0.5f;       // 💡 1m 추락하는 데 걸리는 시간

    // === 상태 추적 변수 ===
    private Coroutine collapseCoroutine;

    // 💡 새 속성: 붕괴 카운트다운이 현재 진행 중인지 외부에 알려줍니다.
    public bool IsCollapseStarted => collapseCoroutine != null;

    // === 내부 컴포넌트 ===
    private Renderer tileRenderer;
    private Rigidbody tileRigidbody;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();

        // Rigidbody 설정 (물리적 추락 대신 제어된 이동을 위해 필요)
        tileRigidbody = GetComponent<Rigidbody>();
        if (tileRigidbody == null)
        {
            tileRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        tileRigidbody.isKinematic = true;  // 초기에는 Kinematic 유지 (제어 이동)
        tileRigidbody.useGravity = false;
    }

    // 플레이어 스크립트가 충돌을 감지했을 때 이 함수를 호출합니다.
    public void StartDelayedCollapse()
    {
        // 중복 호출 방지
        if (collapseCoroutine == null)
        {
            collapseCoroutine = StartCoroutine(StartCollapseWithDelay());
        }
    }

    // 외부(PlayerTileDetector)에서 붕괴 카운트다운을 강제로 중지합니다.
    public void CancelCollapse()
    {
        if (collapseCoroutine != null)
        {
            StopCoroutine(collapseCoroutine);
            collapseCoroutine = null;

            // (선택 사항) 색상 복구 등의 초기화 작업
        }
    }

    // N초를 기다리는 코루틴
    IEnumerator StartCollapseWithDelay()
    {
        // 'collapseDelay' 변수에 저장된 시간만큼 대기 (5초 또는 20초)
        yield return new WaitForSeconds(collapseDelay);

        // 대기 후 제어된 추락 및 투명화 시작
        StartCoroutine(ControlledFallAndFade());

        collapseCoroutine = null; // 대기 코루틴은 끝났으므로 null로 설정
    }

    // 💡 새로운 코루틴: 1m 아래로 부드럽게 추락하며 투명화합니다.
    IEnumerator ControlledFallAndFade()
    {
        float startTime = Time.time;
        Vector3 startPos = transform.position;
        // 1m 아래 지점 계산
        Vector3 endPos = startPos + Vector3.down * fallDistance;

        // FadeOut/색상 변화 준비
        Color startColor = tileRenderer.material.GetColor("_BaseColor");

        float timer = 0f;

        // Kinematic 상태 유지 (물리 엔진 대신 Lerp로 부드럽게 제어 이동)
        tileRigidbody.isKinematic = true;
        tileRigidbody.useGravity = false;

        // fallDuration 시간 동안 반복
        while (timer < fallDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fallDuration; // 0.0f to 1.0f

            // 1. 추락: Lerp로 1m 아래로 부드럽게 이동
            transform.position = Vector3.Lerp(startPos, endPos, t);

            // 2. 투명화: 동시에 투명도 감소 (fadeOutDuration 대신 fallDuration과 동일하게 t를 사용)
            float alpha = Mathf.Lerp(1f, 0f, t);
            Color newColor = startColor;
            newColor.a = alpha;

            // Material에 색상 적용
            // 투명도를 지원하는 셰이더를 사용해야 합니다.
            tileRenderer.material.SetColor("_BaseColor", newColor);

            yield return null;
        }

        // 붕괴 완료: 타일을 파괴합니다.
        Destroy(gameObject);
    }
}