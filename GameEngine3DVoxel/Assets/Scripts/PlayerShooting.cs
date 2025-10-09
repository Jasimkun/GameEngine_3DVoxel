using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{

    public GameObject projectilePrefab;   //projectile ������

    public GameObject BoomPrefab;

    public Transform firePoint;           //�߻� ��ġ (�ѱ�)

    Camera cam;

    private GameObject currentWeaponPrefab; // ���� ���õ� ����

    private bool isBoomMode = false;        // ���� ��ȯ ����

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;        //���� ī�޶� ��������
        currentWeaponPrefab = projectilePrefab;
    }

    // Update is called once per frame
    void Update()
    {

        // Z Ű�� ���� ��ȯ
        if (Input.GetKeyDown(KeyCode.Z))
        {
            isBoomMode = !isBoomMode;
            currentWeaponPrefab = isBoomMode ? BoomPrefab : projectilePrefab;
            if(isBoomMode)
            { 
                Debug.Log("��ź��");
            }
            else
            {
                Debug.Log("����ź");
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        //ȭ�鿡�� ���콺 -> ����(Ray) ���
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint;
        targetPoint = ray.GetPoint(50f);
        Vector3 direction = (targetPoint - firePoint.position).normalized;  //���� ����

        //Projectile ����
        GameObject proj = Instantiate(currentWeaponPrefab, firePoint.position, Quaternion.LookRotation(direction));
    }
}
