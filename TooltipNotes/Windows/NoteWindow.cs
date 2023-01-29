using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Logging;

namespace NotesPlugin.Windows;

public class NoteWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private bool focusNoteField = false;
    private string noteKey = "";
    private string text = "";

    public NoteWindow(Plugin plugin) : base(
        "Item Note", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    public void Dispose()
    {
    }

    public void Close()
    {
        var window = plugin.NoteWindow;
        if (window.IsOpen)
        {
            window.IsOpen = false;
        }
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
        var enterPressed = ImGui.InputText("", ref text, 1000, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);
        ImGui.PopItemWidth();

        // Check if the user pressed ESC
        // https://github.com/ocornut/imgui/issues/2620#issuecomment-501136289
        if (ImGui.IsItemDeactivated() && ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            Close();
        }
        else
        {
            if (ImGui.Button("Save") || enterPressed)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    plugin.Notes[noteKey] = text;
                }
                else
                {
                    plugin.Notes.Remove(noteKey);
                }
                text = "";
                Close();
            }
        }
    }

    public void Edit(string noteKey)
    {
        IsOpen = true;
        focusNoteField = true;

        this.noteKey = noteKey;
        if (plugin.Notes.ContainsKey(noteKey))
            text = plugin.Notes[noteKey];
    }
}
