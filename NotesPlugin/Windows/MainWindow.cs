using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotesPlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private TextureWrap GoatImage;
    private Plugin Plugin;

    public string Note = string.Empty;
    private string filepath = "C:/Users/Marvin/RiderProjects/NotesPlugin/NotesPlugin/bin/x64/Debug/Notes.json";
     

    public MainWindow(Plugin plugin) : base(
        "Note Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
        Plugin.WindowSystem.GetWindow("Note Window").IsOpen = false;
    }

    

    public override void Draw()
    { 
        ImGui.InputText($"Note",ref this.Note,1000);
        // ImGui.Text($"The current id is {path}");
        if (ImGui.Button("Enter Note"))
        {
            Plugin.Notes.Add(Plugin.currentID,Note);
            string jsonstring = JsonSerializer.Serialize(Plugin.Notes);
            File.WriteAllText(filepath, jsonstring);
            Note = $"";
            close();
        }
    }
}
