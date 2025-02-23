# On-Screen Keyboard (OSK) Integration Guide

This guide provides detailed instructions for integrating the On-Screen Keyboard (OSK) service into your Blazor Hybrid application. By following the steps outlined below, you will be able to enhance your application's user interface with a fully functional on-screen keyboard.

## Table of Contents

1. [Registering the OSK Service in Program.cs](#registering-the-osk-service-in-programcs)
2. [Adding the OSK to a MudTextField](#adding-the-osk-to-a-mudtextfield)
3. [Customizing the Layout](#customizing-the-layout)
4. [Setup Instructions](#setup-instructions)
5. [Potential Pitfalls and Considerations](#potential-pitfalls-and-considerations)

## 1. Registering the OSK Service in Program.cs

To utilize the On-Screen Keyboard (OSK) in your Blazor Hybrid application, begin by registering the OSK service in the `Program.cs` file.

```csharp
builder.Services.AddScoped<OskService>();
```

````

## 2. Adding the OSK to a MudTextField

To integrate the OSK with a MudTextField, bind the field to the OSK service for seamless interaction. Example usage:

```html
<MudTextField @bind-Value="textInput" />
```

Ensure that the `textInput` variable is defined in your component code.

## 3. Customizing the Layout

The OSK component allows for customization, including the option to display a numpad layout. To enable the numpad, use the `ShowNumpad` parameter:

```html
<OnScreenKeyboard ShowNumpad="true" />
```

## 4. Setup Instructions

To integrate the OSK into another Blazor Hybrid project, follow these steps:

### 1. Copy Required Files

- Copy the `OskService.cs` file into your Services folder.
- Copy the `OnScreenKeyboard.razor` file into your Components folder.
- Copy the `wwwroot/js/oskInterop.js` file into your `wwwroot/js` folder.
- Copy the `wwwroot/css/osk.css` file into your `wwwroot/css` folder.
- Ensure your `MainPage.razor` includes the OnScreenKeyboard component.

### 2. Update Program.cs

Register the `OskService` as shown in section 1.

### 3. Modify Usage in Components

- Inject `OskService` where needed:

```csharp
@inject OskService OskService
```

- Use `MudTextField` with `OskService` binding for text entry fields:

```html
<MudTextField @bind-Value="OskService.InputValue" />
```

## 5. Potential Pitfalls and Considerations

### a) Handling Input Binding

Use `@bind-Value` to ensure the OSK updates the text field appropriately.

### b) Managing Styles and Responsiveness

If necessary, adjust styles in `osk.css` to fit the UI and ensure a responsive design.

---

Following these guidelines will facilitate a smooth integration of the OSK into your Blazor Hybrid application, providing an enhanced user experience.

```

You can copy and paste this code into a file named `README.md` in your project as needed.
```
````
