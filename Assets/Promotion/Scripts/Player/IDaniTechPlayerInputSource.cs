using UnityEngine;

public interface IDaniTechPlayerInputSource
{
    Vector2 MoveInput { get; }
    Vector2 LookInput { get; }
    bool IsRunPressed { get; }
    bool JumpPressed { get; }
    float ZoomInput { get; }
}
