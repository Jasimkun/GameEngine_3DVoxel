using UnityEngine;

public class PlayerTileDetector : MonoBehaviour
{
    // Character Controller가 다른 오브젝트와 부딪혔을 때 호출됩니다.
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 밟은 오브젝트(hit.gameObject)가 타일인지 확인하고, 스크립트가 있는지 확인합니다.
        VoxelCollapse tileScript = hit.gameObject.GetComponent<VoxelCollapse>();

        // 타일이고, 아직 붕괴 과정이 시작되지 않았다면
        if (tileScript != null && !tileScript.isCollapsing)
        {
            // 타일의 붕괴 함수를 플레이어 스크립트에서 직접 호출합니다.
            tileScript.StartDelayedCollapse();
        }
    }
}