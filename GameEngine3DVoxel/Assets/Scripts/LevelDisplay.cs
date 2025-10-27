using UnityEngine;
using TMPro; // TextMeshPro ���
using System.Collections;

public class LevelDisplay : MonoBehaviour
{
    public TMP_Text levelText; // Inspector���� ������ TextMeshPro ������Ʈ
    public float displayDuration = 2f; // �ؽ�Ʈ�� ���̴� �ð� (3��)
    public float fadeDuration = 1f; // �ؽ�Ʈ�� ������� �ð� (1��)

    private Coroutine fadeCoroutine; // ���� ���� �ڷ�ƾ �����

    void Awake()
    {
        // ������ �� �ؽ�Ʈ�� �� ���̵��� ���İ� 0���� ����
        if (levelText != null)
        {
            levelText.color = new Color(levelText.color.r, levelText.color.g, levelText.color.b, 0);
        }
        else
        {
            Debug.LogError("LevelDisplay: Level Text�� ������� �ʾҽ��ϴ�!");
        }
    }

    // GameManager�� ȣ���� �Լ�
    public void ShowLevel(int levelNumber)
    {
        if (levelText == null) return;

        // ������ ���� ���� ���̵� �ڷ�ƾ�� �ִٸ� ����
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // �ؽ�Ʈ ���� �� ǥ��
        levelText.text = "���� " + levelNumber;
        levelText.color = new Color(levelText.color.r, levelText.color.g, levelText.color.b, 1); // ���İ� 1 (���� ������)

        // ���̵� �ƿ� �ڷ�ƾ ����
        fadeCoroutine = StartCoroutine(FadeOutText());
    }

    // ������ ������� �ϴ� �ڷ�ƾ
    private IEnumerator FadeOutText()
    {
        // 1. ������ �ð�(3��) ���� ��ٸ�
        yield return new WaitForSeconds(displayDuration);

        // 2. ������ �ð�(1��) ���� ������ �����ϰ� ����
        float timer = 0f;
        Color startColor = levelText.color; // ���� ���� (���� 1)
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0); // ��ǥ ���� (���� 0)

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Lerp(����, ��, ���൵)�� ����Ͽ� �ε巴�� ���� ����
            levelText.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            yield return null; // ���� �����ӱ��� ���
        }

        // Ȯ���ϰ� ���İ� 0���� ����
        levelText.color = endColor;
        fadeCoroutine = null; // �ڷ�ƾ �Ϸ�
    }
}