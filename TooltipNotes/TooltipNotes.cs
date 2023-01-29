using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using NotesPlugin.Windows;
using XivCommon;
using XivCommon.Functions.Tooltips;
using Dalamud.ContextMenu;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Logging;

namespace NotesPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "TooltipNotes";

        private readonly XivCommonBase XivCommon;

        private readonly InventoryContextMenuItem inventoryContextMenuItem;
        private readonly InventoryContextMenuItem inventoryContextMenuItem2;

        private readonly DalamudContextMenu contextMenuBase;
        private DalamudPluginInterface PluginInterface { get; init; }

        public WindowSystem WindowSystem = new("TooltipNotes");

        private NoteWindow NoteWindow { get; init; }

        public readonly Notes Notes;
        public string EditingNoteKey = "";
        public string LastNoteKey = "";

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            // CommandManager = commandManager;

            // you might normally want to embed resources and load them from the manifest stream
            var filepath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Notes.json");
            Notes = new Notes(filepath);

            NoteWindow = new NoteWindow(this);

            WindowSystem.AddWindow(NoteWindow);

            PluginInterface.UiBuilder.Draw += DrawUI;

            XivCommon = new XivCommonBase(Hooks.Tooltips);
            XivCommon.Functions.Tooltips.OnItemTooltip += OnItemTooltipOverride;
            contextMenuBase = new DalamudContextMenu();
            inventoryContextMenuItem = new InventoryContextMenuItem(
                new SeString(new TextPayload("Add Note")), AddNote, true);
            inventoryContextMenuItem2 = new InventoryContextMenuItem(
                new SeString(new TextPayload("Edit Note")), EditNote, true);
            contextMenuBase.OnOpenInventoryContextMenu += OpenInventoryContextMenuOverride;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            NoteWindow.Dispose();
            contextMenuBase.OnOpenInventoryContextMenu -= OpenInventoryContextMenuOverride;
            contextMenuBase.Dispose();
            XivCommon.Functions.Tooltips.OnItemTooltip -= OnItemTooltipOverride;
            XivCommon.Dispose();
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        private void editNote()
        {
            EditingNoteKey = LastNoteKey;
            NoteWindow.IsOpen = true;
            if (Notes.ContainsKey(EditingNoteKey))
                NoteWindow.Note = Notes[EditingNoteKey];
        }

        public void AddNote(InventoryContextMenuItemSelectedArgs args)
        {
            editNote();
        }

        public void EditNote(InventoryContextMenuItemSelectedArgs args)
        {
            editNote();
        }

        private void OpenInventoryContextMenuOverride(InventoryContextMenuOpenArgs args)
        {
            args.AddCustomItem(Notes.ContainsKey(LastNoteKey) ? inventoryContextMenuItem2 : inventoryContextMenuItem);
        }

        public void OnItemTooltipOverride(ItemTooltip itemTooltip, ulong itemid)
        {
            var glam = itemTooltip[ItemTooltipString.GlamourName];

            ItemTooltipString tooltipField;

            if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Levels))
            {
                tooltipField = ItemTooltipString.EquipLevel;
            }
            else if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Description))
            {
                tooltipField = ItemTooltipString.Description;
                glam = "";
            }
            else if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Effects))
            {
                tooltipField = ItemTooltipString.Effects;
                glam = "";
            }
            else
            {
                return;
            }

            LastNoteKey = string.Format("{0}{1}", glam, itemid.ToString());
            if (LastNoteKey.Length > 8)
            {
                LastNoteKey = LastNoteKey.Remove(0, 2);
            }

            var description = new SeStringBuilder();
            description.Append(itemTooltip[tooltipField]);

            if (Notes.TryGetValue(LastNoteKey, out var noteText))
            {
                // Thanks to NotNite from the Discord for the help!
                // Color table: https://i.imgur.com/cZceCI3.png
                // Data (the 'key' is the 'colorKey' parameter)
                // https://github.com/xivapi/ffxiv-datamining/blob/master/csv/UIColor.csv
                // Using AddUiForegroundOff doesn't work because the whole cell is colored
                description.AddUiForeground(1);
                description.AddUiGlow(60);
                description.Append("\n\nNote: ");
                description.AddUiGlowOff();
                description.Append(noteText);
                description.AddUiForegroundOff();
                PluginLog.Debug($"Note should say {noteText}");
                PluginLog.Debug($"itemid: {itemid}, glamid: {LastNoteKey}, flags:\n{string.Join("\n", itemTooltip.Fields.ToString().Split(' '))}");
            }
            itemTooltip[tooltipField] = description.Build();
        }
    }
}
