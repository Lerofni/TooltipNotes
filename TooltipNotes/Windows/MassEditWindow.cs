using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Logging;

namespace NotesPlugin.Windows;

public class MassEditWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private Dictionary<string, string> NoteData = new Dictionary<string, string>();


    public MassEditWindow(Plugin plugin) : base(
        "All Notes", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
        // NoteData = Notes.Data();
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
        ImGui.Text("{Notes.Data}");

    }

    
}
