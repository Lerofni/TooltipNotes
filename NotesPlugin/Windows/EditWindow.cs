using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotesPlugin.Windows;

public class EditWindow : Window, IDisposable
{
    private TextureWrap GoatImage;
    private Plugin Plugin;

    public static string Note = string.Empty;
    private string filepath = "C:/Users/Marvin/RiderProjects/NotesPlugin/NotesPlugin/bin/x64/Debug/Notes.json";

    public EditWindow(Plugin plugin) : base(
        "Edit Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(250, 110),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        this.Plugin = plugin;
        
    }

    public void Dispose()
    {
        this.GoatImage.Dispose();
    }

    public void close()
    {
        Plugin.WindowSystem.GetWindow("Edit Window").IsOpen = false;
    }

    

    public override void Draw()
    {
        
        ImGui.InputText($"Note",ref Note,1000);
        // ImGui.Text($"The current Note is {Note}");
        if (ImGui.Button("Enter Note"))
        {
            Plugin.Notes[Plugin.currentID] = Note;
            string jsonstring = JsonSerializer.Serialize(Plugin.Notes);
            File.WriteAllText(filepath, jsonstring);
            Note = $"";
            close();
        }
    }
}
