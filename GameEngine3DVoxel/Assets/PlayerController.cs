using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private float speed;

    private float walkSpeed = 5f;

    private float runSpeed = 12f;

    private float stopSpeed = 0f;

    private float jumpPower = 7f;

    private float stopJumpPower = 0f;

    public CinemachineSwitcher cinemachineSwitcher;

    public float gravity = -9.81f;

    public CinemachineVirtualCamera virtualCam;

    public float rotationSpeed = 10f;

    private CinemachinePOV pov;

    private CharacterController controller;

    private Vector3 velocity;

    public bool isGrounded;


    public int maxHP = 100;

    private int currentHP;

    public Slider hpSlider;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        pov = virtualCam.GetCinemachineComponent<CinemachinePOV>();

        currentHP = maxHP;
        hpSlider.value = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            pov.m_HorizontalAxis.Value = transform.eulerAngles.y;
            pov.m_VerticalAxis.Value = 0f;
        }

        if (cinemachineSwitcher.usingFreeLook == true)
        {
            speed = stopSpeed;
            jumpPower = stopJumpPower;
        }
        else
        {
            jumpPower = 7f;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                speed = runSpeed;
                virtualCam.m_Lens.FieldOfView = 80f;
            }
            else
            {
                speed = walkSpeed;
                virtualCam.m_Lens.FieldOfView = 60f;
            }
        }

        //����
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = jumpPower;
        }


        //���� ��� �ִ��� Ȯ��
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;  //���鿡 ���̱�
        }
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        //ī�޶� ���� ���� ���
        Vector3 camForward = virtualCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = virtualCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 move = (camForward * z + camRight * x).normalized;  //�̵� ���� = ī�޶� forward/right ���
        controller.Move(move * speed * Time.deltaTime);

        float cameraYaw = pov.m_HorizontalAxis.Value;   //���콺 �¿� ȸ����
        Quaternion targetRot = Quaternion.Euler(0f, cameraYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);


        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        hpSlider.value = (float)currentHP / maxHP;

        if (currentHP <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
