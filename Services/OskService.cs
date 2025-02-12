using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace oskPro.Services;

public interface IOskService
{
  event Action<string>? OnKeyPressed;
  event Action? OnVisibilityChanged;
  Task OpenOsk(MudTextField<string> field, bool showNumpad = false);
  void CloseOsk();
  void KeyPressed(string key);
  MudTextField<string>? ActiveField { get; }
  bool IsVisible { get; }
  bool IsNumpadVisible { get; }
}

public partial class OskService(IJSRuntime jsRuntime) : IOskService, IDisposable
{
  private readonly IJSRuntime _jsRuntime = jsRuntime;
  private MudTextField<string>? _activeField;
  private DotNetObjectReference<OskService>? _dotNetRef;
  private IJSObjectReference? _jsModule;
  private IJSObjectReference? _oskListener;
  private CancellationTokenSource? _closeCts = new();

  public MudTextField<string>? ActiveField => _activeField;
  public bool IsVisible { get; private set; }
  public bool IsNumpadVisible { get; private set; }
  public event Action<string>? OnKeyPressed;
  public event Action? OnVisibilityChanged;

  public async Task OpenOsk(MudTextField<string> field, bool showNumpad = false)
  {
    try
    {
      _closeCts?.Cancel();
      _closeCts = new CancellationTokenSource();

      _activeField = field;
      IsNumpadVisible = showNumpad;
      IsVisible = true;

      await Task.Delay(10); // Allow DOM update

      _jsModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/oskInterop.js");
      _dotNetRef ??= DotNetObjectReference.Create(this);

      // Dispose previous listener
      if (_oskListener != null)
      {
        await _oskListener.InvokeVoidAsync("dispose");
        await _oskListener.DisposeAsync();
      }

      var inputId = field.GetInputId();
      _oskListener = await _jsModule.InvokeAsync<IJSObjectReference>(
          "addOskClickListener",
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

  public async void CloseOsk()
  {
    if (!IsVisible) return;

    IsVisible = false;
    IsNumpadVisible = false;
    _activeField = null;

    if (_oskListener != null)
    {
      await _oskListener.InvokeVoidAsync("dispose");
      await _oskListener.DisposeAsync();
      _oskListener = null;
    }

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

  public void KeyPressed(string key)
  {
    if (_activeField == null) return;

    var currentValue = _activeField.Value ?? "";
    var newValue = key switch
    {
      "Backspace" => currentValue.Length > 0 ? currentValue[..^1] : "",
      "Space" => currentValue + " ",
      _ => currentValue + key
    };
    _activeField.ValueChanged.InvokeAsync(newValue);
    OnKeyPressed?.Invoke(key);
  }

  public async void Dispose()
  {
    _dotNetRef?.Dispose();
    if (_jsModule != null)
      await _jsModule.DisposeAsync();
    if (_oskListener != null)
      await _oskListener.DisposeAsync();
    GC.SuppressFinalize(this);
  }
}

public static class MudTextFieldExtensions
{
  public static string GetInputId(this MudTextField<string> field)
  {
    return field.InputId!;
  }
}