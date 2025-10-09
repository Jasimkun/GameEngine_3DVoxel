using UnityEngine;
using System.Collections;

public class VoxelCollapse : MonoBehaviour
{
    // === 설정 변수 ===
    public float collapseDelay = 5.0f;     // 붕괴 시작까지의 대기 시간
    public float fadeOutDuration = 1.0f;   // 완전히 투명해지는 데 걸리는 시간

    // === 상태 변수 ===
    // [HideInInspector]를 사용하면 Inspector 창에서 이 변수를 숨길 수 있습니다.
    [HideInInspector] public bool isCollapsing = false;

    // === 내부 컴포넌트 ===
    private Renderer tileRenderer;
    private Rigidbody tileRigidbody;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();

        // Rigidbody가 없으면 추가하고 고정합니다.
        tileRigidbody = GetComponent<Rigidbody>();
        if (tileRigidbody == null)
        {
            tileRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        // 타일이 물리적 벽 역할을 하도록 설정
        tileRigidbody.isKinematic = true;
        tileRigidbody.useGravity = false;
    }

    // 플레이어 스크립트가 충돌을 감지했을 때 이 함수를 호출합니다.
    public void StartDelayedCollapse()
    {
        // 중복 호출 방지
        if (!isCollapsing)
        {
            isCollapsing = true;
            StartCoroutine(StartCollapseWithDelay());
        }
    }

    // 5초를 기다리는 코루틴
    IEnumerator StartCollapseWithDelay()
    {
        // 5초 동안 대기
        yield return new WaitForSeconds(collapseDelay);

        // 5초 후 추락 및 투명화 시작
        StartCoroutine(FallDown());
        StartCoroutine(FadeOut());
    }

    // 타일을 추락시키는 코루틴
    IEnumerator FallDown()
    {
        // 고정을 풀고 중력의 영향을 받게 합니다. (추락 시작)
        tileRigidbody.isKinematic = false;
        tileRigidbody.useGravity = true;
        yield break;
    }

    // 타일을 서서히 투명하게 만드는 코루틴
    IEnumerator FadeOut()
    {
        // URP 호환 코드: Material 색상(흰색)을 가져와서 알파(투명도)만 조절
        Color startColor = tileRenderer.material.GetColor("_BaseColor");
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;

            // 알파 값을 1 (선명)에서 0 (투명)으로 부드럽게 보간
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);

            Color newColor = startColor;
            newColor.a = alpha;

            // URP 호환 코드: 투명도를 적용합니다.
            tileRenderer.material.SetColor("_BaseColor", newColor);

            yield return null;
        }

        // 완전히 사라지면 없앱니다.
        Destroy(gameObject);
    }
}