using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Text.Json;
using Dalamud.Plugin;
using Dalamud.Game.ClientState;
using Dalamud.IoC;
using Dalamud.Logging;

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
    private string oldpluginconfig = "";
    private ulong characterId = 0;

    public ConfigWindow(string pluginName, Config config, string oldpluginconfig, ulong characterId) : base(
        $"{pluginName} Config", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.config = config;
        this.oldpluginconfig = oldpluginconfig;
        this.characterId = characterId;
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

    public class ColorInfo
    {
        public ushort Index = ushort.MaxValue;
        public byte R = 0;
        public byte G = 0;
        public byte B = 0;
        public byte A = 0;

        public static ColorInfo FromUIColor(ushort index, uint foreground)
        {
            return new()
            {
                Index = index,
                R = (byte)((foreground >> 24) & 0xFF),
                G = (byte)((foreground >> 16) & 0xFF),
                B = (byte)((foreground >> 8) & 0xFF),
                A = (byte)((foreground >> 0) & 0xFF),
            };
        }

        public override string ToString()
        {
            return $"#{R:X2}{G:X2}{B:X2}:{Index}";
        }

        public Vector4 Vec4 => new Vector4(R / 255f, G / 255f, B / 255f, A / 255f);
    }

    public static List<ColorInfo> ForegroundColors = new();
    public static List<ColorInfo> GlowColors = new();

    public static bool MarkupUI(string id, ref Config.Markup markup, Config.Markup defaultMarkup)
    {
        void PalettePicker(string name, List<ColorInfo> palette, ref ushort index)
        {
            var paletteButtonFlags = ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip;
            ImGui.Separator();
            ImGui.BeginGroup(); // Lock X position
            ImGui.Text(name);
            ImGui.SameLine();
            var selectedColor = new ColorInfo();
            foreach (var color in palette)
            {
                if (color.Index == index)
                {
                    selectedColor = color;
                }
            }
            ImGui.ColorButton($"##selected{name}", selectedColor.Vec4, paletteButtonFlags, new Vector2(20, 20));

            for (var i = 0; i < palette.Count; i++)
            {
                ImGui.PushID(i);
                if ((i % 15) != 0)
                    ImGui.SameLine(0f, ImGui.GetStyle().ItemSpacing.Y);

                if (ImGui.ColorButton($"##palette{name}", palette[i].Vec4, paletteButtonFlags, new Vector2(20, 20)))
                {
                    index = palette[i].Index;
                }
                ImGui.PopID();
            }
            ImGui.EndGroup();
        }

        PalettePicker($"Color:", ForegroundColors, ref markup.ColorKey);
        PalettePicker($"Glow:", GlowColors, ref markup.GlowColorKey);

        var close = ImGui.Button("Close");
        ImGui.SameLine();
        if (ImGui.Button("Default"))
        {
            markup.ColorKey = defaultMarkup.ColorKey;
            markup.GlowColorKey = defaultMarkup.GlowColorKey;
        }

        return close;
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
            if (MarkupUI($"markup{id}", ref markup, defaultMarkup))
                ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    public void NoteConverter()
    {
        string oldjson = File.ReadAllText(oldpluginconfig);
        var oldNotesDict = JsonSerializer.Deserialize<Dictionary<string, string>>(oldjson);
        if (oldNotesDict == null)
        {
            PluginLog.Error($"Failed to deserialize: {oldpluginconfig}");
            return;
        }

        foreach (var i in oldNotesDict)
        {
            Config.Note note = new();
            var key = "";
            if (characterSpecific)
            {
                key = $"{characterId:X16}-";
            }
            key += i.Key;
            note.Text = i.Value;
            config[key] = note;
        }

        File.Move(oldpluginconfig, oldpluginconfig + ".old", true);
    }

    public override void Draw()
    {
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            IsOpen = false;
            return;
        }

        ImGui.Text("Settings:");
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

        ImGui.Separator();

        ImGui.Text("Custom Colors:");

        ImGui.Checkbox("Enable", ref enableStyles);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Enable custom note/label colors");
        }

        if (enableStyles)
        {
            StyleButton("Note Colors", "note", ref noteMarkup, Config.Markup.DefaultNote, "Default note text colors");
            ImGui.SameLine();
        }
        ImGui.Checkbox($"{(enableStyles ? "Prefix" : "Note prefix")}##notePrefix", ref notePrefix);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Show 'Note:' in front of notes");
        }
        if (notePrefix && enableStyles)
        {
            ImGui.SameLine();
            StyleButton("Prefix Colors", "notePrefix", ref notePrefixMarkup, Config.Markup.DefaultNotePrefix, "Colors for the 'Note:' prefix");
        }

        if (enableStyles)
        {
            StyleButton("Label Colors", "label", ref labelMarkup, Config.Markup.DefaultLabel, "Default label text colors");
            ImGui.SameLine();
        }
        ImGui.Checkbox($"{(enableStyles ? "Prefix" : "Label prefix")}##labelPrefix", ref labelPrefix);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip("Show 'Labels:' in front of labels");
        }
        if (labelPrefix && enableStyles)
        {
            ImGui.SameLine();
            StyleButton("Prefix Colors", "labelPrefix", ref labelPrefixMarkup, Config.Markup.DefaultLabelPrefix, "Colors for the 'Label:' prefix");
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

            var labelDescription = labels[i].Name.Length > 0 ? $"'{labels[i].Name}'" : "new";

            ImGui.SameLine();
            ImGui.Checkbox($"Menu##menu{i}", ref labels[i].ShowInMenu);
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip($"Add a menu for toggling the {labelDescription} label");
            }

            if (enableStyles)
            {
                ImGui.SameLine();
                var name = labels[i].Name;
                StyleButton("Colors", $"style{i}", ref labels[i].Markup, new(), $"Colors for the {labelDescription} label");
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

        ImGui.Separator();

        if (File.Exists(oldpluginconfig) && ImGui.Button("Migrate Old Notes to new system"))
        {
            NoteConverter();
        }

        ImGui.Separator();

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
