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

    public static string Note = string.Empty;

    public NoteWindow(Plugin plugin) : base(
        "Item Note", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(250, 110),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose()
    {
    }

    public void close()
    {
        var window = plugin.WindowSystem.GetWindow(WindowName);
        if (window != null)
        {
            window.IsOpen = false;
        }
    }

    public override void Draw()
    {
        var enterPressed = ImGui.InputText("Note", ref Note, 1000, ImGuiInputTextFlags.EnterReturnsTrue);
        if (ImGui.Button("Save") || enterPressed)
        {
            if (!string.IsNullOrEmpty(Note))
            {
                plugin.Notes[plugin.EditingNoteKey] = Note;
            }
            else
            {
                plugin.Notes.Remove(plugin.EditingNoteKey);
            }
            Note = "";
            close();
        }
    }
}
