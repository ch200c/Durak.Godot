using Godot;

namespace Durak.Godot;

public partial class MiniMap : SubViewportContainer
{
    private float _defaultAnchorLeft;
    private float _defaultAnchorBottom;
    private float _defaultAnchorTop;
    private float _defaultAnchorRight;
    private Vector2 _defaultSize;
    private bool _isFullSize = false;

    public override void _Ready()
    {
        _defaultAnchorLeft = AnchorLeft;
        _defaultAnchorBottom = AnchorBottom;
        _defaultAnchorTop = AnchorTop;
        _defaultAnchorRight = AnchorRight;
        _defaultSize = Size;
    }

    private void _on_gui_input(InputEvent @event)
    {
        //if (@event.IsActionPressed("left_mouse_button"))
        //{
        //    if (_isFullSize)
        //    {
        //        SetSize(_defaultSize);
        //        AnchorLeft = _defaultAnchorLeft;
        //        AnchorBottom = _defaultAnchorBottom;
        //        AnchorRight = _defaultAnchorRight;
        //        AnchorTop = _defaultAnchorTop;
        //    }
        //    else
        //    {

        //        SetSize(GetViewport().GetVisibleRect().Size / 2);
        //        AnchorLeft = 0;
        //        AnchorTop = 0;
        //        AnchorBottom = 1;
        //        AnchorRight = 1;
        //    }

        //    _isFullSize = !_isFullSize;
        //}
    }
}