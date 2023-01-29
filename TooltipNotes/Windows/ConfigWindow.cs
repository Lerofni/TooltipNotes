using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Logging;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace NotesPlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Notes notes;

    // Config state
    private bool characterSpecific;
    private bool glamourSpecific;
    private bool enableStyles;
    private Notes.Markup prefixMarkup = new();
    private Notes.Markup defaultMarkup = new();
    private List<Notes.Label> labels = new();

    // Internal helper state
    private int focusLabelIndex = -1;
    private string errorMessage = "";

    public ConfigWindow(string pluginName, Notes notes) : base(
        $"{pluginName} Config", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.notes = notes;
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    public void Dispose()
    {
    }

    public override void OnOpen()
    {
        characterSpecific = notes.CharacterSpecific;
        glamourSpecific = notes.GlamourSpecific;
        enableStyles = notes.EnableStyles;
        prefixMarkup = Notes.DeepClone(notes.PrefixMarkup);
        defaultMarkup = Notes.DeepClone(notes.DefaultMarkup);

        try
        {
            labels = Notes.DeepClone(notes.Labels.Values.Where(l => l.Name.Length > 0).ToList());
        }
        catch (NullReferenceException)
        {
        }
        labels.Add(new Notes.Label());

        focusLabelIndex = labels.Count - 1;
        errorMessage = "";
    }

    public static void AddUnderLine(Vector4 color)
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        min.Y = max.Y;
        uint col = 0;
        col |= (uint)(color.X * 255);
        col |= (uint)(color.Y * 255) << 8;
        col |= (uint)(color.Z * 255) << 16;
        col |= (uint)(color.W * 255) << 24;
        ImGui.GetWindowDrawList().AddLine(min, max, col, 1.0f);
    }

    public static void OpenURL(string url)
    {
        new System.Threading.Thread(() =>
        {
            try
            {

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = url,
                });
            }
            catch
            {
            }
        }).Start();
    }

    public static bool MarkupUI(string id, ref Notes.Markup markup, Notes.Markup defaultMarkup)
    {
        ImGui.Checkbox("Glow", ref markup.Glow);
        int colorKey = markup.ColorKey;
        ImGui.PushItemWidth(100);
        if (ImGui.InputInt($"##{id}", ref colorKey))
            markup.ColorKey = (ushort)(colorKey & 0xFFFF);
        ImGui.PopItemWidth();

        ImGui.SameLine();
        var x = ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight];
        ImGui.PushStyleColor(ImGuiCol.Text, x);
        ImGui.Text("Color Key");
        if (ImGui.IsItemHovered())
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                OpenURL("https://i.imgur.com/cZceCI3.png");
            }
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            AddUnderLine(x);
        }
        else
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
        }
        ImGui.PopStyleColor();

        if (ImGui.Button("Default"))
        {
            markup.ColorKey = defaultMarkup.ColorKey;
            markup.Glow = defaultMarkup.Glow;
            PluginLog.Debug($"Default: {markup.ColorKey}:{markup.Glow}");
        }

        return true;
    }

    public void StyleButton(string label, string id, ref Notes.Markup markup, Notes.Markup defaultMarkup)
    {
        var popupId = $"popup{id}";
        if (ImGui.Button($"{label}##{id}"))
            ImGui.OpenPopup(popupId);

        if (ImGui.BeginPopup(popupId))
        {
            MarkupUI($"markup{id}", ref markup, defaultMarkup);
            ImGui.EndPopup();
        }
    }

    public override void Draw()
    {
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            IsOpen = false;
            return;
        }

        ImGui.Checkbox("Character-specific notes", ref characterSpecific);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Changing this will hide your existing notes!");
        }

        ImGui.Checkbox("Glamour-specific notes", ref glamourSpecific);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Changing this might hide some existing notes!");
        }

        ImGui.Checkbox("Enable styles", ref enableStyles);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Enable label/note styling (experimental)");
        }

        if (enableStyles)
        {
            ImGui.SameLine();
            StyleButton("Prefix", "prefix", ref prefixMarkup, Notes.Markup.DefaultPrefix);
            ImGui.SameLine();
            StyleButton("Note", "note", ref defaultMarkup, Notes.Markup.DefaultNote);
        }

        ImGui.Separator();
        ImGui.Text("Labels:");

        // Prevent invalid focus which causes bugs later
        if (focusLabelIndex > labels.Count)
            focusLabelIndex = -1;

        var labelButtonSize = new Vector2(45, 23);
        for (var i = 0; i < labels.Count; i++)
        {
            ImGui.PushItemWidth(150);

            var inputFlags = ImGuiInputTextFlags.EnterReturnsTrue;
            if (i == focusLabelIndex)
            {
                inputFlags |= ImGuiInputTextFlags.AutoSelectAll;
                ImGui.SetKeyboardFocusHere();
                focusLabelIndex = -1;
            }

            var enterPressed = ImGui.InputText($"##labelInput{i}", ref labels[i].Name, 50, inputFlags);

            ImGui.SameLine();
            ImGui.Checkbox($"Menu##menu{i}", ref labels[i].ShowInMenu);
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Add this label to the item menu");
            }

            if (enableStyles)
            {
                ImGui.SameLine();
                StyleButton("Style", $"style{i}", ref labels[i].Markup, new());
            }

            ImGui.SameLine();
            var lastLabel = i + 1 == labels.Count;
            if (lastLabel)
            {
                var addClicked = ImGui.Button($"Add##labelAdd{i}", labelButtonSize);
                if (addClicked || enterPressed)
                {
                    focusLabelIndex = labels.Count;
                    labels.Add(new Notes.Label());
                }
            }
            else
            {
                if (ImGui.Button($"Delete##labelDelete{i}", labelButtonSize))
                {
                    labels.RemoveAt(i);
                    return;
                }
                else if (enterPressed)
                {
                    // Select the next label field
                    focusLabelIndex = i + 1;
                }
            }

            ImGui.PopItemWidth();
        }
        var saveClicked = ImGui.Button("Save");

        if (saveClicked)
        {
            try
            {
                var nonEmptyLabels = labels.Where(l => l.Name.Length > 0);

                // Make sure no duplicate labels are passed
                var labelsDict = new Dictionary<string, Notes.Label>();
                foreach (var label in nonEmptyLabels)
                {
                    if (!labelsDict.TryAdd(label.Name, label))
                        throw new ArgumentException($"Label '{label.Name}' is not unique!");
                }

                notes.Labels = labelsDict;
                notes.CharacterSpecific = characterSpecific;
                notes.GlamourSpecific = glamourSpecific;
                notes.EnableStyles = enableStyles;
                notes.PrefixMarkup = prefixMarkup;
                notes.DefaultMarkup = defaultMarkup;
                notes.Save();
                IsOpen = false;
            }
            catch (Exception x)
            {
                errorMessage = x.Message;
            }
        }

        if (errorMessage.Length > 0)
        {
            ImGui.TextColored(new Vector4(255, 0, 0, 255), errorMessage);
        }
    }
}
