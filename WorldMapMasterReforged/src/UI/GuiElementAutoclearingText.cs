using System;
using Vintagestory.API.Client;

namespace WorldMapMaster.src.UI;

public class GuiElementAutoclearingText : GuiElementTextInput
{
    public GuiElementAutoclearingText(ICoreClientAPI capi, ElementBounds bounds, Action<string> onTextChanged, CairoFont font) : base(capi, bounds, onTextChanged, font)
    {
    }

    public override void OnFocusGained()
    {
        base.OnFocusGained();
        SetValue("");
    }
}
