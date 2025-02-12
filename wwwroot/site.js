window.registerClickOutsideHandler = function (dotNetHelper) {
  document.addEventListener("click", function (event) {
    let keyboard = document.querySelector(".osk-container");
    let inputFields = document.querySelectorAll("input");
    if (
      keyboard &&
      !keyboard.contains(event.target) &&
      ![...inputFields].includes(event.target)
    ) {
      dotNetHelper.invokeMethodAsync("CloseOskFromJS");
    }
  });
};

window.removeClickOutsideHandler = function () {
  document.removeEventListener("click", this);
};

window.addClickOutsideListener = (elementId, dotnetHelper) => {
  document.addEventListener("click", (event) => {
    const element = document.getElementById(elementId);
    if (element && !element.contains(event.target)) {
      dotnetHelper.invokeMethodAsync("OnClickOutside");
    }
  });
};
