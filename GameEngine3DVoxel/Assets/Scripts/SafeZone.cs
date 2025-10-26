using UnityEngine;

// 1. 이 스크립트는 빈 오브젝트나 바닥(Plane) 등 안전 지점 역할을 할 오브젝트에 붙여주세요.
// 2. 이 오브젝트에는 반드시 Collider 컴포넌트가 있어야 하고, 'Is Trigger'가 체크되어 있어야 합니다.
public class SafeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {

        Debug.Log("!!! 트리거 감지 성공! 충돌한 오브젝트: " + other.name);

        // 1. 충돌한 오브젝트가 "Player" 태그를 가졌는지 확인합니다.
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null)
            {
                // 플레이어 태그는 있지만 스크립트가 없는 경우
                return;
            }

            // 2. HP 비율을 계산합니다 (float으로 형 변환 필수!)
            float currentHP_Percentage = (float)player.currentHP / player.maxHP;

            // 3. 스폰 포인트 갱신 (공통 기능)
            // 이 SafeZone 오브젝트의 위치를 새로운 스폰 지점으로 설정합니다.
            // (만약 스폰 지점을 다른 곳으로 하고 싶다면 this.transform.position 대신 다른 좌표를 넣어주세요)
            player.UpdateSpawnPoint(this.transform.position);

            // 4. HP 60% 이하 조건부 회복
            if (currentHP_Percentage <= 0.6f)
            {
                // 60% 이하일 경우
                Debug.Log("HP가 60% 이하입니다. 50%까지 회복합니다.");

                // 50% (절반) HP 계산
                int targetHP = player.maxHP / 2;

                // PlayerController의 회복 함수 호출 (이미 50%를 넘으면 회복하지 않음)
                player.HealToAmount(targetHP);
            }
            else
            {
                // 60% 초과일 경우
                Debug.Log("HP가 60%를 초과합니다. 스폰 포인트만 갱신합니다.");
            }
        }
    }
}