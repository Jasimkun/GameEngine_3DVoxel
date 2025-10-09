using UnityEngine;

public class PlayerTileDetector : MonoBehaviour
{
    // Character Controller�� �ٸ� ������Ʈ�� �ε����� �� ȣ��˴ϴ�.
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // ���� ������Ʈ(hit.gameObject)�� Ÿ������ Ȯ���ϰ�, ��ũ��Ʈ�� �ִ��� Ȯ���մϴ�.
        VoxelCollapse tileScript = hit.gameObject.GetComponent<VoxelCollapse>();

        // Ÿ���̰�, ���� �ر� ������ ���۵��� �ʾҴٸ�
        if (tileScript != null && !tileScript.isCollapsing)
        {
            // Ÿ���� �ر� �Լ��� �÷��̾� ��ũ��Ʈ���� ���� ȣ���մϴ�.
            tileScript.StartDelayedCollapse();
        }
    }
}