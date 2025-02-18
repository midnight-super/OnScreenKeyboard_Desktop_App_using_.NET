// OskService.cs //
using Microsoft.JSInterop;
using MudBlazor;

namespace oskPro.Services;

public interface IOskService
{
    event Action<string>? OnKeyPressed;
    event Action? OnVisibilityChanged;
    event Action<string, bool>? OnPhysicalKeyPress;
    Task OpenOsk(MudTextField<string> field, bool showNumpad = false);
    Task CloseOskAsync();
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

public partial class OskService(IJSRuntime jsRuntime) : IOskService, IDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private MudTextField<string>? _activeField;
    private DotNetObjectReference<OskService>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _oskListener;

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

            // await _jsModule.InvokeVoidAsync("slideInOsk", "osk-container", 300);

            await field.FocusAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening OSK: {ex.Message}");
        }
    }

    public async Task CloseOskAsync()
    {
        if (!IsVisible) return;

        if (_jsModule != null)
        {
            // Call fadeOut effect (duration in ms)
            await _jsModule.InvokeVoidAsync("slideOutOsk", "osk-container", 300);
        }
        // Wait for the fade-out to complete before hiding
        await Task.Delay(300);

        IsVisible = false;
        IsNumpadVisible = false;
        _activeField = null;

        OnVisibilityChanged?.Invoke();
    }

    [JSInvokable]
    public async Task HandleClickOutside()
    {
        await CloseOskAsync();
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
        if (key == "Tab")
        {
            var inputId = _activeField.GetInputId();
            await _jsRuntime.InvokeVoidAsync("moveToNextFocusableElement", inputId);
            // await CloseOskAsync();
            return;
        }
        var currentValue = _activeField.Value ?? "";
        var cursorInfo = await _jsRuntime.InvokeAsync<CursorPosition>("getCursorPosition", _activeField.GetInputId());

        var newValue = key switch
        {
            "Backspace" => HandleBackspace(currentValue, cursorInfo),
            "⬅" => HandleBackspace(currentValue, cursorInfo),
            "Space" => InsertAtPosition(currentValue, " ", cursorInfo),
            "Enter" => InsertAtPosition(currentValue, "\n", cursorInfo),
            "BACKSPACE" => HandleBackspace(currentValue, cursorInfo),
            "SPACE" => InsertAtPosition(currentValue, " ", cursorInfo),
            "ENTER" => InsertAtPosition(currentValue, "\n", cursorInfo),
            _ => InsertAtPosition(currentValue, key, cursorInfo)
        };
        await _activeField.ValueChanged.InvokeAsync(newValue);
        OnKeyPressed?.Invoke(key);

        // Update the cursor position after the text change
        await _jsRuntime.InvokeVoidAsync("setCursorPosition",
            _activeField.GetInputId(),
            GetNewCursorPosition(key, cursorInfo));
    }

    private static string HandleBackspace(string text, CursorPosition cursorInfo)
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

    private static string InsertAtPosition(string text, string insert, CursorPosition cursorInfo)
    {
        // Replace selection if exists
        if (cursorInfo.SelectionStart != cursorInfo.SelectionEnd)
        {
            return text[..cursorInfo.SelectionStart] + insert + text[cursorInfo.SelectionEnd..];
        }

        // Insert at cursor position
        return text[..cursorInfo.SelectionStart] + insert + text[cursorInfo.SelectionStart..];
    }

    private static int GetNewCursorPosition(string key, CursorPosition cursorInfo)
    {
        if (key == "Backspace" || key == "⬅")
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
        if (isCaps && isShift) return key.ToLower();
        // Handle Shift only for symbols & numbers
        if (isShift)
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
                _ => key.ToUpper() // Default to uppercase
            };
        }

        // Handle Caps Lock only for letters (A-Z, a-z)
        if (isCaps && key.Length == 1 && char.IsLetter(key, 0))
        {
            return key.ToUpper();
        }
        // Handle AltGr (for special characters)
        if (isAltGr)
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
