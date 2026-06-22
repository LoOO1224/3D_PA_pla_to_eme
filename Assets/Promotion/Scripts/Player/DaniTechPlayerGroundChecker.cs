using System;
using UnityEngine;

public class DaniTechPlayerGroundChecker : MonoBehaviour
{
    [SerializeField] private Transform Transform_GroundCheck;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _groundCheckRadius = 0.22f;

    private bool _isGrounded;

    public event Action<bool> OnGroundStateChanged;
    public bool IsGrounded => _isGrounded;

    // 지면 판정 배우는 발밑 마커가 없으면 자기 Transform을 임시 기준으로 씁니다.
    // 하지만 실제 프로젝트에서는 발밑 자식 오브젝트를 연결해 더 정확히 판정합니다.
    private void Awake()
    {
        if (Transform_GroundCheck == null)
        {
            Transform_GroundCheck = transform;
        }
    }

    // FixedUpdate는 물리 촬영 타이밍입니다.
    // Rigidbody 점프와 같은 박자로 지면 여부를 갱신해야 착지 판정이 안정적입니다.
    private void FixedUpdate()
    {
        CheckGroundState();
    }

    // 작은 구를 발밑에 놓고 무대 바닥과 닿았는지 확인합니다.
    // 이 컴포넌트는 "땅인가 아닌가"만 판단하고, 점프 실행은 Jump 배우에게 맡깁니다.
    private void CheckGroundState()
    {
        bool nextGrounded = Physics.CheckSphere(
            Transform_GroundCheck.position,
            _groundCheckRadius,
            _groundMask,
            QueryTriggerInteraction.Ignore);

        if (_isGrounded == nextGrounded)
        {
            return;
        }

        _isGrounded = nextGrounded;
        OnGroundStateChanged?.Invoke(_isGrounded);

        if (DaniTechGameManager.Inst != null)
        {
            DaniTechGameManager.Inst.SetGroundedState(_isGrounded);
        }
    }

    // Scene 뷰에서 발밑 판정 범위를 바로 볼 수 있게 해 초보자가 디버깅하기 쉽게 둡니다.
    private void OnDrawGizmosSelected()
    {
        Transform targetTransform = Transform_GroundCheck != null ? Transform_GroundCheck : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetTransform.position, _groundCheckRadius);
    }
}
