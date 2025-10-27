using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 씬 전체의 적 개수를 추적하는 싱글톤
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance; // 싱글톤 인스턴스

    private int activeEnemyCount = 0;

    // PlayerController 인스턴스를 저장 (경험치 부여용)
    public PlayerController playerController;

    // 공격 가능한 구름 핵 (에디터에서 할당)
    public CloudCore cloudCore;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 씬 시작 시 PlayerController 찾기
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("EnemyManager가 PlayerController를 찾을 수 없습니다! 씬에 PlayerController가 있는지 확인하세요.", this.gameObject);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 적 등록 (Start에서 호출)
    public void RegisterEnemy()
    {
        activeEnemyCount++;
        // Debug.Log(activeEnemyCount + "만큼의 적을 처치해야 해!");
    }

    // 적 사망 시 호출 (Die 함수에서 호출)
    public void UnregisterEnemy()
    {
        // 이미 0 이하면 중복 호출 방지
        if (activeEnemyCount <= 0) return;

        activeEnemyCount--;
        // Debug.Log("적 제거됨. 남은 적 수는 " + activeEnemyCount + "!");

        // 📢 여기 있던 경험치 지급 로직 삭제!
        /*
        if (playerController != null)
        {
            playerController.AddExperience(EXP_PER_ENEMY); // <<< 삭제됨!
        }
        else { ... }
        */

        // 모든 적 처치 시 CloudCore 활성화 로직은 유지
        if (activeEnemyCount <= 0)
        {
            Debug.Log("모든 적을 처치했어! 이제 구름 핵을 파괴하면 돼!");
            if (cloudCore != null)
            {
                cloudCore.ActivateAttackability();
            }
        }
    }

    // 📢 플레이어가 적을 처치했을 때만 호출될 함수 (새로 추가!)
    //    (각 적 스크립트의 TakeDamage에서 호출해야 함)
    public void EnemyDefeated(int experienceValue)
    {
        if (playerController != null)
        {
            playerController.AddExperience(experienceValue);
            // Debug.Log($"플레이어가 경험치 {experienceValue}를 획득했습니다!");
        }
        else
        {
            Debug.LogError("경험치를 주려 했으나 PlayerController 참조가 없습니다!");
        }
    }
}