using UnityEngine;

public class PlayerTileDetector : MonoBehaviour
{
    public float searchRadius = 5f;
    public float newCollapseDelay = 20f;

    // 💡 새로운 변수: 스킬 사용 여부를 추적합니다.
    private bool isSkillUsed = false;

    // Character Controller가 다른 오브젝트와 충돌했을 때 호출됩니다.
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        VoxelCollapse tileScript = hit.gameObject.GetComponent<VoxelCollapse>();

        if (tileScript != null)
        {
            if (!tileScript.IsCollapseStarted)
            {
                tileScript.StartDelayedCollapse();
            }
        }
    }

    void Update()
    {
        // 💡 1. 스킬이 이미 사용되었는지 확인합니다.
        if (isSkillUsed)
        {
            return; // 이미 사용했으면 Q 키 입력을 무시하고 종료
        }

        // Q 키를 눌렀을 때
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetSurroundingTileDelay(searchRadius, newCollapseDelay);

            // 💡 2. 스킬 사용 후, isSkillUsed를 true로 설정하여 재사용을 방지합니다.
            isSkillUsed = true;

            Debug.Log($"[SYSTEM] 타일의 붕괴 속도가 늦어집니다.");
            Debug.Log("이 스킬은 레벨마다 한 번씩이야!");
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
                tileScript.SetTemporaryDelay(newDelay);

                // 2. 만약 붕괴가 이미 시작되었다면, 그 카운트다운을 취소합니다.
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