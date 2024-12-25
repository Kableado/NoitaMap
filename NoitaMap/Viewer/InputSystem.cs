using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace NoitaMap.Viewer;

public static class InputSystem
{
    private static MouseState _lastMouseState;

    private static MouseState _currentMouseState;

    private static IInputContext? _inputContext;

    private static IMouse? _mouse;

    private static IKeyboard? _keyboard;

    public static void Update(IWindow window)
    {
        _inputContext ??= window.CreateInput();

        bool setMouse = _mouse is null;
        _mouse ??= _inputContext.Mice[0];

        bool setKeys = _keyboard is null;
        _keyboard ??= _inputContext.Keyboards[0];

        if (setMouse)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            _mouse.MouseDown += (_, button) =>
            {
                io.AddMouseButtonEvent(((int)button), true);
            };

            _mouse.MouseUp += (_, button) =>
            {
                io.AddMouseButtonEvent(((int)button), false);
            };

            _mouse.Scroll += (_, wheel) =>
            {
                io.AddMouseWheelEvent(wheel.X, wheel.Y);
            };
        }

        if (setKeys)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            _keyboard.KeyChar += (_, character) =>
            {
                io.AddInputCharacter(character);
            };

            _keyboard.KeyDown += (_, key, _) =>
            {
                io.AddKeyEvent(KeyTranslator.GetKey(key), true);
            };

            _keyboard.KeyUp += (_, key, _) =>
            {
                io.AddKeyEvent(KeyTranslator.GetKey(key), false);
            };
        }

        _lastMouseState = _currentMouseState;

        _currentMouseState.Position = _mouse.Position;

        _currentMouseState.LeftDown = _mouse.IsButtonPressed(MouseButton.Left);
        _currentMouseState.RightDown = _mouse.IsButtonPressed(MouseButton.Right);
        _currentMouseState.MiddleDown = _mouse.IsButtonPressed(MouseButton.Middle);

        _currentMouseState.Scroll += _mouse.ScrollWheels[0].Y;
    }

    public static bool LeftMouseDown => !ImGui.GetIO().WantCaptureMouse && _currentMouseState.LeftDown;

    public static bool RightMouseDown => !ImGui.GetIO().WantCaptureMouse && _currentMouseState.RightDown;

    public static bool MiddleMouseDown => !ImGui.GetIO().WantCaptureMouse && _currentMouseState.MiddleDown;

    public static bool LeftMousePressed => !ImGui.GetIO().WantCaptureMouse && (_currentMouseState.LeftDown && !_lastMouseState.LeftDown);

    public static bool RightMousePressed => !ImGui.GetIO().WantCaptureMouse && (_currentMouseState.RightDown && !_lastMouseState.RightDown);

    public static bool MiddleMousePressed => !ImGui.GetIO().WantCaptureMouse && (_currentMouseState.MiddleDown && !_lastMouseState.MiddleDown);

    public static bool LeftMouseReleased => !ImGui.GetIO().WantCaptureMouse && (!_currentMouseState.LeftDown && _lastMouseState.LeftDown);

    public static bool RightMouseReleased => !ImGui.GetIO().WantCaptureMouse && (!_currentMouseState.RightDown && _lastMouseState.RightDown);

    public static bool MiddleMouseReleased => !ImGui.GetIO().WantCaptureMouse && (!_currentMouseState.MiddleDown && _lastMouseState.MiddleDown);

    public static Vector2 MousePosition => _currentMouseState.Position;

    public static float ScrollDelta => ImGui.GetIO().WantCaptureMouse ? 0f : _currentMouseState.Scroll - _lastMouseState.Scroll;

    private struct MouseState
    {
        public Vector2 Position;

        public bool LeftDown;

        public bool RightDown;

        public bool MiddleDown;

        public float Scroll;
    }
}
