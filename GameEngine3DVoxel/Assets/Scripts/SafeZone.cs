using UnityEngine;

// 1. �� ��ũ��Ʈ�� �� ������Ʈ�� �ٴ�(Plane) �� ���� ���� ������ �� ������Ʈ�� �ٿ��ּ���.
// 2. �� ������Ʈ���� �ݵ�� Collider ������Ʈ�� �־�� �ϰ�, 'Is Trigger'�� üũ�Ǿ� �־�� �մϴ�.
public class SafeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        Debug.Log("!!! Ʈ���� ���� ����! �浹�� ������Ʈ: " + other.name);

        // 1. �浹�� ������Ʈ�� "Player" �±׸� �������� Ȯ���մϴ�.
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                // �÷��̾� �±״� ������ ��ũ��Ʈ�� ���� ���
                return;
            }

            // 2. HP ������ ����մϴ� (float���� �� ��ȯ �ʼ�!)
            float currentHP_Percentage = (float)player.currentHP / player.maxHP;

            // 3. ���� ����Ʈ ���� (���� ���)
            // �� SafeZone ������Ʈ�� ��ġ�� ���ο� ���� �������� �����մϴ�.
            // (���� ���� ������ �ٸ� ������ �ϰ� �ʹٸ� this.transform.position ��� �ٸ� ��ǥ�� �־��ּ���)
            player.UpdateSpawnPoint(this.transform.position);

            // 4. HP 60% ���� ���Ǻ� ȸ��
            if (currentHP_Percentage <= 0.6f)
            {
                // 60% ������ ���
                Debug.Log("HP�� 60% �����Դϴ�. 50%���� ȸ���մϴ�.");

                // 50% (����) HP ���
                int targetHP = player.maxHP / 2;

                // PlayerController�� ȸ�� �Լ� ȣ�� (�̹� 50%�� ������ ȸ������ ����)
                player.HealToAmount(targetHP);
            }
            else
            {
                // 60% �ʰ��� ���
                Debug.Log("HP�� 60%�� �ʰ��մϴ�. ���� ����Ʈ�� �����մϴ�.");
            }
        }
    }
}