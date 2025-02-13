using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace oskPro.Services;

public interface IOskService
{
    event Action<string>? OnKeyPressed;
    event Action? OnVisibilityChanged;
    event Action<string, bool>? OnPhysicalKeyPress;
    Task OpenOsk(MudTextField<string> field, bool showNumpad = false);
    void CloseOsk();
    void KeyPressed(string key, bool isPhysical);
    string GetDisplayKey(string key, bool isShift, bool isCaps, bool isAltGr);
    MudTextField<string>? ActiveField { get; }
    bool IsVisible { get; }
    bool IsNumpadVisible { get; }
}

public class CursorPosition
{
    public int SelectionStart { get; set; }
    public int SelectionEnd { get; set; }
}

public partial class OskService : IOskService, IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private MudTextField<string>? _activeField;
    private DotNetObjectReference<OskService>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _oskListener;

    public OskService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public MudTextField<string>? ActiveField => _activeField;
    public bool IsVisible { get; private set; }
    public bool IsNumpadVisible { get; private set; }
    private static readonly string[] sourceArray = ["Shift", "Ctrl", "Alt", "Win", "AltGr", "Menu", "Caps"];

    public event Action<string>? OnKeyPressed;
    public event Action? OnVisibilityChanged;
    public event Action<string, bool>? OnPhysicalKeyPress;

    public async Task OpenOsk(MudTextField<string> field, bool showNumpad = false)
    {
        try
        {
            _activeField = field;
            IsNumpadVisible = showNumpad;
            IsVisible = true;

            await Task.Delay(10);

            _jsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/oskInterop.js");
            _dotNetRef ??= DotNetObjectReference.Create(this);

            if (_oskListener != null)
            {
                await _oskListener.InvokeVoidAsync("dispose");
                await _oskListener.DisposeAsync();
            }

            var inputId = field.GetInputId();
            _oskListener = await _jsModule.InvokeAsync<IJSObjectReference>(
                "initializeOsk",
                _dotNetRef,
                "osk-container",
                inputId
            );

            OnVisibilityChanged?.Invoke();
            await field.FocusAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening OSK: {ex.Message}");
        }
    }

    public void CloseOsk()
    {
        if (!IsVisible) return;

        IsVisible = false;
        IsNumpadVisible = false;
        _activeField = null;

        OnVisibilityChanged?.Invoke();
    }

    [JSInvokable]
    public async Task HandleClickOutside()
    {
        CloseOsk();
        if (_activeField != null)
        {
            await _activeField.BlurAsync();
        }
    }

    [JSInvokable]
    public void HandlePhysicalKeyPress(string key, bool isModifier)
    {
        OnPhysicalKeyPress?.Invoke(key, isModifier);
    }

    public async void KeyPressed(string key, bool isPhysical)
    {
        if (_activeField == null || isPhysical) return;

        var currentValue = _activeField.Value ?? "";
        var cursorInfo = await _jsRuntime.InvokeAsync<CursorPosition>("getCursorPosition", _activeField.GetInputId());

        var newValue = key switch
        {
            "Backspace" => HandleBackspace(currentValue, cursorInfo),
            "Space" => InsertAtPosition(currentValue, " ", cursorInfo),
            "Enter" => InsertAtPosition(currentValue, "\n", cursorInfo),
            _ => InsertAtPosition(currentValue, key, cursorInfo)
        };

        await _activeField.ValueChanged.InvokeAsync(newValue);
        OnKeyPressed?.Invoke(key);

        // Update cursor position after text change
        await _jsRuntime.InvokeVoidAsync("setCursorPosition",
            _activeField.GetInputId(),
            GetNewCursorPosition(key, cursorInfo));
    }

    private string HandleBackspace(string text, CursorPosition cursorInfo)
    {
        if (cursorInfo.SelectionStart != cursorInfo.SelectionEnd)
        {
            // Handle selection deletion
            return text[..cursorInfo.SelectionStart] + text[cursorInfo.SelectionEnd..];
        }
        else if (cursorInfo.SelectionStart > 0)
        {
            // Delete character before cursor
            return text[..(cursorInfo.SelectionStart - 1)] + text[cursorInfo.SelectionStart..];
        }
        return text;
    }

    private string InsertAtPosition(string text, string insert, CursorPosition cursorInfo)
    {
        // Replace selection if exists
        if (cursorInfo.SelectionStart != cursorInfo.SelectionEnd)
        {
            return text[..cursorInfo.SelectionStart] + insert + text[cursorInfo.SelectionEnd..];
        }

        // Insert at cursor position
        return text[..cursorInfo.SelectionStart] + insert + text[cursorInfo.SelectionStart..];
    }

    private int GetNewCursorPosition(string key, CursorPosition cursorInfo)
    {
        if (key == "Backspace")
        {
            if (cursorInfo.SelectionStart != cursorInfo.SelectionEnd)
                return cursorInfo.SelectionStart;
            return Math.Max(0, cursorInfo.SelectionStart - 1);
        }

        return cursorInfo.SelectionStart + 1;
    }

    public string GetDisplayKey(string key, bool isShift, bool isCaps, bool isAltGr)
    {
        if (IsModifierKey(key)) return key;

        if (isShift || isCaps)
        {
            return key switch
            {
                "^" => "°",
                "1" => "!",
                "2" => "\"",
                "3" => "§",
                "4" => "$",
                "5" => "%",
                "6" => "&",
                "7" => "/",
                "8" => "(",
                "9" => ")",
                "0" => "=",
                "ß" => "?",
                "'" => "`",
                "ü" => "Ü",
                "+" => "*",
                "#" => "'",
                "ö" => "Ö",
                "ä" => "Ä",
                "," => ";",
                "." => ":",
                "-" => "_",
                "<" => ">",
                _ => key.ToUpper()
            };
        }
        else if (isAltGr)
        {
            return key switch
            {
                "q" => "@",
                "e" => "€",
                "+" => "~",
                "c" => "¢",
                "n" => "¬",
                "7" => "{",
                "8" => "[",
                "9" => "]",
                "0" => "}",
                "ß" => "\\",
                "ü" => "|",
                _ => key
            };
        }
        return key;
    }

    private static bool IsModifierKey(string key) => sourceArray.Contains(key);

    public async void Dispose()
    {
        _dotNetRef?.Dispose();
        if (_jsModule != null)
            await _jsModule.DisposeAsync();
        if (_oskListener != null)
            await _oskListener.DisposeAsync();
    }
}
public static class MudTextFieldExtensions
{
    public static string GetInputId(this MudTextField<string> field)
    {
        return field.InputId!;
    }
}