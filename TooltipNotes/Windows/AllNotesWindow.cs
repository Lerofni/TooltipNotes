using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace NotesPlugin.Windows;

public class AllNotesWindow : Window, IDisposable
{
    private readonly Config config;

    private class LabelState
    {
        public readonly string Name;
        public bool Checked;

        public LabelState(string name, bool @checked)
        {
            Name = name;
            Checked = @checked;
        }
    }

    // UI state
    private List<LabelState> labels = new();
    private readonly Dictionary<string, Config.Note> notes;
    private readonly ExcelSheet<Item>? itemSheet;
    private Dictionary<string, List<LabelState>>? labeldic;





    public AllNotesWindow(Config config) : base(
        "All notes in one Window")
    {
        itemSheet = Plugin.DataManager?.Excel.GetSheet<Item>();
        this.config = config;
        notes = config.NoteDict();
        Flags = ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.AlwaysVerticalScrollbar;
        

    }

    public void Dispose()
    {
    }

    public override void OnOpen()
    {
        labeldic = new();
        foreach (var notepair in notes)
        {
            var noteLabels = new HashSet<String>();
            var input = notepair.Key;
            foreach (var label in notepair.Value.Labels)
            {
                noteLabels.Add(label);
            }
            labels = new List<LabelState>();
           
            
                foreach (var label in config.Labels.Values)
                {
                    var noteHasLabel = noteLabels.Contains(label.Name);
                    labels.Add(new LabelState(label.Name, noteHasLabel));
                }
                labeldic.Add(input,labels);
        }
    }


    public override void Draw()
    {
        foreach (var notepair in notes)
        {
                uint itemId = 0;
               
                var input = notepair.Key;
                var itemIdString = Regex.Replace(input, @"(.*\D)","");
                if (itemIdString != "")
                {
                    itemId = Convert.ToUInt32(itemIdString);
                    if (itemId > 1000000)
                    {
                        itemId -= 1000000;
                    } 
                }
                var itemName = itemSheet?.GetRow(itemId)?.Name.ToString();
                //  is the glamour icon ingame 
                if (Regex.IsMatch(input, @"") && config.GlamourNote == 0)
                {
                    itemName += " ";
                } else if (Regex.IsMatch(input, @"") && config.GlamourNote == 1)
                {
                    itemName += " (GN)";
                }
                //  is the level sync icon ingame
                if (Regex.IsMatch(input, @"-") && config.CharacterNote == 0)
                {
                    itemName += " ";
                } else if (Regex.IsMatch(input, @"-") && config.CharacterNote == 1)
                {
                    itemName += " (CN)";
                }
                ImGui.InputText($"##{input}", ref itemName,10000, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.ReadOnly);
                ImGui.SameLine();
                ImGui.InputText($"##{input+itemId}", ref notepair.Value.Text, 100000,  ImGuiInputTextFlags.EnterReturnsTrue| ImGuiInputTextFlags.None);
                if (config.EnableStyles)
                {
                    ImGui.SameLine();
                    ConfigWindow.StyleButton("Colors", $"##Color{input}", ref notepair.Value.Markup, new(), "Custom colors for this note alone");
                }
                if (labeldic![input].Count > 0)
                {
                    foreach ( var label in labeldic[input])
                    {
                        var labelConfig = config.Labels[label.Name];
                        if (!labelConfig.HideLabel)
                        {
                            ImGui.SameLine();
                            ImGui.Checkbox($"{label.Name}##{input}", ref label.Checked);    
                        }
                    }
                }
        }

        bool saveAndQuit = ImGui.Button("Save&Quit##allNoteWindow");
        ImGui.SameLine();
        bool save = ImGui.Button("Save##allNoteWindow");
        if (save || saveAndQuit)
        {
            foreach (var notepair in notes)
            {
                var note = notepair.Value;
                var key = notepair.Key;
                if (!string.IsNullOrEmpty(note.Text) || labeldic![key].Any(label => label.Checked))
                {
                    note.Labels = new();
                    foreach (var label in labeldic![key])
                    {
                        if (label.Checked)
                        {
                            note.Labels.Add(label.Name);
                        }
                    }
                    config[key] = note;
                }
                else
                {
                    config.Remove(key);
                }
                
                
            }
            config.Save();
            if (saveAndQuit)
            {
                IsOpen = false;  
            }
            
        }
    }
}
