using System;
using System.Collections.Generic;
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
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace NotesPlugin;

public  class Hook : IDisposable
{
    
    public virtual unsafe void OnGenerateItemTooltip(NumberArrayData* numberArrayData,
                                                     StringArrayData* stringArrayData)
    {
    }

    private List<Hook> Hooklist = new();

    public void addList(Hook hook)
    {
        Hooklist.Add(hook);
    }
    protected static unsafe SeString?
        GetTooltipString(StringArrayData* stringArrayData, ItemTooltipField field) =>
        GetTooltipString(stringArrayData, (int)field);
    
    
    protected static unsafe TooltipFlags GetTooltipVisibility(int** numberArrayData)
    {
        return (TooltipFlags)(*(*(numberArrayData + 4) + 4));
    }
   
     protected static unsafe SeString? GetTooltipString(StringArrayData* stringArrayData, int field) {
            try {
                if (stringArrayData->AtkArrayData.Size <= field) 
                    throw new IndexOutOfRangeException($"Attempted to get Index#{field} ({field}) but size is only {stringArrayData->AtkArrayData.Size}");

                var stringAddress = new IntPtr(stringArrayData->StringArray[field]);
                return stringAddress == IntPtr.Zero ? null : MemoryHelper.ReadSeStringNullTerminated(stringAddress);
            } catch (Exception ex) {
                Plugin.PluginLog?.Error(ex.Message);
                return new SeString();
            }
     }
        protected static unsafe void 
            SetTooltipString(StringArrayData* stringArrayData, ItemTooltipField field, SeString seString) =>
            SetTooltipString(stringArrayData, (int)field, seString);
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
         Plugin.GameInteropProvider?.InitializeFromAttributes(this);
         Plugin.GameGui!.HoveredItemChanged += GuiOnHoveredItemChanged;
         generateItemTooltipHook?.Enable();
     }

     protected static InventoryItem Item => HoveredItem;
     public static InventoryItem HoveredItem { get; private set; }

     public void Dispose()
     {
         Plugin.GameGui!.HoveredItemChanged -= GuiOnHoveredItemChanged;
         generateItemTooltipHook?.Dispose();
     }

  

     public unsafe void* GenerateItemTooltipDetour(AtkUnitBase* addonItemDetail, NumberArrayData* numberArrayData, StringArrayData* stringArrayData)
     {
         if (!blockItemTooltip)
         {
             try
             {
                 foreach (var hook in Hooklist)
                 {
                     hook.OnGenerateItemTooltip(numberArrayData, stringArrayData);
                 }
             }
             catch (Exception ex)
             {
                 Plugin.PluginLog?.Error(ex.Message);
             } 
         }
         else
         {
             blockItemTooltip = false;
         }

         return generateItemTooltipHook!.Original(addonItemDetail, numberArrayData, stringArrayData);
     }
     private ulong lastItem;
     private bool blockItemTooltip;
     private void GuiOnHoveredItemChanged(object? sender, ulong e)
     {
         if (lastItem == 0 && e != 0)
         {
             blockItemTooltip = true;
             lastItem = e;
         }
         else if (lastItem != 0 && e == 0)
         {
             blockItemTooltip = true;
             lastItem = e;
         }
         else
         {
             blockItemTooltip = false;
             lastItem = e;
         }
     }
     public enum ItemTooltipField : byte {
                 ItemName,
                 GlamourName,
                 ItemUiCategory,
                 ItemDescription = 13,
                 Effects = 16,
                 Levels = 23,
                 DurabilityPercent = 28,
                 SpiritbondPercent = 30,
                 ExtractableProjectableDesynthesizable = 35,
                 Param0 = 37,
                 Param1 = 38,
                 Param2 = 39,
                 Param3 = 40,
                 Param4 = 41,
                 Param5 = 42,
                 ShopSellingPrice = 63,
                 ControlsDisplay = 64,
             }
}
