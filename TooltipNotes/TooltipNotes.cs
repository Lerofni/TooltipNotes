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
using Dalamud.Game.ClientState;

namespace NotesPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "TooltipNotes";

        private readonly XivCommonBase XivCommon;

        private readonly InventoryContextMenuItem inventoryContextMenuItem;
        private readonly InventoryContextMenuItem inventoryContextMenuItem2;

        private readonly DalamudContextMenu contextMenuBase;
        private readonly DalamudPluginInterface pluginInterface;

        private WindowSystem windowSystem;

        private readonly NoteWindow noteWindow;
        private readonly ConfigWindow configWindow;

        public readonly Config Notes;
        private string lastNoteKey = "";

        [PluginService]
        [RequiredVersion("1.0")]
        public static ClientState? ClientState { get; private set; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.pluginInterface = pluginInterface;

            Notes = new Config();
            try
            {
                var pluginConfig = pluginInterface.GetPluginConfig();
                if (pluginConfig is Config d)
                    Notes = d;
                PluginLog.Debug("Configuration loaded successfully!");
            }
            catch
            {
                PluginLog.Error("Configuration could not be loaded");
            }
            Notes.PluginInterface = this.pluginInterface;

            windowSystem = new(Name);

            noteWindow = new NoteWindow(Notes);
            windowSystem.AddWindow(noteWindow);

            configWindow = new ConfigWindow(Name, Notes);
            windowSystem.AddWindow(configWindow);

            this.pluginInterface.UiBuilder.Draw += windowSystem.Draw;
            pluginInterface.UiBuilder.OpenConfigUi += () => configWindow.IsOpen = true;

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
            windowSystem.RemoveAllWindows();
            noteWindow.Dispose();
            contextMenuBase.OnOpenInventoryContextMenu -= OpenInventoryContextMenuOverride;
            contextMenuBase.Dispose();
            XivCommon.Functions.Tooltips.OnItemTooltip -= OnItemTooltipOverride;
            XivCommon.Dispose();
        }

        public void AddNote(InventoryContextMenuItemSelectedArgs args)
        {
            noteWindow.Edit(lastNoteKey);
        }

        public void EditNote(InventoryContextMenuItemSelectedArgs args)
        {
            noteWindow.Edit(lastNoteKey);
        }

        private void OpenInventoryContextMenuOverride(InventoryContextMenuOpenArgs args)
        {
            args.AddCustomItem(Notes.ContainsKey(lastNoteKey) ? inventoryContextMenuItem2 : inventoryContextMenuItem);
        }

        public void OnItemTooltipOverride(ItemTooltip itemTooltip, ulong itemid)
        {
            var glamourName = itemTooltip[ItemTooltipString.GlamourName].TextValue;

            ItemTooltipString tooltipField;
            var appendNote = true;
            if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Levels))
            {
                tooltipField = ItemTooltipString.EquipLevel;
            }
            else if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Description))
            {
                appendNote = false;
                tooltipField = ItemTooltipString.Description;
                glamourName = "";
            }
            else if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Effects))
            {
                tooltipField = ItemTooltipString.Effects;
                glamourName = "";
            }
            else
            {
                return;
            }

            if (Notes.CharacterSpecific)
            {
                var characterId = ClientState?.LocalContentId ?? 0;
                lastNoteKey = $"{characterId:X16}-";
            }
            lastNoteKey += itemid;
            if (Notes.GlamourSpecific && glamourName.Length > 0)
            {
                lastNoteKey += $"~{glamourName}";
            }

            if (Notes.TryGetValue(lastNoteKey, out var note))
            {
                var originalData = itemTooltip[tooltipField];
                var description = new SeStringBuilder();

                // If we append the note to the end of the field, add the original data first
                if (appendNote)
                {
                    description.Append(originalData);
                    description.Append("\n\n");
                }

                // Thanks to NotNite from the Discord for the help!
                // Color table: https://i.imgur.com/cZceCI3.png
                // Data (the 'key' is the 'colorKey' parameter)
                // https://github.com/xivapi/ffxiv-datamining/blob/master/csv/UIColor.csv
                // Using AddUiForegroundOff doesn't work because the whole cell is colored

                void AppendMarkup(Config.Markup markup, string text)
                {
                    if (markup.ColorKey >= 580)
                    {
                        // Fall back to default style
                        markup = Notes.DefaultMarkup;
                    }

                    description.AddUiForeground(markup.ColorKey);

                    if(markup.GlowColorKey < 580)
                    {
                        description.AddUiGlow(markup.GlowColorKey);
                    }

                    description.Append(text);

                    if (markup.GlowColorKey < 580)
                    {
                        description.AddUiGlowOff();
                    }

                    description.AddUiForegroundOff();
                }

                if (Notes.EnableStyles)
                {
                    AppendMarkup(Notes.PrefixMarkup, "Note: ");
                    AppendMarkup(note.Markup, note.Text);
                }
                else
                {
                    description.AddUiForeground(1);
                    description.AddUiGlow(60);
                    description.Append("Note: ");
                    description.AddUiGlowOff();
                    description.Append(note.Text);
                    description.AddUiForegroundOff();
                }

                // If we prepend the note, add some newlines before the original data
                if (!appendNote)
                {
                    description.Append("\n\n");
                    description.Append(originalData);
                }

                // Modify the tooltip
                itemTooltip[tooltipField] = description.Build();
            }
        }
    }
}
