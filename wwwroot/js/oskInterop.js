//-----------------oskInterop.js----------------------------//
export function addOskClickListener(dotNetRef, oskContainerClass, inputId) {
  const handler = (event) => {
    const isOsk = event.target.closest(`.${oskContainerClass}`);
    const inputElement = document.getElementById(inputId);
    const isInput = inputElement?.contains(event.target);

    console.log("OSK Click Check:", {
      isOsk,
      isInput,
      inputId,
      targetId: event.target.id,
    });

    if (!isOsk && !isInput) {
      dotNetRef.invokeMethodAsync("HandleClickOutside");
    }
  };

  document.addEventListener("click", handler);
  document.addEventListener("touchstart", handler);

  return {
    dispose: () => {
      document.removeEventListener("click", handler);
      document.removeEventListener("touchstart", handler);
    },
  };
}
