using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Logging;
namespace NotesPlugin.Windows;

public class NoteWindow : Window, IDisposable
{
    private readonly Config config;
    private ItemNote itemNote;

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
    private ItemNote.Note note = new();
    private List<LabelState> labels = new();
    

    public NoteWindow(Config config,ItemNote itemNote) : base(
        "Item Note", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.itemNote = itemNote;
        this.config = config;
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

        ImGui.PushItemWidth(350);
        var enterPressed = ImGui.InputTextMultiline("", ref note.Text, 1000, new Vector2(350,125),ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CtrlEnterForNewLine);
        if (config.EnableStyles)
        {
            ImGui.SameLine();
            ConfigWindow.StyleButton("Colors", "note", ref note.Markup, new(), "Custom colors for this note alone");
        }

        if (labels.Count > 0)
        {
            foreach (var label in labels)
            {
                ItemNote.Label labelConfig;
                labelConfig = itemNote.Labels[label.Name];
                if (!labelConfig.HideLabel)
                {
                    ImGui.Checkbox(label.Name, ref label.Checked);    
                }
               
            }

            
        }

        // Check if the user pressed ESC
        // https://github.com/ocornut/imgui/issues/2620#issuecomment-501136289
        if (ImGui.IsItemDeactivated() && ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            IsOpen = false;
        }
        else
        {
            if (ImGui.Button("Save") || enterPressed)
            {
                if (!string.IsNullOrEmpty(note.Text) || labels.Count(label => label.Checked) > 0)
                {
                    note.Labels = new();
                    foreach (var label in labels)
                    {
                        if (label.Checked)
                        {
                            note.Labels.Add(label.Name);
                        }
                    }
                    itemNote[noteKey] = note;
                }
                else
                {
                    itemNote.Remove(noteKey);
                }
                IsOpen = false;

                // TODO: trigger a tooltip refresh for a better controller experience
            }
        }
    }

    public void Edit(string noteKey)
    {
        IsOpen = true;
        focusNoteField = true;

        this.noteKey = noteKey;
        if (itemNote.ContainsKey(noteKey))
        {
            note = ItemNote.DeepClone(itemNote[noteKey]);
        }
        else
        {
            note = new();
        }

        var noteLabels = new HashSet<string>();
        foreach (var label in note.Labels)
        {
            noteLabels.Add(label);
        }

        labels = new();
        foreach (var label in itemNote.Labels.Values)
        {
            var noteHasLabel = noteLabels.Contains(label.Name);
            labels.Add(new LabelState(label.Name, noteHasLabel));
        }
    }
}
