using Godot;

namespace Durak.Godot;

public partial class Camera : Camera3D
{
    [Signal]
    public delegate void MovedEventHandler();

    [Export]
    private float _lookAroundSpeed = 0.004f;

    [Export]
    private float _movementSpeed = 0.25f;

    [Export]
    private Vector3 _presetPosition1 = new(-0.619f, 0.764f, 0f);

    [Export]
    private Vector3 _presetRotationDegrees1 = new(-22.4f, -90f, 0f);

    [Export]
    private Vector3 _presetPosition2 = new(-0.528f, 0.861f, 0f);

    [Export]
    private Vector3 _presetRotationDegrees2 = new(-44f, -90f, 0f);

    [Export]
    private Vector3 _presetPosition3 = new(-0.212f, 0.886f, 0f);

    [Export]
    private Vector3 _presetRotationDegrees3 = new(-54.5f, -90f, 0f);

    private float _rotationX = 0f;
    private float _rotationY = 0f;

    public override void _Input(InputEvent @event)
    {
        var isMoving = false;

        if (Input.IsActionPressed("middle_mouse_button") && @event is InputEventMouseMotion mouseMotion)
        {
            _rotationX += mouseMotion.Relative.X * _lookAroundSpeed;
            _rotationY += mouseMotion.Relative.Y * _lookAroundSpeed;

            ResetRotation();
            RotateObjectLocal(Vector3.Up, _rotationX);
            RotateObjectLocal(Vector3.Right, _rotationY);
            isMoving = true;
        }

        if (Input.IsActionPressed("f1"))
        {
            RotationDegrees = _presetRotationDegrees1;
            GlobalPosition = _presetPosition1;
            isMoving = true;
        }
        else if (Input.IsActionPressed("f2"))
        {
            RotationDegrees = _presetRotationDegrees2;
            GlobalPosition = _presetPosition2;
            isMoving = true;
        }
        else if (Input.IsActionPressed("f3"))
        {
            RotationDegrees = _presetRotationDegrees3;
            GlobalPosition = _presetPosition3;
            isMoving = true;
        }

        if (isMoving)
        {
            EmitSignal(SignalName.Moved);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var isMoving = false;
        var inFrontOfCamera = -Transform.Basis.Z;
        var movement = inFrontOfCamera * _movementSpeed * (float)delta;

        if (Input.IsActionPressed("ui_up"))
        {
            Position += movement;
            isMoving = true;
        }
        
        if (Input.IsActionPressed("ui_down"))
        {
            Position -= movement;
            isMoving = true;
        }

        if (isMoving)
        {
            EmitSignal(SignalName.Moved);
        }
    }

    private void ResetRotation()
    {
        var transform = Transform;
        transform.Basis = Basis.Identity;
        Transform = transform;
    }
}
