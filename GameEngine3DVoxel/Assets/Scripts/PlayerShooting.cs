using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
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
        // 💡 1. Raycasting 로직을 제거하고 카메라의 정면 방향을 사용합니다.
        //    (카메라가 곧 플레이어의 시선이라고 가정합니다.)
        Vector3 direction = cam.transform.forward;

        // 💡 2. 투사체가 이 방향을 향하도록 회전시켜 생성합니다.
        GameObject proj = Instantiate(currentWeaponPrefab, firePoint.position, Quaternion.LookRotation(direction));

        // 💡 참고: 만약 투사체에 Rigidbody나 스크립트가 있다면, 
        //    해당 스크립트에 direction을 전달하여 움직임을 시작해야 합니다.
    }
}