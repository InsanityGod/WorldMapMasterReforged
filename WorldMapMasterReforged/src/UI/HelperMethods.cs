using System;
using Vintagestory.API.Client;

namespace WorldMapMaster.src.UI;

public static class HelperMethods
{
    /// <summary>
    /// Adds a text input to the current GUI.
    /// </summary>
    /// <param name="composer"></param>
    /// <param name="bounds">The bounds of the text input.</param>
    /// <param name="onTextChanged">The event fired when the text is changed.</param>
    /// <param name="font">The font of the text.</param>
    /// <param name="key">The name of this text component.</param>
    public static GuiComposer AddAutoclearingText(this GuiComposer composer, ElementBounds bounds, Action<string> onTextChanged, CairoFont font = null, string key = null) => composer.Composed
            ? composer
            : composer.AddInteractiveElement(new GuiElementAutoclearingText(composer.Api, bounds, onTextChanged, font ?? CairoFont.TextInput()), key);

    /// <summary>
    /// Gets the text input by input name.
    /// </summary>
    /// <param name="composer"></param>
    /// <param name="key">The name of the text input to get.</param>
    /// <returns>The named text input</returns>
    public static GuiElementAutoclearingText GetAutoclearingText(this GuiComposer composer, string key) => (GuiElementAutoclearingText)composer.GetElement(key);
}
