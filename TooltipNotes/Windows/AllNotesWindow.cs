using System;
using System.Linq;
using System.Collections.Generic;

using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Logging;

namespace NotesPlugin.Windows;

public class AllNotesWindow : Window, IDisposable
{
    private readonly Config config;

    private class LabelState
    {
        public string Name;
        public bool Checked;

        public LabelState(string name, bool @checked)
        {
            Name = name;
            Checked = @checked;
        }
    }

    // UI state
    private bool focusNoteField = false;
    private string noteKey = "";
    private Config.Note note = new();
    private List<LabelState> labels = new();
    public Dictionary<string, NotesPlugin.Config.Note> Notes = new();
    
    



    public AllNotesWindow(Config config) : base(
        "All notes in one Window")
    {
        this.config = config;
        Notes = config.NoteDict();
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    public void Dispose()
    {
    }

    
    
    public override void Draw()
    {
        // thanks to MidoriKami from the Discord for the keyboard focus
        // Reference: https://github.com/KazWolfe/Mappy/blob/970e5ce6888d19dd9e2b9b6568c70cea71c4f059/Mappy/UserInterface/Components/MapSelect.cs#L46
        
        if (focusNoteField)
        {
            ImGui.SetKeyboardFocusHere();
            focusNoteField = false;
        }
        
        
        
        foreach (var notepair in Notes)
        {
                var noteLabels = new HashSet<string>();
                var keyValuePair = notepair;
                var input = keyValuePair.Key;
                foreach (var label in notepair.Value.Labels)
                {
                    noteLabels.Add(label);
                }
                labels = new();
                foreach (var label in config.Labels.Values)
                {
                    var noteHasLabel = noteLabels.Contains(label.Name);
                    labels.Add(new LabelState(label.Name, noteHasLabel));
                }
                ImGui.InputText($"##{notepair.Key}", ref input,100000, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);
                ImGui.SameLine();
                ImGui.InputText($"##{notepair.Value.Text}", ref notepair.Value.Text, 10000, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);
                if (config.EnableStyles)
                {
                    ImGui.SameLine();
                    ConfigWindow.StyleButton("Colors", $"##Color{input}", ref notepair.Value.Markup, new(), "Custom colors for this note alone");
                }
                if (labels.Count > 0)
                {
                    foreach (var label in labels)
                    {
                        Config.Label labelConfig;
                        labelConfig = config.Labels[label.Name];
                        if (!labelConfig.HideLabel)
                        {
                            ImGui.SameLine();
                            ImGui.Checkbox(label.Name, ref label.Checked);    
                        }
                        
                    }
                
                    
                }
        }
    }
}
