using UnityEngine;
using UnityEngine.UI;
using TMPro; // TMP 사용

public class InventoryShopManager : MonoBehaviour
{
    // UI 요소: 인벤토리와 상점을 포함하는 단일 패널
    [Header("통합 패널")] // Header 이름도 한국어로 변경 (선택 사항)
    public GameObject unifiedPanel;

    // UI 요소: 스탯 표시
    [Header("스탯 표시 UI")] // Header 이름도 한국어로 변경 (선택 사항)
    public TextMeshProUGUI expDisplay;
    public TextMeshProUGUI levelDisplay;
    public TextMeshProUGUI hpStatDisplay;
    public TextMeshProUGUI attackStatDisplay;
    public TextMeshProUGUI gunDamageDisplay;    // 총 공격력 텍스트
    public TextMeshProUGUI swordDamageDisplay;  // 칼 공격력 텍스트

    // 상태 변수
    public bool IsPanelOpen { get; private set; } = false;
    private PlayerController currentPlayer;
    private PlayerShooting currentPlayerShooting; // 무기 아이콘 업데이트용

    void Start()
    {
        if (unifiedPanel != null)
        {
            unifiedPanel.SetActive(false);
        }
    }

    public void ToggleInventoryShop(PlayerController player)
    {
        currentPlayer = player;
        IsPanelOpen = !IsPanelOpen;

        // PlayerShooting 참조 가져오기
        if (currentPlayerShooting == null && player != null)
        {
            currentPlayerShooting = player.GetComponent<PlayerShooting>();
        }

        if (unifiedPanel != null)
        {
            unifiedPanel.SetActive(IsPanelOpen);
        }

        if (IsPanelOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            UpdateStats(currentPlayer); // 스탯 업데이트 호출

            // 무기 아이콘 업데이트 함수 호출
            if (currentPlayerShooting != null)
            {
                currentPlayerShooting.UpdateInventoryWeaponIcons();
            }
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ===============================================
    // 📢 스탯 UI 업데이트 (한국어 텍스트로 변경!)
    // ===============================================

    public void UpdateStats(PlayerController player)
    {
        if (player == null) return; // 널 체크

        // 경험치 표시
        if (expDisplay != null) expDisplay.text = $"경험치: {player.currentEXP}";

        // 레벨 표시 (스킬 레벨)
        if (levelDisplay != null) levelDisplay.text = $"스킬 레벨: {player.currentLevel}";

        // 체력 스탯 표시
        if (hpStatDisplay != null)
        {
            // 예시: "체력: 82/100 (강화: +10 / 1 레벨 필요)"
            hpStatDisplay.text =
                $"체력: {player.currentHP}/{player.maxHP} (강화: +{PlayerController.HP_UPGRADE_AMOUNT} / {player.hpUpgradeLevelCost} 레벨 필요)";
        }

        // 공격력 스탯 표시 (업그레이드 비용)
        if (attackStatDisplay != null)
        {
            // 예시: "공격력 강화: (+1 / 1 레벨 필요)"
            attackStatDisplay.text =
                $"공격력 강화: (+{PlayerController.ATTACK_UPGRADE_AMOUNT} / {player.attackUpgradeLevelCost} 레벨 필요)";
        }

        // 무기 공격력 계산
        const int baseAttackDamage = 1; // 기본 공격력
        int upgradedDamage = player.attackDamage - baseAttackDamage;
        if (upgradedDamage < 0) upgradedDamage = 0;
        string damageText = $"{baseAttackDamage} + {upgradedDamage}"; // "1 + X" 형태

        // 총 공격력 텍스트 업데이트
        if (gunDamageDisplay != null)
        {
            gunDamageDisplay.text = $"총 공격력: {damageText}";
        }
        // 칼 공격력 텍스트 업데이트
        if (swordDamageDisplay != null)
        {
            swordDamageDisplay.text = $"칼 공격력: {damageText}";
        }
    }

    // HP 업그레이드 버튼의 OnClick()에 연결
    public void OnHPUpgradeButtonClicked()
    {
        if (currentPlayer != null && currentPlayer.TryUpgradeMaxHP())
        {
            UpdateStats(currentPlayer);
        }
    }

    // 공격력 업그레이드 버튼의 OnClick()에 연결
    public void OnAttackUpgradeButtonClicked()
    {
        if (currentPlayer != null && currentPlayer.TryUpgradeAttackPower())
        {
            UpdateStats(currentPlayer);
        }
    }
}