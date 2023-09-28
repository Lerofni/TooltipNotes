using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Memory.Exceptions;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NotesPlugin;

public class Hook : IDisposable
{
    [PluginService]
    public static IPluginLog? PluginLog { get; private set; }
    
    [PluginService]
    public static IGameGui GameGui { get; private set; }
    public virtual unsafe void OnGenerateItemTooltip(NumberArrayData* numberArrayData,
                                                     StringArrayData* stringArrayData)
    {
    }
    [PluginService]
    private static IGameInteropProvider GameInteropProvider { get;  set; }
     protected static unsafe SeString? GetTooltipString(StringArrayData* stringArrayData, int field) {
            try {
                if (stringArrayData->AtkArrayData.Size <= field) 
                    throw new IndexOutOfRangeException($"Attempted to get Index#{field} ({field}) but size is only {stringArrayData->AtkArrayData.Size}");

                var stringAddress = new IntPtr(stringArrayData->StringArray[field]);
                return stringAddress == IntPtr.Zero ? null : MemoryHelper.ReadSeStringNullTerminated(stringAddress);
            } catch (Exception ex) {
                PluginLog.Error(ex.Message);
                return new SeString();
            }
     }

     protected static unsafe void SetTooltipString(StringArrayData* stringArrayData, int field, SeString seString) {
         seString ??= new SeString();
         var bytes = seString.Encode().ToList();
         bytes.Add(0);
         stringArrayData->SetValue((int)field, bytes.ToArray(), false, true, false);
     }
    
     private unsafe delegate void* GenerateItemTooltip(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData);
     [Signature("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 48 8B 42 20", DetourName = nameof(GenerateItemTooltipDetour),  UseFlags = SignatureUseFlags.Hook)]
     private Hook<GenerateItemTooltip>? generateItemTooltipHook = null;

     public Hook()
     {
         
         GameInteropProvider?.InitializeFromAttributes(this);
         generateItemTooltipHook?.Enable();
     }

   

     public void Dispose()
     {
         generateItemTooltipHook?.Dispose();
     }

  

     public unsafe void* GenerateItemTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
     {
         try
         {
             OnGenerateItemTooltip(numberArrayData, stringArrayData);
         }
         catch (Exception ex)
         {
             PluginLog.Error(ex.Message);
         }
                 
         return generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
     }
}
