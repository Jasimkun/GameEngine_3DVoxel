/* * IDamageable.cs
 * �� ��ũ��Ʈ�� ��� ��(Enemy, Teleport ��)�� 
 * �������� ����� "�ź���" ������ �մϴ�.
 */
public interface IDamageable
{
    // �� "�ź���"�� ���� ��ũ��Ʈ��
    // �ݵ�� TakeDamage(int damage) �Լ��� ������ �־�� �մϴ�.
    void TakeDamage(int damage);
}