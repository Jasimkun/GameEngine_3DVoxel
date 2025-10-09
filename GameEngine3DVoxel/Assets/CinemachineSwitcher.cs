using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CinemachineSwitcher : MonoBehaviour
{

    public CinemachineVirtualCamera virtualCam;    //�⺻ tps ī�޶�

    public CinemachineFreeLook freeLookCam;     //���� ȸ�� tps ī�޶�

    public bool usingFreeLook = false;

    // Start is called before the first frame update
    void Start()
    {
        //������ Virtual Camera Ȱ��ȭ
        virtualCam.Priority = 10;
        freeLookCam.Priority = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))   //��Ŭ��
        {
            usingFreeLook = !usingFreeLook;
            if (usingFreeLook)
            {
                freeLookCam.Priority = 20;    //FreeLook Ȱ��ȭ
                virtualCam.Priority = 0;
            }
            else
            {
                virtualCam.Priority = 20;    //Virtual Camera Ȱ��ȭ
                freeLookCam.Priority = 0;
            }
        }
    }
}
