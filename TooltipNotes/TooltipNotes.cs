using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Configuration;
using Dalamud.Interface.Windowing;
using NotesPlugin.Windows;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace NotesPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "TooltipNotes";
        public string? configDirectory;
        private const string openconfig = "/tnconfig";
        private const string openallNote = "/tnallnotes";
        private const string newNote = "/tnnote";

    

      
        

     
        
        private WindowSystem windowSystem;

        private readonly NoteWindow noteWindow;
        private readonly ConfigWindow configWindow;
        private readonly AllNotesWindow allNotesWindow;
        
        [PluginService]
        public static ICommandManager? CommandManager { get; private set; }
        
        public readonly Config Config;
        public ItemNote itemNote;
        public static string lastNoteKey = "";
        

        
        [PluginService]
        public static IDalamudPluginInterface? PluginInterface { get; private set; }
        
        [PluginService]
      
        public static IClientState? ClientState { get; private set; }

        [PluginService]
        
        public static IDataManager? DataManager { get; private set; }

        

        [PluginService]
        public static IPluginLog? PluginLog { get; private set; }
        
        [PluginService]
        public static IGameInteropProvider? GameInteropProvider { get; private set; }
        
        [PluginService]
        public static IGameGui? GameGui { get; private set; }
        
        [PluginService]
        public static IContextMenu? ContextMenu{ get; private set; }

        private Hook hook;
        private Hook tooltipLogic;


        public Plugin()
        {
            configDirectory = PluginInterface?.GetPluginConfigDirectory()!;
            ConfigWindow.ForegroundColors.Clear();
            ConfigWindow.GlowColors.Clear();

            if (DataManager != null)
            {
                var colorSheet = DataManager.GetExcelSheet<UIColor>();
                if (colorSheet != null)
                {
                    for (var i = 0u; i < colorSheet.Count; i++)
                    {

                        if (colorSheet.TryGetRow(i, out var row))
                        {
                            ConfigWindow.ForegroundColors.Add(ConfigWindow.ColorInfo.FromUIColor((ushort)i, row.Dark));
                            ConfigWindow.GlowColors.Add(ConfigWindow.ColorInfo.FromUIColor((ushort)i, row.Light)); 
                            
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

            if (!File.Exists(configDirectory + "\\TooltipNotes.json.old") & File.Exists(configDirectory + "\\ItemNotes.json"))
            {
                itemNote = new ItemNote();
                itemNote.ConfigDirectory = configDirectory;
                ConvertNotes();
            }
            else if (!File.Exists(configDirectory + "\\ItemNotes.json"))
            {
                itemNote = new ItemNote();
                itemNote.ConfigDirectory = configDirectory;
            }
            else
            {
                itemNote = ItemNote.Load(configDirectory);
            }

            Config.PluginInterface = PluginInterface;

            windowSystem = new(Name);

            noteWindow = new NoteWindow(Config, itemNote);
            windowSystem.AddWindow(noteWindow);
            configWindow = new ConfigWindow(Name, Config, itemNote);
            windowSystem.AddWindow(configWindow);
            allNotesWindow = new AllNotesWindow(Config, itemNote);
            windowSystem.AddWindow(allNotesWindow);

            PluginInterface!.UiBuilder.Draw += windowSystem.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += () => configWindow.IsOpen = true;

            ContextMenu!.OnMenuOpened += OnMenuOpened;
            hook = new Hook();
            tooltipLogic = new TooltipLogic(Config, itemNote);
            hook.addList(tooltipLogic);
        }

        private void OnMenuOpened(IMenuOpenedArgs args)
        {
            if(!(args.Target is MenuTargetInventory { TargetItem: { } item })) return;
            if (item.IsEmpty) return;
            var itemdid = (args.Target as MenuTargetInventory)?.TargetItem?.ItemId ?? 0u;   
            
            if (itemNote.ContainsKey(lastNoteKey))
            {
              
                args.AddMenuItem(new()
                {
                    Name= "Edit Note", 
                    OnClicked =  this.EditNote(args),
                    PrefixChar = 'T',
                    PrefixColor = 579,
                    IsSubmenu = false
                    
                });
            }
            else
            {
                
                args.AddMenuItem(new()
                {
                    Name= "Add Note", 
                    OnClicked =  this.AddNote(args),
                    PrefixChar = 'T',
                    PrefixColor = 579,
                    IsSubmenu = false
                
                });
            }
            if (!noteWindow.IsOpen)
            {
                foreach (var label in itemNote.Labels.Values)
                {
                    if (label.ShowInMenu && !label.HideLabel)
                    {
                        var hasLabel = false;
                        if (itemNote.TryGetValue(lastNoteKey, out var note))
                        {
                            hasLabel = note.Labels.Contains(label.Name);
                        }
                        var name = new SeString(new TextPayload($"{(hasLabel ? "Unlabel" : "Label")}: {label.Name}"));
                        args.AddMenuItem(new MenuItem()
                        {
                            Name = name,
                            OnClicked = args =>
                            {
                                ItemNote.Note note1;
                                if (!itemNote.ContainsKey(lastNoteKey))
                                {
                                    note1 = new();
                                    itemNote[lastNoteKey] = note1;
                                }
                                else
                                {
                                    note1 = itemNote[lastNoteKey];
                                }
        
                                if (hasLabel)
                                {
                                    note1.Labels.Remove(label.Name);
                                    if (note1.Labels.Count == 0 && note1.Text.Length == 0)
                                        itemNote.Remove(lastNoteKey);
                                }
                                else
                                {
                                    note1.Labels.Add(label.Name);
                                }
                                itemNote.Save();
                            },
                            PrefixChar = 'T',
                            PrefixColor = 579,
                            IsSubmenu = false
                            
                        });
                    }
                }
            }
            
        }

        public void Dispose()
        {
            windowSystem.RemoveAllWindows();
            noteWindow.Dispose();
            configWindow.Dispose();
            allNotesWindow.Dispose();
            // contextMenuBase.OnOpenInventoryContextMenu -= OpenInventoryContextMenuOverride;
            // contextMenuBase.Dispose();
            ContextMenu!.OnMenuOpened -= OnMenuOpened;
            tooltipLogic.Dispose();
            hook.Dispose();
            CommandManager?.RemoveHandler(openconfig);
            CommandManager?.RemoveHandler(openallNote);
            CommandManager?.RemoveHandler(newNote);
        }

        public Action<IMenuItemClickedArgs>? AddNote(IMenuOpenedArgs args)
        {
            return clickedArgs => 
            noteWindow.Edit(lastNoteKey);
        }

        public Action<IMenuItemClickedArgs>? EditNote(IMenuOpenedArgs args)
        {
            return clickedArgs => 
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
        


        // private InventoryContextMenuItem createLabelContextMenuItem(string label)
        // {
        //     var hasLabel = false;
        //     if (itemNote.TryGetValue(lastNoteKey, out var note))
        //     {
        //         hasLabel = note.Labels.Contains(label);
        //     }
        //     var name = new SeString(new TextPayload($"{(hasLabel ? "Unlabel" : "Label")}: {label}"));
        //     return new InventoryContextMenuItem(name, args =>
        //     {
        //         ItemNote.Note note1;
        //         if (!itemNote.ContainsKey(lastNoteKey))
        //         {
        //             note1 = new();
        //             itemNote[lastNoteKey] = note1;
        //         }
        //         else
        //         {
        //             note1 = itemNote[lastNoteKey];
        //         }
        //
        //         if (hasLabel)
        //         {
        //             note1.Labels.Remove(label);
        //             if (note1.Labels.Count == 0 && note1.Text.Length == 0)
        //                 itemNote.Remove(lastNoteKey);
        //         }
        //         else
        //         {
        //             note1.Labels.Add(label);
        //         }
        //         itemNote.Save();
        //     }, true);
        // }
        //
        // private void OpenInventoryContextMenuOverride(InventoryContextMenuOpenArgs args)
        // {
        //     args.AddCustomItem(itemNote.ContainsKey(lastNoteKey) ? inventoryContextMenuItem2 : inventoryContextMenuItem);
        //
        //     // Updating labels while editing does not work
        //     if (!noteWindow.IsOpen)
        //     {
        //         foreach (var label in itemNote.Labels.Values)
        //         {
        //             if (label.ShowInMenu && !label.HideLabel)
        //             {
        //                 args.AddCustomItem(createLabelContextMenuItem(label.Name));
        //             }
        //         }
        //     }
        // }
        // convert notes from Config to ItemNote
        public void ConvertNotes()
        {
            File.Move(configDirectory + ".json", configDirectory + "\\TooltipNotes.json.old");
            Config.Save();
            foreach (var note in Config.Notes)
                {
                    var itemNoteNote = new ItemNote.Note();
                    itemNoteNote.Text = note.Value.Text;
                    itemNoteNote.Markup = note.Value.Markup;
                    itemNoteNote.Labels = note.Value.Labels;
                    itemNote.Notes[note.Key] = itemNoteNote;
                    Config.Notes.Remove(note.Key);
                }
                foreach (var label in Config.Labels)
                {
                    var itemNoteLabel = new ItemNote.Label();
                    itemNoteLabel.Name = label.Value.Name;
                    itemNoteLabel.ShowInMenu = label.Value.ShowInMenu;
                    itemNoteLabel.HideLabel = label.Value.HideLabel;
                    itemNoteLabel.Markup = label.Value.Markup;
                    itemNote.Labels[label.Key] = itemNoteLabel;
                    Config.Labels.Remove(label.Key);
                }
                itemNote.Save();
                Config.Save();
                
        }
        
    }
}
