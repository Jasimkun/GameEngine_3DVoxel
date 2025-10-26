using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 💡 씬 전체의 적 개수를 추적하는 싱글톤
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance; // 싱글톤 인스턴스

    private int activeEnemyCount = 0;

    // 📢 추가: 한 명의 적 처치 시 얻는 경험치 값
    private const int EXP_PER_ENEMY = 5;

    // 📢 추가: PlayerController 인스턴스를 저장하여 경험치 부여에 사용
    // (씬 시작 시 찾거나, 플레이어가 생성될 때 등록하는 방식이 일반적입니다.)
    public PlayerController playerController;

    // 💡 공격 가능한 구름 핵 (에디터에서 할당)
    public CloudCore cloudCore;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 📢 추가: 씬 시작 시 PlayerController를 찾아 할당 (가장 단순한 방법)
            // 💡 주의: PlayerController가 이미 씬에 존재해야 합니다.
            playerController = FindObjectOfType<PlayerController>();
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

        // 📢 핵심 경험치 로직 추가
        if (playerController != null)
        {
            // PlayerController에 경험치를 추가하는 메서드를 호출합니다.
            // ⚠️ 이 로직이 작동하려면 PlayerController에 'AddExperience(int amount)' 메서드가 있어야 합니다.
            playerController.AddExperience(EXP_PER_ENEMY);
            Debug.Log($"플레이어에게 경험치 {EXP_PER_ENEMY} 부여됨.");
        }
        else
        {
            Debug.LogError("PlayerController를 찾을 수 없어 경험치를 부여할 수 없습니다.");
        }

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