using UnityEngine;
using TMPro; // TextMeshPro 사용
using System.Collections;

public class LevelDisplay : MonoBehaviour
{
    public TMP_Text levelText; // Inspector에서 "LevelDisplayText" 연결
    // 🔻 1. [추가] 붕괴 시간 텍스트 변수
    public TMP_Text collapseDelayText; // Inspector에서 "CollapseDelayText" 연결

    public float displayDuration = 3f; // 보이는 시간
    public float fadeDuration = 1f;    // 사라지는 시간
    private Coroutine fadeCoroutine;

    void Awake()
    {
        // 시작 시 두 텍스트 모두 투명하게
        if (levelText != null)
        {
            levelText.color = new Color(levelText.color.r, levelText.color.g, levelText.color.b, 0);
        }
        else { Debug.LogError("LevelDisplay: Level Text가 연결되지 않았습니다!"); }

        // 🔻 2. [추가] 붕괴 시간 텍스트 초기화
        if (collapseDelayText != null)
        {
            collapseDelayText.color = new Color(collapseDelayText.color.r, collapseDelayText.color.g, collapseDelayText.color.b, 0);
        }
        else { Debug.LogError("LevelDisplay: Collapse Delay Text가 연결되지 않았습니다!"); }
    }

    // GameManager가 호출할 함수
    public void ShowLevel(int levelNumber)
    {
        // 두 텍스트 중 하나라도 없으면 실행 중지
        if (levelText == null || collapseDelayText == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // GameManager에서 현재 붕괴 지연 시간 가져오기
        float collapseDelay = (GameManager.Instance != null) ? GameManager.Instance.currentCollapseDelay : 5.0f;

        // 🔻 3. [수정] 각 텍스트 내용 개별 설정
        levelText.text = $"Level {levelNumber}";
        collapseDelayText.text = $"(타일 붕괴: {collapseDelay:F1}초)"; // 소수점 한 자리까지

        // 🔻 3. [수정] 각 텍스트 알파값 1로 설정 (보이게)
        levelText.color = new Color(levelText.color.r, levelText.color.g, levelText.color.b, 1);
        collapseDelayText.color = new Color(collapseDelayText.color.r, collapseDelayText.color.g, collapseDelayText.color.b, 1);

        fadeCoroutine = StartCoroutine(FadeOutText());
    }

    // 서서히 사라지게 하는 코루틴
    private IEnumerator FadeOutText()
    {
        // 1. 설정된 시간(3초) 동안 기다림
        yield return new WaitForSeconds(displayDuration);

        // 2. 설정된 시간(1초) 동안 두 텍스트 모두 서서히 투명하게
        float timer = 0f;
        // 시작 색상 저장 (알파=1)
        Color startColorLevel = levelText.color;
        Color startColorDelay = collapseDelayText.color;
        // 목표 색상 저장 (알파=0)
        Color endColorLevel = new Color(startColorLevel.r, startColorLevel.g, startColorLevel.b, 0);
        Color endColorDelay = new Color(startColorDelay.r, startColorDelay.g, startColorDelay.b, 0);

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration; // 진행도 (0 ~ 1)

            // 🔻 4. [수정] 두 텍스트 색상 동시 변경
            levelText.color = Color.Lerp(startColorLevel, endColorLevel, progress);
            collapseDelayText.color = Color.Lerp(startColorDelay, endColorDelay, progress);

            yield return null; // 다음 프레임까지 대기
        }

        // 확실하게 알파값 0으로 설정
        levelText.color = endColorLevel;
        collapseDelayText.color = endColorDelay;
        fadeCoroutine = null; // 코루틴 완료
    }
}