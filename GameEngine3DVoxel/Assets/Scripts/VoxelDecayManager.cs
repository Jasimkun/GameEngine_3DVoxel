using UnityEngine;
using System.Collections; // Coroutine�� ����ϱ� ���� �ʿ��մϴ�.

public class VoxelDecayManager : MonoBehaviour
{
    // === ���� ���� ===
    [Header("Decay Settings")]
    public float decayTime = 5.0f; // �ر����� �ɸ��� �� �ð� (5��)
    public Color decayColor = Color.red; // �ر� �� ����� ���� ���� (������)

    // === ���� ���� ===
    private bool isDecaying = false;
    private Renderer tileRenderer;
    private Material tileMaterial;
    private Color originalColor;

    // �� Ÿ���� �ʵ� �߻��⿡ ���� �ð� ���� ȿ���� �޾Ҵ��� ����
    private float timeModifier = 0f;

    void Start()
    {
        // Ÿ���� Renderer ������Ʈ�� �����ɴϴ�. (��ũ��Ʈ�� Renderer�� �ִ� default�� ���� �پ� �����Ƿ�)
        tileRenderer = GetComponent<Renderer>();

        // �� ���������� Renderer�� �ݵ�� �־�� �մϴ�.
        if (tileRenderer == null)
        {
            Debug.LogError("VoxelDecayManager: Renderer ������Ʈ�� ã�� �� �����ϴ�. (��ũ��Ʈ ��ġ Ȯ�� ���)", this);
            enabled = false;
            return;
        }

        // Material�� �����ͼ� ���纻�� �����մϴ�. (�ٸ� Ÿ�Ͽ� ������ ���� �ʵ���)
        tileMaterial = tileRenderer.material;
        originalColor = tileMaterial.color;
    }

    // �÷��̾���� ������ �����ϴ� �Լ�
    private void OnTriggerEnter(Collider other)
    {
        // "Player" �±׸� ���� ������Ʈ�� �����ߴ��� Ȯ���մϴ�.
        if (other.CompareTag("Player") && !isDecaying)
        {
            // Ÿ�� ������Ʈ�� Renderer�� �ƴ�, �� Ÿ���� ���� �浹ü�� Renderer�� ã�� �ʵ��� ����
            StartDecayProcess();
        }
    }

    // �ر� ������ �����ϴ� �Լ�
    private void StartDecayProcess()
    {
        isDecaying = true;
        // �ڷ�ƾ�� ����Ͽ� �ر� Ÿ�̸Ӹ� �����մϴ�.
        StartCoroutine(DecayCoroutine());
    }

    // �ܺο��� ȣ���Ͽ� �ر� �ð��� �����ϴ� �Լ� (�ʵ� �߻��� �ý���)
    public void IncreaseDecayTime(float timeToAdd)
    {
        // ���� Ÿ�̸ӿ� �߰� �ð��� ���մϴ�.
        timeModifier += timeToAdd;
        Debug.Log($"Ÿ�� Ÿ�̸� ����: {gameObject.name}�� �ر� �ð��� {timeToAdd}�� ����Ǿ����ϴ�. �� ���� �ð�: {timeModifier}��");

        // �̹� �ر� ������ ���۵Ǿ����� Ÿ�̸� ������ �����ؾ� �մϴ�.
        // ���� �ڷ�ƾ�� �����ϰ� ���ο� Ÿ�̸ӷ� �ٽ� ������ �ʿ䰡 �ֽ��ϴ�.
        if (isDecaying)
        {
            StopAllCoroutines();
            // ����� �ð����� ���ο� �ڷ�ƾ�� �����մϴ�.
            StartCoroutine(DecayCoroutine());
        }
    }

    // �ر� Ÿ�̸ӿ� ���� ��ȭ�� �����ϴ� �ڷ�ƾ
    IEnumerator DecayCoroutine()
    {
        float timer = 0f;

        // ���� �ر� �ð��� �⺻ �ð�(5��) + �ܺο��� �߰��� ���� �ð�(timeModifier)
        float finalDecayTime = decayTime + timeModifier;

        // Ÿ�̸Ӱ� ���� �ر� �ð��� ������ ������ �ݺ�
        while (timer < finalDecayTime)
        {
            // �ð� ��� (����Ƽ ������ �ð�)
            timer += Time.deltaTime;

            // ��� ���� (0.0f ~ 1.0f)
            float progress = timer / finalDecayTime;

            // ���� ��ȭ ���
            Color currentColor = Color.Lerp(originalColor, decayColor, progress);

            // ������(���İ�) ����: 5�� ���� 0%���� 100%�� ��ȭ�մϴ�.
            // 5�� ���� 1�ʿ� 20%�� �����ϵ��� progress�� ����մϴ�.
            currentColor.a = progress;

            // ��Ƽ���� ���� ����
            // ���� ������ ���� ��Ƽ������ ������ ��带 �ݵ�� 'Fade' �Ǵ� 'Transparent'�� �����ؾ� �մϴ�!
            tileMaterial.color = currentColor;

            yield return null; // ���� �����ӱ��� ���
        }

        // 5�ʰ� ����� �� (Ÿ�̸Ӱ� ���� ��)
        Debug.Log($"Ÿ�� �ر� �Ϸ�: {gameObject.name}�� �ر��Ǿ����ϴ�.");

        // ���������� ������ ���������� ����
        tileMaterial.color = decayColor;
        tileMaterial.color = new Color(decayColor.r, decayColor.g, decayColor.b, 1f);

        // Ÿ���� ��Ȱ��ȭ�Ͽ� �ر� ó�� (�ٽ� ���� �� ���� ��)
        gameObject.SetActive(false);
    }
}