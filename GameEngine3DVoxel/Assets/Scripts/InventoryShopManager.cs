using UnityEngine;
using UnityEngine.UI;
using TMPro; // 📢 TextMesh Pro 사용을 위한 네임스페이스 추가

public class InventoryShopManager : MonoBehaviour
{
    // 📢 UI 요소: 인벤토리와 상점을 포함하는 단일 패널
    [Header("Unified Panel")]
    public GameObject unifiedPanel;

    // 📢 UI 요소: 스탯 표시 (TextMeshProUGUI로 변경)
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
        // 시작 시 패널은 닫혀있어야 합니다.
        if (unifiedPanel != null)
        {
            unifiedPanel.SetActive(false);
        }
    }

    // ===============================================
    // 📢 E 키 처리: 통합 패널 열기/닫기
    // ===============================================
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
            // 패널 열기: 시간 정지 및 커서 활성화
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            UpdateStats(currentPlayer); // 스탯 정보를 UI에 업데이트
        }
        else
        {
            // 패널 닫기: 시간 재개 및 커서 잠금
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ===============================================
    // 📢 스탯 UI 업데이트 (경험치 획득/업그레이드 시 호출)
    // ===============================================

    public void UpdateStats(PlayerController player)
    {
        // 📢 .text 속성은 동일하게 사용합니다.

        // 경험치 표시
        if (expDisplay != null) expDisplay.text = $"EXP: {player.currentEXP}";

        // 레벨 표시
        if (levelDisplay != null) levelDisplay.text = $"Level: {player.currentLevel}";

        // 체력 스탯 표시
        if (hpStatDisplay != null)
        {
            hpStatDisplay.text =
                $"HP: {player.currentHP}/{player.maxHP} (+{PlayerController.HP_UPGRADE_AMOUNT} / {PlayerController.HP_UPGRADE_COST} EXP)";
        }

        // 공격력 스탯 표시
        if (attackStatDisplay != null)
        {
            attackStatDisplay.text =
               $"Attack: {player.attackPower} (+{PlayerController.ATTACK_UPGRADE_AMOUNT} / {PlayerController.ATTACK_UPGRADE_COST} EXP)";
        }
    }

    // 📢 HP 업그레이드 버튼의 OnClick()에 연결
    public void OnHPUpgradeButtonClicked()
    {
        if (currentPlayer != null && currentPlayer.TryUpgradeMaxHP())
        {
            // TryUpgradeMaxHP 내부에서 이미 UpdateStats를 호출합니다.
        }
    }

    // 📢 공격력 업그레이드 버튼의 OnClick()에 연결
    public void OnAttackUpgradeButtonClicked()
    {
        if (currentPlayer != null && currentPlayer.TryUpgradeAttackPower())
        {
            // TryUpgradeAttackPower 내부에서 이미 UpdateStats를 호출합니다.
        }
    }
}