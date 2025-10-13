using UnityEngine;

public class PlayerTileDetector : MonoBehaviour
{
    public float searchRadius = 5f;        // 타일 검색 반경
    public float newCollapseDelay = 20f;   // Q 키를 눌렀을 때 변경할 붕괴 시간

    // Character Controller가 다른 오브젝트와 충돌했을 때 호출됩니다.
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        VoxelCollapse tileScript = hit.gameObject.GetComponent<VoxelCollapse>();

        if (tileScript != null)
        {
            // 💡 핵심 1: 붕괴가 이미 시작되지 않은 경우에만 StartDelayedCollapse()를 호출합니다.
            // 이렇게 해야 Q 스킬로 딜레이가 20초로 변경된 타일도 밟는 순간 붕괴가 시작됩니다.
            if (!tileScript.IsCollapseStarted)
            {
                // StartDelayedCollapse는 내부적으로 tileScript.collapseDelay (5초 또는 20초) 값을 사용해야 합니다.
                tileScript.StartDelayedCollapse();
            }
        }
    }

    void Update()
    {
        // Q 키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetSurroundingTileDelay(searchRadius, newCollapseDelay);
            Debug.Log($"주변 {searchRadius}m 반경 타일의 붕괴 시간을 {newCollapseDelay}초로 변경했습니다. (밟으면 적용)");
        }
    }

    void SetSurroundingTileDelay(float radius, float newDelay)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);

        foreach (var hitCollider in hitColliders)
        {
            VoxelCollapse tileScript = hitCollider.GetComponent<VoxelCollapse>();

            if (tileScript != null)
            {
                // 1. 타일의 붕괴 시간(collapseDelay)을 새로운 시간(20초)으로 변경합니다.
                tileScript.collapseDelay = newDelay;

                // 2. 💡 핵심 2: 만약 붕괴가 이미 시작되었다면 (예: 5초 카운트다운 중이었다면),
                //    그 카운트다운을 취소합니다.
                if (tileScript.IsCollapseStarted)
                {
                    tileScript.CancelCollapse();
                }

                // (선택 사항: 시각적 피드백)
                // tileScript.GetComponent<Renderer>().material.color = Color.blue;
            }
        }
    }
}