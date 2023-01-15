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

public class MainWindow : Window, IDisposable
{
    
    private Plugin Plugin;
    private String Filepath;

    public string Note = string.Empty;
    
     

    public MainWindow(Plugin plugin, String filepath) : base(
        "Note Window", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(250, 110),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        
        this.Plugin = plugin;
        this.Filepath = filepath;
    }

    public void Dispose()
    {
        
    }

    public void close()
    {
        Plugin.WindowSystem.GetWindow("Note Window").IsOpen = false;
    }

    

    public override void Draw()
    { 
        ImGui.InputText($"Note",ref this.Note,1000);
        // ImGui.Text($"The current path is {Filepath}");
        if (ImGui.Button("Enter Note"))
        {
            Plugin.Notes.Add(Plugin.currentID,Note);
            string jsonstring = JsonSerializer.Serialize(Plugin.Notes);
            File.WriteAllText(Filepath, jsonstring);
            PluginLog.Debug("Notes successfully added ");
            Note = $"";
            close();
        }
    }
}
