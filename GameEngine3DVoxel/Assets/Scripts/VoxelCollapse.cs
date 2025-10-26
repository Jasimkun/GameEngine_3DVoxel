using UnityEngine;
using System.Collections;

public class VoxelCollapse : MonoBehaviour
{
    // === 설정 변수 ===
    public float collapseDelay = 5.0f;
    public float fadeOutDuration = 1.0f; // 이 변수는 현재 사용되지 않음

    [Header("Fall Settings")]
    public float fallDistance = 1.0f;
    public float fallDuration = 0.5f;

    // === 상태 추적 변수 ===
    private Coroutine collapseCoroutine;
    public bool IsCollapseStarted => collapseCoroutine != null;

    // === 내부 컴포넌트 ===
    private Renderer tileRenderer;
    private Rigidbody tileRigidbody; // Rigidbody는 부모에 있는 것이 좋을 수 있음

    void Awake()
    {
        // 📢 스크립트와 Renderer가 같은 오브젝트에 있으므로 GetComponent 사용
        tileRenderer = GetComponent<Renderer>();

        // 찾았는지 로그 출력
        if (tileRenderer != null)
        {
            //Debug.Log($"Awake: Renderer 찾기 성공! 오브젝트: {gameObject.name}", this.gameObject);
            // Material도 있는지 확인
            if (tileRenderer.material == null)
            {
                Debug.LogError("Awake: Renderer는 찾았지만 Material이 없습니다! Material을 할당해주세요.", this.gameObject);
            }
        }
        else
        {
            Debug.LogError("Awake: 이 오브젝트({gameObject.name})에서 Renderer 컴포넌트를 찾을 수 없습니다! 확인 필요!", this.gameObject);
        }

        // Rigidbody 설정 (자식 오브젝트보다는 부모 오브젝트에 있는 것이 일반적)
        // 만약 Rigidbody가 부모에 있다면 GetComponentInParent<Rigidbody>() 사용 고려
        tileRigidbody = GetComponent<Rigidbody>();
        if (tileRigidbody == null)
        {
            // 필요하다면 부모에서 찾아보기
            // tileRigidbody = GetComponentInParent<Rigidbody>();
            // 그래도 없다면 추가 (추가 시 부모/자식 구조 고려 필요)
            if (tileRigidbody == null)
            {
                // Debug.LogWarning("Rigidbody 컴포넌트를 찾을 수 없어 새로 추가합니다. 부모/자식 구조를 확인하세요.", gameObject);
                // tileRigidbody = gameObject.AddComponent<Rigidbody>(); // 자식에 추가하는 것이 맞는지 확인 필요
            }
        }
        // Rigidbody 설정은 일단 보류 (오류와 직접 관련 없을 수 있음)
        // if(tileRigidbody != null) {
        //     tileRigidbody.isKinematic = true;
        //     tileRigidbody.useGravity = false;
        // }
    }

    public void StartDelayedCollapse()
    {
        if (collapseCoroutine == null)
        {
            // 코루틴 시작 전 Renderer null 체크
            if (tileRenderer == null)
            {
                Debug.LogError("StartDelayedCollapse: 코루틴 시작 전인데 tileRenderer가 null입니다! Awake 확인 필요!", this.gameObject);
                return;
            }
            // Material null 체크
            if (tileRenderer.material == null)
            {
                Debug.LogError("StartDelayedCollapse: 코루틴 시작 전인데 tileRenderer.material이 null입니다!", this.gameObject);
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

    IEnumerator StartCollapseWithDelay()
    {
        yield return new WaitForSeconds(collapseDelay);

        if (tileRenderer != null && tileRenderer.material != null) // Material 체크 추가
        {
            StartCoroutine(ControlledFallAndFade());
        }
        else
        {
            Debug.LogError("StartCollapseWithDelay: ControlledFallAndFade 시작 불가 - tileRenderer 또는 material이 null입니다!", this.gameObject);
        }

        collapseCoroutine = null;
    }

    IEnumerator ControlledFallAndFade()
    {
        // 코루틴 시작 시점에 최종 확인
        if (tileRenderer == null || tileRenderer.material == null)
        {
            Debug.LogError("ControlledFallAndFade: 코루틴 시작 시 tileRenderer 또는 material이 null입니다! 종료.", this.gameObject);
            Destroy(gameObject.transform.parent != null ? gameObject.transform.parent.gameObject : gameObject); // 부모 오브젝트 파괴 시도
            yield break;
        }

        float startTime = Time.time;
        // 🚨 Rigidbody를 사용하지 않으므로 부모 오브젝트의 위치를 기준으로 해야 할 수 있음
        Transform rootTransform = gameObject.transform.parent ?? gameObject.transform; // 부모 Transform 가져오기 (없으면 자신)
        Vector3 startPos = rootTransform.position; // 부모의 위치 사용
        Vector3 endPos = startPos + Vector3.down * fallDistance;
        Color startColor = Color.white; // 기본값

        // _BaseColor 속성이 있는지 확인하고 가져오기
        if (tileRenderer.material.HasProperty("_BaseColor"))
        {
            startColor = tileRenderer.material.GetColor("_BaseColor");
        }
        else if (tileRenderer.material.HasProperty("_Color"))
        { // _Color 속성도 확인
            startColor = tileRenderer.material.GetColor("_Color");
            Debug.LogWarning("'_BaseColor' 속성을 찾을 수 없어 '_Color' 속성을 사용합니다.", gameObject);
        }
        else
        {
            Debug.LogError("Material에 '_BaseColor' 또는 '_Color' 속성이 없습니다! 셰이더를 확인하세요.", gameObject);
            // 색상 변경 없이 진행하거나 여기서 멈출 수 있음
        }


        float timer = 0f;

        // Rigidbody 대신 Transform 직접 제어 (부모 오브젝트 이동)
        while (timer < fallDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fallDuration);

            rootTransform.position = Vector3.Lerp(startPos, endPos, t); // 부모 위치 변경

            float alpha = Mathf.Lerp(1f, 0f, t);
            Color newColor = startColor;
            newColor.a = alpha;

            // Material 속성 이름 확인 후 색상 적용
            if (tileRenderer.material.HasProperty("_BaseColor"))
            {
                tileRenderer.material.SetColor("_BaseColor", newColor);
            }
            else if (tileRenderer.material.HasProperty("_Color"))
            {
                tileRenderer.material.SetColor("_Color", newColor);
            }

            yield return null;
        }

        Destroy(rootTransform.gameObject); // 부모 오브젝트 파괴
    }
}