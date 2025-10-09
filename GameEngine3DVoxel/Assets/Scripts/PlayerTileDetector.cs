using UnityEngine;

public class PlayerTileDetector : MonoBehaviour
{
    // === Q 키 붕괴 시간 변경 설정 ===
    public float searchRadius = 5f;        // 타일 검색 반경 (5x5 영역 대신 5 유닛 반경 사용)
    public float newCollapseDelay = 20f;   // Q 키를 눌렀을 때 변경할 붕괴 시간

    // Character Controller가 다른 오브젝트와 충돌했을 때 호출되는 특별한 함수입니다.
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 1. 충돌한 오브젝트에서 VoxelCollapse 스크립트를 찾습니다.
        VoxelCollapse tileScript = hit.gameObject.GetComponent<VoxelCollapse>();

        // 2. 붕괴 가능한 타일이라면 붕괴를 시작하도록 명령합니다.
        if (tileScript != null)
        {
            tileScript.StartDelayedCollapse();
        }
    }

    void Update()
    {

        // Q 키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetSurroundingTileDelay(searchRadius, newCollapseDelay);
            Debug.Log($"주변 {searchRadius}m 반경 타일의 붕괴 시간을 {newCollapseDelay}초로 변경했습니다.");
        }
    }

    void SetSurroundingTileDelay(float radius, float newDelay)
    {
        // 1. 플레이어 주변의 모든 콜라이더를 검색합니다.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);

        // 2. 검색된 모든 콜라이더를 순회합니다.
        foreach (var hitCollider in hitColliders)
        {
            // 3. 검색된 오브젝트가 붕괴 가능한 타일인지 확인합니다.
            VoxelCollapse tileScript = hitCollider.GetComponent<VoxelCollapse>();

            // 4. VoxelCollapse 스크립트가 있다면 (즉, 붕괴 가능한 타일이라면)
            if (tileScript != null)
            {
                // 5. 타일의 붕괴 시간(collapseDelay)을 새로운 시간으로 변경합니다.
                tileScript.collapseDelay = newDelay;

                // (선택 사항) 변경된 타일의 색상을 시각적으로 표시할 수 있습니다.
                // tileScript.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
            }
        }
    }
}