using Godot;

namespace Durak.Godot;

public partial class Camera : Camera3D
{
    [Export]
    private float _lookAroundSpeed = 0.004f;

    [Export]
    private float _movementSpeed = 0.25f;

    [Export]
    private Vector3 _presetPosition1 = new(-0.619f, 0.764f, 0f);

    [Export]
    private Vector3 _presetRotationDegrees1 = new(-22.4f, -90f, 0f);

    [Export]
    private Vector3 _presetPosition2 = new(-0.477f, 0.907f, 0f);

    [Export]
    private Vector3 _presetRotationDegrees2 = new(-50.2f, -90f, 0f);

    private float _rotationX = 0f;
    private float _rotationY = 0f;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionPressed("middle_mouse_button") && @event is InputEventMouseMotion mouseMotion)
        {
            _rotationX += mouseMotion.Relative.X * _lookAroundSpeed;
            _rotationY += mouseMotion.Relative.Y * _lookAroundSpeed;

            // reset rotation
            var transform = Transform;
            transform.Basis = Basis.Identity;
            Transform = transform;

            RotateObjectLocal(Vector3.Up, _rotationX);
            RotateObjectLocal(Vector3.Right, _rotationY);
        }

        if (Input.IsActionPressed("f1"))
        {
            GlobalRotationDegrees = _presetRotationDegrees1;
            GlobalPosition = _presetPosition1;
        }

        if (Input.IsActionPressed("f2"))
        {
            GlobalRotationDegrees = _presetRotationDegrees2;
            GlobalPosition = _presetPosition2;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var inFrontOfCamera = -GlobalTransform.Basis.Z;

        if (Input.IsActionPressed("ui_up"))
        {
            GlobalPosition += inFrontOfCamera * _movementSpeed * (float)delta;
        }

        if (Input.IsActionPressed("ui_down"))
        {
            GlobalPosition -= inFrontOfCamera * _movementSpeed * (float)delta;
        }
    }
}
