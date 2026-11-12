// A tab of the structure window. Its button and page in StructureWindow.uxml are
// named "<Name>Tab" and "<Name>Page"; the button's text is its caption. The window
// shows the tab only for a structure it is for and asks it to draw its page every
// time the page is shown.
public interface IStructureTab
{
    string Name { get; }

    bool IsFor(Structure structure);

    void Show(Structure structure);

    // Its page was switched away from or the window closed.
    void Hide();
}
