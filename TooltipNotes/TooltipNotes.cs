using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Text.RegularExpressions;
using Dalamud.Interface.Windowing;
using NotesPlugin.Windows;
using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Dalamud.Game.ClientState;
using Dalamud.Data;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;

namespace NotesPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "TooltipNotes";
        private const string openconfig = "/tnconfig";
        private const string openallNote = "/tnallnotes";
        private const string newNote = "/tnnote";

    

        private readonly InventoryContextMenuItem inventoryContextMenuItem;
        private readonly InventoryContextMenuItem inventoryContextMenuItem2;

        private readonly DalamudContextMenu contextMenuBase;
        
        private WindowSystem windowSystem;

        private readonly NoteWindow noteWindow;
        private readonly ConfigWindow configWindow;
        private readonly AllNotesWindow allNotesWindow;
        
        [PluginService]
        public static ICommandManager? CommandManager { get; private set; }
        
        public readonly Config Config;
        public static string lastNoteKey = "";
        public string oldpluginConfig;
        public Dictionary<string, string> OldNotesDict = new Dictionary<string, string>();
        

        
        [PluginService]
        public static DalamudPluginInterface? PluginInterface { get; private set; }
        
        [PluginService]
        [RequiredVersion("1.0")]
        public static IClientState? ClientState { get; private set; }

        [PluginService]
        [RequiredVersion("1.0")]
        public static IDataManager? DataManager { get; private set; }

        private ulong characterId => ClientState?.LocalContentId ?? 0;
        
        [PluginService]
        public static IPluginLog? PluginLog { get; private set; }
        
        [PluginService]
        public static IGameInteropProvider? GameInteropProvider { get; private set; }
        
        [PluginService]
        public static IGameGui? GameGui { get; private set; }

        private Hook hook;
        private Hook tooltipLogic;


        public Plugin()
        {

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

            CommandManager?.AddHandler(openconfig, new CommandInfo(Onopenconfig)
            {
                HelpMessage = "This opens the TooltipNotes Config"
            });
            CommandManager?.AddHandler(openallNote, new CommandInfo(OnopenallNote)
            {
                HelpMessage = "This opens a Window with all your notes displayed"
            });
            CommandManager?.AddHandler(newNote, new CommandInfo(OnopennewNote)
            {
                HelpMessage = "This lets you open a note window based on the last hovered item "
            });
            
            Config = new Config();
            try
            {
                var pluginConfig = PluginInterface?.GetPluginConfig();
                if (pluginConfig is Config d)
                    Config = d;
                PluginLog?.Debug("Configuration loaded successfully!");
            }
            catch
            {
                PluginLog?.Error("Configuration could not be loaded");
            }
            
            Config.PluginInterface = PluginInterface;
            oldpluginConfig = Path.Combine(PluginInterface?.GetPluginConfigDirectory()!, "Notes.json");

            windowSystem = new(Name);

            noteWindow = new NoteWindow(Config);
            windowSystem.AddWindow(noteWindow);
            configWindow = new ConfigWindow(Name, Config, oldpluginConfig, characterId);
            windowSystem.AddWindow(configWindow);
            allNotesWindow = new AllNotesWindow(Config);
            windowSystem.AddWindow(allNotesWindow);

            PluginInterface!.UiBuilder.Draw += windowSystem.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += () => configWindow.IsOpen = true;

         
            contextMenuBase = new DalamudContextMenu(PluginInterface);
            inventoryContextMenuItem = new InventoryContextMenuItem(
                new SeString(new TextPayload("Add Note")), AddNote, true);
            inventoryContextMenuItem2 = new InventoryContextMenuItem(
                new SeString(new TextPayload("Edit Note")), EditNote, true);
            contextMenuBase.OnOpenInventoryContextMenu += OpenInventoryContextMenuOverride;
            hook = new Hook();
            tooltipLogic = new TooltipLogic(Config,characterId);
            hook.addList(tooltipLogic);
        }
        
        public void Dispose()
        {
            windowSystem.RemoveAllWindows();
            noteWindow.Dispose();
            configWindow.Dispose();
            allNotesWindow.Dispose();
            contextMenuBase.OnOpenInventoryContextMenu -= OpenInventoryContextMenuOverride;
            contextMenuBase.Dispose();
            tooltipLogic.Dispose();
            hook.Dispose();
            CommandManager?.RemoveHandler(openconfig);
            CommandManager?.RemoveHandler(openallNote);
            CommandManager?.RemoveHandler(newNote);
        }

        public void AddNote(InventoryContextMenuItemSelectedArgs args)
        {
            noteWindow.Edit(lastNoteKey);
        }

        public void EditNote(InventoryContextMenuItemSelectedArgs args)
        {
            noteWindow.Edit(lastNoteKey);
        }

        public void Onopenconfig(string command, string args)
        {
            configWindow.IsOpen = true;
        }
        
        public void OnopenallNote(string command, string args)
        {
            allNotesWindow.IsOpen = true;
        }
        
        public void OnopennewNote(string command, string args)
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
                Config.Note note1;
                if (!Config.ContainsKey(lastNoteKey))
                {
                    note1 = new();
                    Config[lastNoteKey] = note1;
                }
                else
                {
                    note1 = Config[lastNoteKey];
                }

                if (hasLabel)
                {
                    note1.Labels.Remove(label);
                    if (note1.Labels.Count == 0 && note1.Text.Length == 0)
                        Config.Remove(lastNoteKey);
                }
                else
                {
                    note1.Labels.Add(label);
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
                    if (label.ShowInMenu && !label.HideLabel)
                    {
                        args.AddCustomItem(createLabelContextMenuItem(label.Name));
                    }
                }
            }
        }
    }
}
