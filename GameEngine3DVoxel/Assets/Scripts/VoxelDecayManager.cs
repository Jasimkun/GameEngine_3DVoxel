using UnityEngine;
using System.Collections; // Coroutine을 사용하기 위해 필요합니다.

public class VoxelDecayManager : MonoBehaviour
{
    // === 설정 변수 ===
    [Header("Decay Settings")]
    public float decayTime = 5.0f; // 붕괴까지 걸리는 총 시간 (5초)
    public Color decayColor = Color.red; // 붕괴 시 사용할 최종 색상 (빨간색)

    // === 내부 변수 ===
    private bool isDecaying = false;
    private Renderer tileRenderer;
    private Material tileMaterial;
    private Color originalColor;

    // 이 타일이 필드 발생기에 의해 시간 지연 효과를 받았는지 추적
    private float timeModifier = 0f;

    void Start()
    {
        // 타일의 Renderer 컴포넌트를 가져옵니다. (스크립트가 Renderer가 있는 default에 직접 붙어 있으므로)
        tileRenderer = GetComponent<Renderer>();

        // 이 시점에서는 Renderer가 반드시 있어야 합니다.
        if (tileRenderer == null)
        {
            Debug.LogError("VoxelDecayManager: Renderer 컴포넌트를 찾을 수 없습니다. (스크립트 위치 확인 요망)", this);
            enabled = false;
            return;
        }

        // Material을 가져와서 복사본을 생성합니다. (다른 타일에 영향을 주지 않도록)
        tileMaterial = tileRenderer.material;
        originalColor = tileMaterial.color;
    }

    // 플레이어와의 접촉을 감지하는 함수
    private void OnTriggerEnter(Collider other)
    {
        // "Player" 태그를 가진 오브젝트가 접촉했는지 확인합니다.
        if (other.CompareTag("Player") && !isDecaying)
        {
            // 타일 오브젝트의 Renderer가 아닌, 이 타일을 밟은 충돌체의 Renderer를 찾지 않도록 주의
            StartDecayProcess();
        }
    }

    // 붕괴 과정을 시작하는 함수
    private void StartDecayProcess()
    {
        isDecaying = true;
        // 코루틴을 사용하여 붕괴 타이머를 시작합니다.
        StartCoroutine(DecayCoroutine());
    }

    // 외부에서 호출하여 붕괴 시간을 연장하는 함수 (필드 발생기 시스템)
    public void IncreaseDecayTime(float timeToAdd)
    {
        // 현재 타이머에 추가 시간을 더합니다.
        timeModifier += timeToAdd;
        Debug.Log($"타일 타이머 연장: {gameObject.name}의 붕괴 시간이 {timeToAdd}초 연장되었습니다. 총 지연 시간: {timeModifier}초");

        // 이미 붕괴 과정이 시작되었더라도 타이머 연장이 가능해야 합니다.
        // 현재 코루틴을 중지하고 새로운 타이머로 다시 시작할 필요가 있습니다.
        if (isDecaying)
        {
            StopAllCoroutines();
            // 연장된 시간으로 새로운 코루틴을 시작합니다.
            StartCoroutine(DecayCoroutine());
        }
    }

    // 붕괴 타이머와 색상 변화를 관리하는 코루틴
    IEnumerator DecayCoroutine()
    {
        float timer = 0f;

        // 최종 붕괴 시간은 기본 시간(5초) + 외부에서 추가된 지연 시간(timeModifier)
        float finalDecayTime = decayTime + timeModifier;

        // 타이머가 최종 붕괴 시간에 도달할 때까지 반복
        while (timer < finalDecayTime)
        {
            // 시간 경과 (유니티 프레임 시간)
            timer += Time.deltaTime;

            // 경과 비율 (0.0f ~ 1.0f)
            float progress = timer / finalDecayTime;

            // 색상 변화 계산
            Color currentColor = Color.Lerp(originalColor, decayColor, progress);

            // 불투명도(알파값) 설정: 5초 동안 0%에서 100%로 변화합니다.
            // 5초 기준 1초에 20%씩 증가하도록 progress를 사용합니다.
            currentColor.a = progress;

            // 머티리얼에 색상 적용
            // 투명도 조절을 위해 머티리얼의 렌더링 모드를 반드시 'Fade' 또는 'Transparent'로 설정해야 합니다!
            tileMaterial.color = currentColor;

            yield return null; // 다음 프레임까지 대기
        }

        // 5초가 경과한 후 (타이머가 끝난 후)
        Debug.Log($"타일 붕괴 완료: {gameObject.name}이 붕괴되었습니다.");

        // 최종적으로 빨간색 불투명으로 설정
        tileMaterial.color = decayColor;
        tileMaterial.color = new Color(decayColor.r, decayColor.g, decayColor.b, 1f);

        // 타일을 비활성화하여 붕괴 처리 (다시 밟을 수 없게 됨)
        gameObject.SetActive(false);
    }
}