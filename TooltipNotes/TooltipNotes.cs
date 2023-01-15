﻿using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
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
        // private const string CommandName = "/pmy";
        
        private readonly XivCommonBase XivCommon;
        
        private readonly InventoryContextMenuItem inventoryContextMenuItem;
        private readonly InventoryContextMenuItem inventoryContextMenuItem2;
        
        private readonly DalamudContextMenu contextMenuBase;
        private DalamudPluginInterface PluginInterface { get; init; }
        // private CommandManager CommandManager { get; init; }
       
        
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("TooltipNotesNotesPlugin");
        public ulong id = 0;
        public ulong currentID = 0;
        public string none = "";
        public Dictionary<ulong, string> Notes = new Dictionary<ulong, string>();
       
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            // this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            var filepath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Notes.json");
            
            WindowSystem.AddWindow(new ConfigWindow(this));
            WindowSystem.AddWindow(new MainWindow(this,filepath));
            WindowSystem.AddWindow(new EditWindow(this,filepath));

            
           
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            XivCommon = new XivCommonBase(Hooks.Tooltips);
            XivCommon.Functions.Tooltips.OnItemTooltip += OnItemTooltipOverride;
            contextMenuBase = new DalamudContextMenu();
            inventoryContextMenuItem = new InventoryContextMenuItem(
                new SeString(new TextPayload("Add Note")),AddNote , true);
            inventoryContextMenuItem2 = new InventoryContextMenuItem(
                new SeString(new TextPayload("Edit Note")),EditNote , true);
            contextMenuBase.OnOpenInventoryContextMenu += OpenInventoryContextMenuOverride;

            if (File.Exists(filepath))
            {
                string jsonString = File.ReadAllText(filepath);
                Notes = JsonSerializer.Deserialize<Dictionary<ulong, string>>(jsonString);
                PluginLog.Debug("Notes.json loaded successfully");
            }
            else
            {
                PluginLog.Debug("Notes.json couldn't be loaded or doesn't exist(should resolve upon adding a note");
            }

        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            // this.CommandManager.RemoveHandler(CommandName);
            contextMenuBase.OnOpenInventoryContextMenu -= OpenInventoryContextMenuOverride;
            contextMenuBase.Dispose();
            XivCommon.Functions.Tooltips.OnItemTooltip -= OnItemTooltipOverride;
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            WindowSystem.GetWindow("Note Window").IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void AddNote(InventoryContextMenuItemSelectedArgs args)
        {
            currentID = id;
            WindowSystem.GetWindow("Note Window").IsOpen = true;
        }
        
        public void EditNote(InventoryContextMenuItemSelectedArgs args)
        {
            currentID = id;
            WindowSystem.GetWindow("Edit Window").IsOpen = true;
            EditWindow.Note = Notes[currentID];
        }


        public void DrawConfigUI()
        {
            WindowSystem.GetWindow("A Wonderful Configuration Window").IsOpen = true;
        }

        private void OpenInventoryContextMenuOverride(InventoryContextMenuOpenArgs args)
        {
            args.AddCustomItem(Notes.TryGetValue(id, out none) ? inventoryContextMenuItem2 : inventoryContextMenuItem);
        }
        public void OnItemTooltipOverride(ItemTooltip itemTooltip, ulong itemid)
        {
            string value = "";
            id = itemid;
            ItemTooltipString itemTooltipString;
            itemTooltipString = ItemTooltipString.Description;
            var description = itemTooltip[itemTooltipString];
            if (Notes.TryGetValue(id, out value) && value != "")
            { 
                description = description.Append($"\n\n Note: \n");
                description = description.Append(value);
                description = description.Append($"\n");
                PluginLog.Debug($"Note should say {value}");
            }
            
            itemTooltip[itemTooltipString] = description;
        }
    }
}
