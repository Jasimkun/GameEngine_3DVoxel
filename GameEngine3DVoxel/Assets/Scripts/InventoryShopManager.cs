using UnityEngine;
using UnityEngine.UI;
using TMPro; // TMP 사용

public class InventoryShopManager : MonoBehaviour
{
    // 📢 UI 요소: 인벤토리와 상점을 포함하는 단일 패널
    [Header("Unified Panel")]
    public GameObject unifiedPanel;

    // 📢 UI 요소: 스탯 표시
    [Header("Stat Display Elements")]
    public TextMeshProUGUI expDisplay;
    public TextMeshProUGUI levelDisplay;
    public TextMeshProUGUI hpStatDisplay;
    public TextMeshProUGUI attackStatDisplay;

    // 📢 상태 변수
    public bool IsPanelOpen { get; private set; } = false;
    private PlayerController currentPlayer;

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

        if (unifiedPanel != null)
        {
            unifiedPanel.SetActive(IsPanelOpen);
        }

        if (IsPanelOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            UpdateStats(currentPlayer);
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ===============================================
    // 📢 스탯 UI 업데이트 (다음 업그레이드 비용 반영)
    // ===============================================

    public void UpdateStats(PlayerController player)
    {
        // 경험치 표시
        if (expDisplay != null) expDisplay.text = $"EXP: {player.currentEXP}";

        // 레벨 표시 (업그레이드 포인트 역할)
        if (levelDisplay != null) levelDisplay.text = $"Skill Level: {player.currentLevel}";

        // 체력 스탯 표시 (📢 다음 업그레이드 레벨 비용 사용)
        if (hpStatDisplay != null)
        {
            // player.hpUpgradeLevelCost 변수 사용
            hpStatDisplay.text =
                $"HP: {player.currentHP}/{player.maxHP} (+{PlayerController.HP_UPGRADE_AMOUNT} / {player.hpUpgradeLevelCost} Level)";
        }

        // 공격력 스탯 표시 (📢 다음 업그레이드 레벨 비용 사용)
        if (attackStatDisplay != null)
        {
            // player.attackUpgradeLevelCost 변수 사용
            attackStatDisplay.text =
               $"Attack: {player.attackDamage} (+{PlayerController.ATTACK_UPGRADE_AMOUNT} / {player.attackUpgradeLevelCost} Level)";
        }
    }

    // 📢 HP 업그레이드 버튼의 OnClick()에 연결
    public void OnHPUpgradeButtonClicked()
    {
        if (currentPlayer != null && currentPlayer.TryUpgradeMaxHP())
        {
            // 로직은 PlayerController에서 처리됨
        }
    }

    // 📢 공격력 업그레이드 버튼의 OnClick()에 연결
    public void OnAttackUpgradeButtonClicked()
    {
        if (currentPlayer != null && currentPlayer.TryUpgradeAttackPower())
        {
            // 로직은 PlayerController에서 처리됨
        }
    }
}