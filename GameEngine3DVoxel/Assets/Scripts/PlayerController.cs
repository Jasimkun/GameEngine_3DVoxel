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

    // === DOT (Damage Over Time) 설정 변수 ===
    private Coroutine fireDotCoroutine; // 💡 지속 피해 코루틴 참조

    // === 리스폰 설정 변수 ===
    [Header("Respawn Settings")]
    private Vector3 startPosition; // 시작 위치만 저장

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        pov = virtualCam.GetCinemachineComponent<CinemachinePOV>();

        currentHP = maxHP;
        hpSlider.value = 1f;

        // 시작 위치를 저장합니다.
        startPosition = transform.position;

        // CharacterController를 사용할 경우, Rigidbody를 추가하고 Kinematic을 체크해야 
        // OnTriggerEnter가 안정적으로 작동합니다. (에디터에서 수동으로 추가 권장)
    }

    // Update is called once per framed
    void Update()
    {
        // === 입력 및 속도 제어 ===
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

        // === 땅 확인 및 점프 로직 ===
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            if (velocity.y < 0)
            {
                velocity.y = -2f;  // 지면에 붙이기
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = jumpPower;
            }
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // === 이동 및 회전 로직 ===
        Vector3 camForward = virtualCam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = virtualCam.transform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 move = (camForward * z + camRight * x).normalized;
        controller.Move(move * speed * Time.deltaTime);

        float cameraYaw = pov.m_HorizontalAxis.Value;
        Quaternion targetRot = Quaternion.Euler(0f, cameraYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);


        // === 중력 적용 ===
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // 💡 새로운 함수: Trigger 충돌 감지 (DeadZone 감지에 사용)
    private void OnTriggerEnter(Collider other)
    {
        // DeadZone 태그를 가진 오브젝트와 충돌했는지 확인합니다.
        if (other.CompareTag("DeadZone"))
        {
            Debug.Log("DeadZone에 진입! 즉시 리스폰합니다.");
            Respawn();
        }

        // 💡 Projectile과의 충돌 감지 (Fire 적의 투사체 포함)
        // 💡 투사체 스크립트에 DOT 정보를 포함하는 함수가 있다고 가정합니다.
        // if (other.GetComponent<FireProjectile>() != null)
        // {
        //     // FireProjectile 스크립트에서 DOT 정보를 가져와서 적용한다고 가정합니다.
        //     // FireProjectile fireProj = other.GetComponent<FireProjectile>();
        //     // ApplyFireDOT(fireProj.initialDamage, fireProj.dotDamage, fireProj.dotDuration);
        //     // Destroy(other.gameObject);
        // }
    }


    // === 리스폰 함수 ===
    void Respawn()
    {
        // 💡 리스폰 시 진행 중이던 DOT 코루틴 중지
        if (fireDotCoroutine != null)
        {
            StopCoroutine(fireDotCoroutine);
            fireDotCoroutine = null;
        }

        // 1. 캐릭터 컨트롤러 비활성화 및 위치 재설정
        controller.enabled = false;
        transform.position = startPosition;
        controller.enabled = true;

        // 2. 속도 초기화
        velocity = Vector3.zero;

        // 3. 체력 복구 (선택 사항)
        currentHP = maxHP;
        hpSlider.value = 1f;
    }

    // === 피해 및 사망 로직 ===
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
        // 사망 시 리스폰 호출
        Respawn();
    }

    public void StartDamageOverTime(int damage, float duration, float interval)
    {
        StartCoroutine(DamageOverTimeCoroutine(damage, duration, interval));
    }

    private IEnumerator DamageOverTimeCoroutine(int damage, float duration, float interval)
    {
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            // TakeDamage는 이미 구현되어 있다고 가정
            TakeDamage(damage);
            yield return new WaitForSeconds(interval);
        }
    }
}