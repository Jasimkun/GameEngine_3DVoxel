using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 💡 씬 전체의 적 개수를 추적하는 싱글톤
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance; // 싱글톤 인스턴스

    private int activeEnemyCount = 0;

    // 💡 공격 가능한 구름 핵 (에디터에서 할당)
    public CloudCore cloudCore;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 💡 씬 시작 시 또는 스폰 시 호출 (선택 사항)
    public void RegisterEnemy()
    {
        activeEnemyCount++;
        Debug.Log("새 적 등록됨. 현재 적 수: " + activeEnemyCount);
    }

    // 💡 적이 파괴될 때 Enemy 스크립트에서 호출됨
    public void UnregisterEnemy()
    {
        activeEnemyCount--;
        Debug.Log("적 파괴됨. 현재 적 수: " + activeEnemyCount);

        // 💡 핵심 로직: 적이 0이 되면 CloudCore에 알림
        if (activeEnemyCount <= 0)
        {
            Debug.Log("모든 적이 파괴되었습니다! 구름 핵 활성화.");
            if (cloudCore != null)
            {
                cloudCore.ActivateAttackability();
            }
        }
    }
}