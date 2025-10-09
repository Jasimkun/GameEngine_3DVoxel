using UnityEngine;
using System.Collections;

public class VoxelCollapse : MonoBehaviour
{
    // === ���� ���� ===
    // 'public'���� �����Ǿ�� PlayerTileDetector���� �� ���� ������ �� �ֽ��ϴ�.
    public float collapseDelay = 5.0f;     // �ر� ���۱����� ��� �ð�
    public float fadeOutDuration = 1.0f;   // ������ ���������� �� �ɸ��� �ð�

    // === ���� ���� ===
    // Ÿ���� �ر� ���� ������ Ȯ�� (�ܺ� ��ũ��Ʈ ���� ����)
    [HideInInspector] public bool isCollapsing = false;

    // === ���� ������Ʈ ===
    private Renderer tileRenderer;
    private Rigidbody tileRigidbody;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();

        // Rigidbody�� ������ �߰��ϰ� �����մϴ�.
        tileRigidbody = GetComponent<Rigidbody>();
        if (tileRigidbody == null)
        {
            tileRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        // Ÿ���� ������ �� ������ �ϵ��� 'Kinematic'���� ����
        tileRigidbody.isKinematic = true;
        tileRigidbody.useGravity = false;
    }

    // �÷��̾� ��ũ��Ʈ�� �浹�� �������� �� �� �Լ��� ȣ���մϴ�.
    public void StartDelayedCollapse()
    {
        // �ߺ� ȣ�� ����
        if (!isCollapsing)
        {
            isCollapsing = true;
            StartCoroutine(StartCollapseWithDelay());
        }
    }

    // 5�� (�Ǵ� ����� �ð�)�� ��ٸ��� �ڷ�ƾ
    IEnumerator StartCollapseWithDelay()
    {
        // 'collapseDelay' ������ ����� �ð���ŭ ���
        yield return new WaitForSeconds(collapseDelay);

        // ��� �� �߶� �� ����ȭ ����
        StartCoroutine(FallDown());
        StartCoroutine(FadeOut());
    }

    // Ÿ���� �߶���Ű�� �ڷ�ƾ
    IEnumerator FallDown()
    {
        // ������ Ǯ�� �߷��� ������ �ް� �մϴ�. (�߶� ����)
        tileRigidbody.isKinematic = false;
        tileRigidbody.useGravity = true;
        yield break;
    }

    // Ÿ���� ������ �����ϰ� ����� �ڷ�ƾ
    IEnumerator FadeOut()
    {
        // URP ȣȯ �ڵ�: Material�� �⺻ ����(���� ����)�� �����ɴϴ�.
        Color startColor = tileRenderer.material.GetColor("_BaseColor");
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;

            // ���� ���� 1 (����)���� 0 (����)���� �ε巴�� ����
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);

            Color newColor = startColor;
            newColor.a = alpha;

            // URP ȣȯ �ڵ�: ������ Material�� �����մϴ�.
            tileRenderer.material.SetColor("_BaseColor", newColor);

            yield return null;
        }

        // ������ ������� �޸𸮿��� �����մϴ�.
        Destroy(gameObject);
    }
}