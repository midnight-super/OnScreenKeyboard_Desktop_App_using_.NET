export function initializeOsk(dotNetRef, oskContainerClass, inputId) {
  const keyboardMap = {
    a: "a",
    b: "b",
    c: "c",
    d: "d",
    e: "e",
    f: "f",
    g: "g",
    h: "h",
    i: "i",
    j: "j",
    k: "k",
    l: "l",
    m: "m",
    n: "n",
    o: "o",
    p: "p",
    q: "q",
    r: "r",
    s: "s",
    t: "t",
    u: "u",
    v: "v",
    w: "w",
    x: "x",
    y: "y",
    z: "z",
    0: "0",
    1: "1",
    2: "2",
    3: "3",
    4: "4",
    5: "5",
    6: "6",
    7: "7",
    8: "8",
    9: "9",
    ",": ",",
    ".": ".",
    "-": "-",
    "+": "+",
    ß: "ß",
    ü: "ü",
    ö: "ö",
    ä: "ä",
    "#": "#",
    "<": "<",
    " ": "Space",
    Enter: "Enter",
    Backspace: "Backspace",
  };

  const modifierKeys = {
    ShiftLeft: "Shift",
    ShiftRight: "Shift",
    CapsLock: "Caps",
    AltRight: "AltGr",
    ControlLeft: "Ctrl",
    ControlRight: "Ctrl",
    AltLeft: "Alt",
    MetaLeft: "Win",
    MetaRight: "Menu",
  };

  const keydownHandler = (event) => {
    const inputElement = document.getElementById(inputId);
    if (!inputElement?.matches(":focus")) return;

    const key = event.key.toLowerCase();
    const code = event.code;

    if (code in modifierKeys) {
      dotNetRef.invokeMethodAsync(
        "HandlePhysicalKeyPress",
        modifierKeys[code],
        true
      );
      return;
    }

    let mappedKey = null;
    if (key in keyboardMap) {
      mappedKey = keyboardMap[key];
    } else if (event.code === "Backspace") {
      mappedKey = "Backspace";
    } else if (event.code === "Space") {
      mappedKey = "Space";
    } else if (event.code === "Enter") {
      mappedKey = "Enter";
    }

    if (mappedKey) {
      dotNetRef.invokeMethodAsync("HandlePhysicalKeyPress", mappedKey, false);
    }
  };

  const clickHandler = (event) => {
    const isOsk = event.target.closest(`.${oskContainerClass}`);
    const inputElement = document.getElementById(inputId);
    const isInput = inputElement?.contains(event.target);

    if (!isOsk && !isInput) {
      dotNetRef.invokeMethodAsync("HandleClickOutside");
    }
  };

  document.addEventListener("keydown", keydownHandler);
  document.addEventListener("click", clickHandler);
  document.addEventListener("touchstart", clickHandler);

  return {
    dispose: () => {
      document.removeEventListener("keydown", keydownHandler);
      document.removeEventListener("click", clickHandler);
      document.removeEventListener("touchstart", clickHandler);
    },
  };
}

// Functions to handle cursor position
window.getCursorPosition = function (inputId) {
  const input = document.getElementById(inputId);
  if (!input) return { selectionStart: 0, selectionEnd: 0 };

  return {
    selectionStart: input.selectionStart,
    selectionEnd: input.selectionEnd,
  };
};

window.setCursorPosition = function (inputId, position) {
  const input = document.getElementById(inputId);
  if (!input) return;

  input.setSelectionRange(position, position);
};
