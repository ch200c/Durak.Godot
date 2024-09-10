using Godot;

namespace Durak.Godot;

public partial class MiniMap : SubViewportContainer
{
    private float _defaultAnchorLeft;
    private float _defaultAnchorBottom;
    private float _defaultAnchorTop;
    private float _defaultAnchorRight;
    private Vector2 _defaultSize;
    private Vector2 _defaultPosition;
    private bool _isFullSize = false;

    public override void _Ready()
    {
        _defaultAnchorLeft = AnchorLeft;
        _defaultAnchorBottom = AnchorBottom;
        _defaultAnchorTop = AnchorTop;
        _defaultAnchorRight = AnchorRight;

        _defaultSize = Size;
        _defaultPosition = Position;

        GetViewport().SizeChanged += Viewport_SizeChanged;
    }

    private void Viewport_SizeChanged()
    {
        if (_isFullSize)
        {
            SetFullSize();
        }
        else
        {
            ResetOffsets();
        }
    }

    private void _on_gui_input(InputEvent @event)
    {
        if (@event.IsActionPressed("left_mouse_button"))
        {
            if (_isFullSize)
            {
                AnchorLeft = _defaultAnchorLeft;
                AnchorBottom = _defaultAnchorBottom;
                AnchorRight = _defaultAnchorRight;
                AnchorTop = _defaultAnchorTop;

                SetSize(_defaultSize);
                SetPosition(_defaultPosition);
                ResetOffsets();
            }
            else
            {
                SetFullSize();
            }

            _isFullSize = !_isFullSize;
        }
    }

    private void SetFullSize()
    {
        var fullWindow = GetViewport().GetVisibleRect().Size;
        SetSize(new Vector2(fullWindow.X, fullWindow.Y * 0.6f));
        SetPosition(new Vector2(0, 0));
    }

    private void ResetOffsets()
    {
        OffsetBottom = 0;
        OffsetTop = 0;
        OffsetRight = 0;
        OffsetLeft = 0;
    }
}