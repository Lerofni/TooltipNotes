using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NotesPlugin;

public class TooltipLogic : Hook
{
    public override unsafe void OnGenerateItemTooltip(
        NumberArrayData* numberArrayData,
        StringArrayData* stringArrayData)
    {
        var test = GetTooltipString(stringArrayData, 1);
        PluginLog.Debug(test.ToString());
        PluginLog.Debug("Test");
        
    }
}
