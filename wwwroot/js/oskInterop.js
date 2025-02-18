// oskInterop.js //
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
    Tab: "Tab",
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
    if (event.key === "Tab") {
      return; // Let browser handle focus
    }
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
    const isInput =
      inputElement &&
      (event.target === inputElement || inputElement.contains(event.target));

    // Check if the input is focused (active). If so, do not close the OSK.
    if (!isOsk && !isInput && document.activeElement !== inputElement) {
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

export function slideOutOsk(oskContainerClass, duration) {
  const element = document.querySelector("." + oskContainerClass);
  if (!element) return;

  // Reset to starting position
  element.style.transform = "translate(-50%, 0%)";

  let start = null;
  function step(timestamp) {
    if (!start) start = timestamp;
    const progress = timestamp - start;

    // Calculate vertical offset while maintaining horizontal centering
    const verticalOffset = Math.min((progress / duration) * 100, 100);
    element.style.transform = `translate(-50%, ${verticalOffset}%)`;

    if (progress < duration) {
      window.requestAnimationFrame(step);
    } else {
      // Final position off-screen (keep horizontal centering)
      element.style.transform = "translate(-50%, 100%)";
    }
  }
  window.requestAnimationFrame(step);
}

window.moveToNextFocusableElement = function (currentId) {
  const focusableElements = Array.from(
    document.querySelectorAll(
      'a[href], area[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])'
    )
  ).filter(
    (el) => el.tabIndex >= 0 || el.tagName === "A" || el.tagName === "BUTTON"
  );

  const currentElement = document.getElementById(currentId);
  if (!currentElement) return;

  const currentIndex = focusableElements.indexOf(currentElement);
  if (currentIndex === -1) return;

  const nextIndex = (currentIndex + 1) % focusableElements.length;
  focusableElements[nextIndex].focus();
};
