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
        // private const string CommandName = "/pmy";
        
        private readonly XivCommonBase XivCommon;
        
        private readonly InventoryContextMenuItem inventoryContextMenuItem;
        private readonly InventoryContextMenuItem inventoryContextMenuItem2;
        
        private readonly DalamudContextMenu contextMenuBase;
        private DalamudPluginInterface PluginInterface { get; init; }
        // private CommandManager CommandManager { get; init; }
       
        
        
        public WindowSystem WindowSystem = new("TooltipNotes");
        
        private MainWindow MainWindow { get; init; }
        private EditWindow EditWindow { get; init; }
        public ulong id = 0;
        public string currentglamid = "";
        public string none = "";
        public SeString glam = $"";
        public string glamid = "";
        public Dictionary<string, string> Notes = new Dictionary<string, string>();
       
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            // this.CommandManager = commandManager;

           

            // you might normally want to embed resources and load them from the manifest stream
            var filepath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "Notes.json");
            
            // WindowSystem.AddWindow(new MainWindow(this,filepath));
            // WindowSystem.AddWindow(new EditWindow(this,filepath));
            MainWindow = new MainWindow(this, filepath);
            EditWindow = new EditWindow(this, filepath);
            
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(EditWindow);
           
            this.PluginInterface.UiBuilder.Draw += DrawUI;
           
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
                Notes = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
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
            MainWindow.Dispose();
            EditWindow.Dispose();
            // this.CommandManager.RemoveHandler(CommandName);
            contextMenuBase.OnOpenInventoryContextMenu -= OpenInventoryContextMenuOverride;
            contextMenuBase.Dispose();
            XivCommon.Functions.Tooltips.OnItemTooltip -= OnItemTooltipOverride;
            
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            // WindowSystem.GetWindow("Note Window").IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void AddNote(InventoryContextMenuItemSelectedArgs args)
        {
            currentglamid = glamid;
            MainWindow.IsOpen = true;
        }
        
        public void EditNote(InventoryContextMenuItemSelectedArgs args)
        {
            currentglamid = glamid;
            EditWindow.IsOpen = true;
            EditWindow.Note = Notes[currentglamid];
        }



        private void OpenInventoryContextMenuOverride(InventoryContextMenuOpenArgs args)
        {
            args.AddCustomItem(Notes.TryGetValue(glamid, out none) ? inventoryContextMenuItem2 : inventoryContextMenuItem);
            
        }
        public void OnItemTooltipOverride(ItemTooltip itemTooltip, ulong itemid)
        {
            string value = "";
            id = itemid;
            // PluginLog.Debug($"{glamid}");
            ItemTooltipString itemTooltipString;
            ItemTooltipString itemTooltipString1;
            
            if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Description))
            {
                itemTooltipString = ItemTooltipString.Description;
                itemTooltipString1 = ItemTooltipString.GlamourName;
                glam = "";
            }
            else if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Levels))
            {
                itemTooltipString = ItemTooltipString.EquipLevel;
                itemTooltipString1 = ItemTooltipString.GlamourName;
                 
                 
                
            }
            else if (itemTooltip.Fields.HasFlag(ItemTooltipFields.Effects))
            {
                itemTooltipString = ItemTooltipString.Effects;
                itemTooltipString1 = ItemTooltipString.GlamourName;
                glam = "";
            }
            else
            {
                return;
            }
            glam = itemTooltip[itemTooltipString1];
            glamid = string.Format("{0}{1}", glam, id.ToString());
            if (glamid.Length > 8)
            {
                glamid = glamid.Remove(0, 2);
            }
            var description = itemTooltip[itemTooltipString];
            
            if (Notes.TryGetValue(glamid, out value) && value != "")
            { 
                description = description.Append($"\n\nNote: \n");
                description = description.Append(value);
                description = description.Append($"\n");
                PluginLog.Debug($"Note should say {value}");
                
            }
            itemTooltip[itemTooltipString] = description;
        }
    }
}
