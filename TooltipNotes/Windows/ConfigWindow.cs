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
    private readonly Config config;

    // Config state
    private bool characterSpecific;
    private bool glamourSpecific;
    private bool enableStyles;
    private bool notePrefix;
    private Config.Markup notePrefixMarkup = new();
    private Config.Markup noteMarkup = new();
    private bool labelPrefix;
    private Config.Markup labelPrefixMarkup = new();
    private Config.Markup labelMarkup = new();
    private List<Config.Label> labels = new();

    // Internal helper state
    private int focusLabelIndex = -1;
    private string errorMessage = "";

    public ConfigWindow(string pluginName, Config config) : base(
        $"{pluginName} Config", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.config = config;
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    public void Dispose()
    {
    }

    public override void OnOpen()
    {
        characterSpecific = config.CharacterSpecific;
        glamourSpecific = config.GlamourSpecific;
        enableStyles = config.EnableStyles;
        notePrefix = config.NotePrefix;
        notePrefixMarkup = Config.DeepClone(config.NotePrefixMarkup);
        noteMarkup = Config.DeepClone(config.NoteMarkup);
        labelPrefix = config.LabelPrefix;
        labelPrefixMarkup = config.LabelPrefixMarkup;
        labelMarkup = config.LabelMarkup;

        try
        {
            labels = Config.DeepClone(config.Labels.Values.Where(l => l.Name.Length > 0).ToList());
        }
        catch (NullReferenceException)
        {
        }
        labels.Add(new Config.Label());

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

    public static bool MarkupUI(string id, ref Config.Markup markup, Config.Markup defaultMarkup)
    {
        ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);

        void Hyperlink(string text, string url)
        {
            var navColor = ImGui.GetStyle().Colors[(int)ImGuiCol.NavHighlight];
            ImGui.PushStyleColor(ImGuiCol.Text, navColor);
            ImGui.Text(text);
            if (ImGui.IsItemHovered())
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    OpenURL(url);
                }
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                AddUnderLine(navColor);
            }
            ImGui.PopStyleColor();
        }

        int colorKey = markup.ColorKey;
        ImGui.PushItemWidth(100);
        if (ImGui.InputInt($"##color{id}", ref colorKey))
            markup.ColorKey = (ushort)colorKey;
        ImGui.PopItemWidth();

        ImGui.SameLine();
        Hyperlink("Color", "https://i.imgur.com/cZceCI3.png");

        ImGui.PushItemWidth(100);
        int glowColorKey = markup.GlowColorKey;
        if (ImGui.InputInt($"##glow{id}", ref glowColorKey))
            markup.GlowColorKey = (ushort)glowColorKey;
        ImGui.PopItemWidth();

        ImGui.SameLine();
        Hyperlink("Glow", "https://i.imgur.com/cZceCI3.png");

        if (ImGui.Button("Default"))
        {
            markup.ColorKey = defaultMarkup.ColorKey;
            markup.GlowColorKey = defaultMarkup.GlowColorKey;
            PluginLog.Debug($"color: {markup.ColorKey}, glow: {markup.GlowColorKey}");
        }

        return true;
    }

    public static void StyleButton(string label, string id, ref Config.Markup markup, Config.Markup defaultMarkup, string tooltip = "")
    {
        var popupId = $"popup{id}";
        if (ImGui.Button($"{label}##{id}"))
            ImGui.OpenPopup(popupId);

        if (tooltip.Length > 0 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(tooltip);
        }

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
            ImGui.SetTooltip("Enable custom label/note styling (experimental)");
        }

        if (enableStyles)
        {
            ImGui.SameLine();
            StyleButton("Note", "note", ref noteMarkup, Config.Markup.DefaultNote, "Default note style");

            ImGui.SameLine();
            StyleButton("Label", "label", ref labelMarkup, Config.Markup.DefaultLabel, "Default label style");
        }

        ImGui.Checkbox("Note prefix", ref notePrefix);
        if (notePrefix && enableStyles)
        {
            ImGui.SameLine();
            StyleButton("Style", "notePrefix", ref notePrefixMarkup, Config.Markup.DefaultNotePrefix, "Default note prefix style");
        }

        ImGui.Checkbox("Label prefix", ref labelPrefix);
        if (labelPrefix && enableStyles)
        {
            ImGui.SameLine();
            StyleButton("Style", "labelPrefix", ref labelPrefixMarkup, Config.Markup.DefaultLabelPrefix, "Default label prefix style");
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
                    labels.Add(new Config.Label());
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
                var labelsDict = new Dictionary<string, Config.Label>();
                foreach (var label in nonEmptyLabels)
                {
                    if (!labelsDict.TryAdd(label.Name, label))
                        throw new ArgumentException($"Label '{label.Name}' is not unique!");
                }

                config.Labels = labelsDict;
                config.CharacterSpecific = characterSpecific;
                config.GlamourSpecific = glamourSpecific;
                config.EnableStyles = enableStyles;
                config.NotePrefix = notePrefix;
                if (enableStyles)
                {
                    config.NotePrefixMarkup = notePrefixMarkup;
                    config.NoteMarkup = noteMarkup;
                }
                else
                {
                    config.NotePrefixMarkup = Config.Markup.DefaultNotePrefix;
                    config.NoteMarkup = Config.Markup.DefaultLabel;
                }
                config.LabelPrefix = labelPrefix;
                if (enableStyles)
                {
                    config.LabelPrefixMarkup = labelPrefixMarkup;
                    config.LabelMarkup = labelMarkup;
                }
                else
                {
                    config.LabelPrefixMarkup = Config.Markup.DefaultLabelPrefix;
                    config.LabelMarkup = Config.Markup.DefaultLabel;
                }
                config.Save();
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