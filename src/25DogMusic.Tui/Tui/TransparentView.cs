using Terminal.Gui;

namespace FnMusic.Tui;

/// <summary>
/// 透明容器：自身不铺底色，只负责给子视图提供统一的内边距，
/// 让窗口绘制的封面壁纸能透出来。
/// </summary>
public sealed class TransparentView : View
{
    public TransparentView()
    {
        // 容器自身不参与焦点，但必须允许焦点穿透到子视图
        CanFocus = true;
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        // 什么都不画，保留底下的壁纸
        return true;
    }
}
