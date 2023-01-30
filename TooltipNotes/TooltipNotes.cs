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
using Dalamud.Data;
using Dalamud.Game;
using Lumina.Excel.GeneratedSheets;

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

        public readonly Config Config;
        private string lastNoteKey = "";
        public string oldpluginConfig = "";
        public Dictionary<string, string> OldNotesDict = new Dictionary<string, string>();
        private Config.Note note = new();
        private ulong characterId = 0;
        

        [PluginService]
        [RequiredVersion("1.0")]
        public static ClientState? ClientState { get; private set; }

        [PluginService]
        [RequiredVersion("1.0")]
        public static DataManager? DataManager { get; private set; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            


            ConfigWindow.ForegroundColors.Clear();
            ConfigWindow.GlowColors.Clear();


            if (DataManager != null)
            {
                var colorSheet = DataManager.GetExcelSheet<UIColor>();
                if (colorSheet != null)
                {
                    for (var i = 0u; i < colorSheet.RowCount; i++)
                    {
                        var row = colorSheet.GetRow(i);
                        if (row != null)
                        {
                            ConfigWindow.ForegroundColors.Add(ConfigWindow.ColorInfo.FromUIColor((ushort)i, row.UIForeground));
                            ConfigWindow.GlowColors.Add(ConfigWindow.ColorInfo.FromUIColor((ushort)i, row.UIGlow));
                        }
                    }
                }
            }

            Config = new Config();
            try
            {
                var pluginConfig = pluginInterface.GetPluginConfig();
                if (pluginConfig is Config d)
                    Config = d;
                PluginLog.Debug("Configuration loaded successfully!");
            }
            catch
            {
                PluginLog.Error("Configuration could not be loaded");
            }
            Config.PluginInterface = this.pluginInterface;
            oldpluginConfig = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "Notes.json");
            
            // PluginLog.Debug($"{oldpluginConfig}");
            windowSystem = new(Name);

            noteWindow = new NoteWindow(Config);
            windowSystem.AddWindow(noteWindow);
            characterId = ClientState?.LocalContentId ?? 0;
            configWindow = new ConfigWindow(Name, Config,oldpluginConfig,characterId);
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
            configWindow.Dispose();
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
       

        private InventoryContextMenuItem createLabelContextMenuItem(string label)
        {
            var hasLabel = false;
            if (Config.TryGetValue(lastNoteKey, out var note))
            {
                hasLabel = note.Labels.Contains(label);
            }
            var name = new SeString(new TextPayload($"{(hasLabel ? "Unlabel" : "Label")}: {label}"));
            return new InventoryContextMenuItem(name, args =>
            {
                Config.Note note;
                if (!Config.ContainsKey(lastNoteKey))
                {
                    note = new();
                    Config[lastNoteKey] = note;
                }
                else
                {
                    note = Config[lastNoteKey];
                }

                if (hasLabel)
                {
                    note.Labels.Remove(label);
                    if (note.Labels.Count == 0 && note.Text.Length == 0)
                        Config.Remove(lastNoteKey);
                }
                else
                {
                    note.Labels.Add(label);
                }
                Config.Save();
            }, true);
        }

        private void OpenInventoryContextMenuOverride(InventoryContextMenuOpenArgs args)
        {
            args.AddCustomItem(Config.ContainsKey(lastNoteKey) ? inventoryContextMenuItem2 : inventoryContextMenuItem);

            // Updating labels while editing does not work
            if (!noteWindow.IsOpen)
            {
                foreach (var label in Config.Labels.Values)
                {
                    if (label.ShowInMenu)
                    {
                        args.AddCustomItem(createLabelContextMenuItem(label.Name));
                    }
                }
            }
        }

        public void OnItemTooltipOverride(ItemTooltip itemTooltip, ulong itemid)
        {
            var glamourName = itemTooltip[ItemTooltipString.GlamourName].TextValue;

            ItemTooltipString tooltipField;
            var appendNote = true;
            if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Effects))
            {
                tooltipField = ItemTooltipString.Effects;
                glamourName = "";
            }
            else if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Levels))
            {
                tooltipField = ItemTooltipString.EquipLevel;
            }
            else if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Description))
            {
                appendNote = false;
                tooltipField = ItemTooltipString.Description;
                glamourName = "";
            }
            else
            {
                return;
            }

            if (Config.CharacterSpecific)
            {
                characterId = ClientState?.LocalContentId ?? 0;
                lastNoteKey = $"{characterId:X16}-";
            }
            
            
            if (Config.GlamourSpecific && glamourName.Length > 0)
            {
                lastNoteKey += $"{glamourName}";
            }
            lastNoteKey += itemid;
            PluginLog.Debug($"{lastNoteKey}");
            if (Config.TryGetValue(lastNoteKey, out var note))
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


                void AppendMarkup(Config.Markup markup, string text, Config.Markup fallbackMarkup)
                {
                    if (markup.ColorKey == 0 && markup.GlowColorKey == 0)
                        markup = fallbackMarkup;

                    var foregroundColor = markup.ColorKey;
                    var foregroundAlpha = ConfigWindow.ForegroundColors.Find(c => c.Index == foregroundColor)?.A ?? 0;
                    if (foregroundAlpha == 0)
                    {
                        foregroundColor = fallbackMarkup.ColorKey;
                    }
                    description.AddUiForeground(foregroundColor);

                    var glowColor = markup.GlowColorKey;
                    var glowAlpha = ConfigWindow.ForegroundColors.Find(c => c.Index == glowColor)?.A ?? 0;
                    description.AddUiGlow(glowColor);

                    description.Append(text);

                    description.AddUiGlowOff();

                    description.AddUiForegroundOff();
                }

                if (note.Text.Length > 0)
                {
                    if (Config.NotePrefix)
                    {
                        AppendMarkup(Config.NotePrefixMarkup, "Note: ", Config.Markup.DefaultNotePrefix);
                    }
                    var noteMarkup = Config.EnableStyles ? note.Markup : new();
                    AppendMarkup(noteMarkup, note.Text, Config.NoteMarkup);
                }

                for (var i = 0; i < note.Labels.Count; i++)
                {
                    var label = note.Labels[i];
                    var labelMarkup = new Config.Markup();
                    if (Config.EnableStyles && Config.Labels.TryGetValue(label, out var labelConfig))
                    {
                        labelMarkup = labelConfig.Markup;
                    }
                    if (i == 0)
                    {
                        if (note.Text.Length > 0)
                        {
                            description.Append("\n");
                        }
                        if (Config.LabelPrefix)
                        {
                            AppendMarkup(Config.LabelPrefixMarkup, "Labels: ", Config.Markup.DefaultLabelPrefix);
                        }
                    }
                    else
                    {
                        AppendMarkup(Config.LabelMarkup, ", ", Config.Markup.DefaultLabel);
                    }
                    AppendMarkup(labelMarkup, label, Config.LabelMarkup);
                }


                // If we prepend the note, add some newlines before the original data
                if (!appendNote)
                {
                    description.Append("\n\n");
                    description.Append(originalData);
                }

                // Modify the tooltip
                itemTooltip[tooltipField] = description.Build();
                lastNoteKey = "";
            }

            lastNoteKey = "";
        }
    }
}
