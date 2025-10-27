using UnityEngine;
using TMPro; // TextMeshPro 사용
using System.Collections;

public class LevelDisplay : MonoBehaviour
{
    public TMP_Text levelText; // Inspector에서 연결할 TextMeshPro 오브젝트
    public float displayDuration = 2f; // 텍스트가 보이는 시간 (3초)
    public float fadeDuration = 1f; // 텍스트가 사라지는 시간 (1초)

    private Coroutine fadeCoroutine; // 실행 중인 코루틴 저장용

    void Awake()
    {
        // 시작할 때 텍스트가 안 보이도록 알파값 0으로 설정
        if (levelText != null)
        {
            levelText.color = new Color(levelText.color.r, levelText.color.g, levelText.color.b, 0);
        }
        else
        {
            Debug.LogError("LevelDisplay: Level Text가 연결되지 않았습니다!");
        }
    }

    // GameManager가 호출할 함수
    public void ShowLevel(int levelNumber)
    {
        if (levelText == null) return;

        // 이전에 실행 중인 페이드 코루틴이 있다면 중지
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 텍스트 설정 및 표시
        levelText.text = "레벨 " + levelNumber;
        levelText.color = new Color(levelText.color.r, levelText.color.g, levelText.color.b, 1); // 알파값 1 (완전 불투명)

        // 페이드 아웃 코루틴 시작
        fadeCoroutine = StartCoroutine(FadeOutText());
    }

    // 서서히 사라지게 하는 코루틴
    private IEnumerator FadeOutText()
    {
        // 1. 설정된 시간(3초) 동안 기다림
        yield return new WaitForSeconds(displayDuration);

        // 2. 설정된 시간(1초) 동안 서서히 투명하게 만듦
        float timer = 0f;
        Color startColor = levelText.color; // 현재 색상 (알파 1)
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0); // 목표 색상 (알파 0)

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // Lerp(시작, 끝, 진행도)를 사용하여 부드럽게 색상 변경
            levelText.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
            yield return null; // 다음 프레임까지 대기
        }

        // 확실하게 알파값 0으로 설정
        levelText.color = endColor;
        fadeCoroutine = null; // 코루틴 완료
    }
}