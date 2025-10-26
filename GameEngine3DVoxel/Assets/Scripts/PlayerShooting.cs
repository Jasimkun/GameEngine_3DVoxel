using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    // 📢 1. PlayerController 참조 변수 추가
    public PlayerController playerController;

    public GameObject projectilePrefab;
    public GameObject BoomPrefab;
    public Transform firePoint;

    Camera cam;

    private GameObject currentWeaponPrefab;
    private bool isBoomMode = false;

    void Start()
    {
        cam = Camera.main;
        currentWeaponPrefab = projectilePrefab;

        // 📢 2. PlayerController 참조를 자동으로 찾습니다 (PlayerController가 같은 GameObject에 있다고 가정)
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        // 📢 PlayerController가 없으면 오류 메시지를 출력합니다.
        if (playerController == null)
        {
            Debug.LogError("PlayerController 스크립트를 찾을 수 없습니다. 데미지 정보를 가져올 수 없습니다.");
        }
    }

    void Update()
    {
        // Z 키로 무기 전환
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isBoomMode = !isBoomMode;
            currentWeaponPrefab = isBoomMode ? BoomPrefab : projectilePrefab;
            if (isBoomMode)
            {
                Debug.Log("폭탄띠");
            }
            else
            {
                Debug.Log("안폭탄");
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Vector3 direction = cam.transform.forward;

        GameObject proj = Instantiate(currentWeaponPrefab, firePoint.position, Quaternion.LookRotation(direction));

        // 📢 3. 투사체에 데미지를 전달하는 핵심 로직 추가
        if (playerController != null)
        {
            // 투사체가 'Projectile'인지 확인하고 데미지를 전달합니다.
            // (폭탄도 Projectile을 사용하거나, Projectile과 유사한 SetDamage 함수가 있다고 가정)
            Projectile projectileComponent = proj.GetComponent<Projectile>();

            if (projectileComponent != null)
            {
                // 🔥 PlayerController의 attackDamage 값을 투사체에 설정
                projectileComponent.SetDamage(playerController.attackDamage);
            }
            else
            {
                // BoomPrefab은 Projectile 컴포넌트가 없을 수 있으므로 이 메시지는 정상일 수 있습니다.
                // BoomPrefab이 데미지를 받는 방법이 다르다면 해당 로직을 여기에 추가해야 합니다.
                Debug.LogWarning(proj.name + "에 Projectile 컴포넌트가 없습니다. 데미지가 설정되지 않았을 수 있습니다.");
            }
        }
    }
}