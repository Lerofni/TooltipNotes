using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Logging;

namespace NotesPlugin.Windows;

public class NoteWindow : Window, IDisposable
{
    private readonly Config config;
    private bool focusNoteField = false;
    private string noteKey = "";
    private Config.Note note = new();

    public NoteWindow(Config config) : base(
        "Item Note", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
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
        var enterPressed = ImGui.InputText("", ref note.Text, 1000, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);
        if (config.EnableStyles)
        {
            ImGui.SameLine();
            ConfigWindow.StyleButton("Style", "note", ref note.Markup, new());
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
                if (!string.IsNullOrEmpty(note.Text))
                {
                    config[noteKey] = note;
                }
                else
                {
                    config.Remove(noteKey);
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
        if (config.ContainsKey(noteKey))
        {
            note = Config.DeepClone(config[noteKey]);
        }
        else
        {
            note = new();
        }
    }
}
