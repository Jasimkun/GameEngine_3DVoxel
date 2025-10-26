using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI; // UI 사용을 위해 필수

public class PlayerController : MonoBehaviour
{
    private float speed;
    private float walkSpeed = 5f;
    private float runSpeed = 12f;
    private float stopSpeed = 0f;
    private float jumpPower = 7f;
    private float stopJumpPower = 0f;

    // 🔥 경험치 및 레벨 관련 변수
    private int currentLevel = 1;
    private int currentEXP = 0;
    private int requiredEXP = 25; // Level 1 -> 2에 필요한 초기 경험치

    // 🔥 레벨업에 필요한 기본 경험치 및 증가량 상수
    private const int BASE_EXP_TO_NEXT_LEVEL = 25; // 레벨 2에 필요한 경험치
    private const int EXP_INCREASE_PER_LEVEL = 10; // 레벨이 오를 때마다 증가하는 요구 경험치량


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

    // 📢 UI 연결 변수
    [Header("UI")]
    public Slider expSlider; // 경험치 바 슬라이더
    public Image expFillImage; // 📢 경험치 바의 Fill Image 컴포넌트

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

        // 🔥 레벨업 시스템 초기화
        CalculateRequiredEXP();
        Debug.Log($"플레이어 초기화. 현재 레벨: {currentLevel}, 다음 레벨업까지 필요한 경험치: {requiredEXP}");

        // 📢 UI 초기화: 경험치 바 상태 업데이트 (0일 때 Fill Image 숨김)
        UpdateEXPSlider();

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

    // === DOT 로직 ===
    public void StartDamageOverTime(int damage, float duration, float interval)
    {
        // 🔥 이미 DOT 코루틴이 실행 중이면 중지하고 새로 시작 (새 공격이 갱신)
        if (fireDotCoroutine != null)
        {
            StopCoroutine(fireDotCoroutine);
        }
        fireDotCoroutine = StartCoroutine(DamageOverTimeCoroutine(damage, duration, interval));
    }

    private IEnumerator DamageOverTimeCoroutine(int damage, float duration, float interval)
    {
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            // TakeDamage 호출
            TakeDamage(damage);
            yield return new WaitForSeconds(interval);
        }
        fireDotCoroutine = null; // 코루틴이 완료되면 참조 해제
    }

    // 🔥 경험치 획득 메서드 (EnemyManager에서 호출됨)
    public void AddExperience(int amount)
    {
        currentEXP += amount;
        Debug.Log($"경험치 +{amount} 획득. 현재 레벨: {currentLevel}, 현재 경험치: {currentEXP} / 다음 레벨까지: {requiredEXP}");

        // 📢 경험치 바 업데이트
        UpdateEXPSlider();

        // 레벨업이 가능한지 확인합니다.
        CheckForLevelUp();
    }

    // 🔥 레벨업 확인 및 처리
    private void CheckForLevelUp()
    {
        while (currentEXP >= requiredEXP)
        {
            // 1. 레벨업
            currentLevel++;

            // 2. 남은 경험치 계산 (초과 경험치)
            currentEXP -= requiredEXP;

            // 3. 다음 레벨업에 필요한 경험치 재계산
            CalculateRequiredEXP();

            Debug.Log("🎉 레벨 업! 🎉");
            Debug.Log($"현재 레벨: {currentLevel}. 다음 레벨업까지 {requiredEXP} 경험치 필요.");

            // 📢 레벨업 시 경험치 바를 갱신합니다 (새로운 requiredEXP 기준으로).
            UpdateEXPSlider();

            // 4. 레벨업 보상 로직을 여기에 추가합니다.
        }
    }

    // 🔥 다음 레벨업에 필요한 경험치를 계산하는 메서드
    private void CalculateRequiredEXP()
    {
        // Level N -> N+1에 필요한 경험치: 25 + (N-1) * 10
        requiredEXP = BASE_EXP_TO_NEXT_LEVEL + (currentLevel - 1) * EXP_INCREASE_PER_LEVEL;
    }

    // 📢 추가: 경험치 슬라이더를 업데이트하고 Fill Image를 제어하는 핵심 메서드
    private void UpdateEXPSlider()
    {
        if (expSlider == null) return;

        // 1. 슬라이더 값 업데이트
        // 현재 경험치 / 다음 레벨까지 필요한 총 경험치
        float expPercentage = (float)currentEXP / requiredEXP;
        expSlider.value = expPercentage;

        // 2. Fill Image 활성화/비활성화 제어
        if (expFillImage != null)
        {
            // 현재 경험치가 0보다 크면 Fill Image 활성화 (보이게)
            // 현재 경험치가 0이면 Fill Image 비활성화 (안 보이게)
            expFillImage.enabled = currentEXP > 0;
        }
    }
}