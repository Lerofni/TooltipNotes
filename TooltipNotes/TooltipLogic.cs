using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Component.GUI;
using NotesPlugin.Windows;

namespace NotesPlugin;

public class TooltipLogic : Hook
{
    private Config config;
    private ItemNote itemNote;


    // Logic mirroring the old Logic in TooltipNotes.cs goes here

    public override unsafe void OnGenerateItemTooltip(
        NumberArrayData* numberArrayData,
        StringArrayData* stringArrayData)
    {
        var itemid = Plugin.GameGui!.HoveredItem;
        var EnableDebug = config.EnableDebug;
        var GlamourName = GetTooltipString(stringArrayData, ItemTooltipField.GlamourName);
        ItemTooltipField field;
        var appendNote = true;
        
        var tooltipVisibility = GetTooltipVisibility((int**)numberArrayData);
        if (tooltipVisibility.HasFlag(TooltipFlags.Description))
        {
            appendNote = false;
            field = ItemTooltipField.ItemDescription;
            if (!tooltipVisibility.HasFlag(TooltipFlags.Levels))
            {
                GlamourName = "";
            }
        }
        else if (tooltipVisibility.HasFlag(TooltipFlags.Levels))
        {
            field = ItemTooltipField.Levels;
        }
        else if (tooltipVisibility.HasFlag(TooltipFlags.Effects))
        {
            field = ItemTooltipField.Effects;
            GlamourName = "";
        }
        else
        {
            return;
        }

        if (!config.QualitySpecific && itemid >= 1000000)
        {
            itemid -= 1000000;
            Plugin.PluginLog?.Debug($"Itemid: {itemid}");
        }
        Plugin.lastNoteKey = itemid.ToString();
        if (GlamourName != null && config.GlamourSpecific && GlamourName.TextValue.Length > 0)
        {
            Plugin.lastNoteKey = $"{GlamourName}" + Plugin.lastNoteKey;
        }

        if (config.CharacterSpecific)
        {
            Plugin.lastNoteKey = $"{Plugin.ClientState!.LocalContentId:X16}-" + Plugin.lastNoteKey;
        }


        if (EnableDebug)
        {
            Plugin.PluginLog?.Debug($"NoteId: {Plugin.lastNoteKey}");
        }

        if (itemNote.TryGetValue(Plugin.lastNoteKey, out var note) || itemNote.TryGetValue(itemid.ToString(), out note))
        {
            var originalData = GetTooltipString(stringArrayData,field);
            var description = new SeStringBuilder();

            // If we append the note to the end of the field, add the original data first

            if (appendNote)
            {
                if (originalData != null) description.Append(originalData);
                description.Append("\n\n");
            }

            // Thanks to NotNite from the Discord for the help!
            // Color table: https://i.imgur.com/cZceCI3.png
            // Data (the 'key' is the 'colorKey' parameter)
            // https://github.com/xivapi/ffxiv-datamining/blob/master/csv/UIColor.csv
            // Using AddUiForegroundOff doesn't work because the whole cell is colored

            void AppendMarkup(Config.Markup markup, string text, Config.Markup fallbackMarkup)
            {
                if (markup.ColorKey == 0 && markup.GlowColorKey == 0)
                    markup = fallbackMarkup;

                var foregroundColor = markup.ColorKey;
                var foregroundAlpha = ConfigWindow.ForegroundColors.Find(c => c.Index == foregroundColor)?.A ?? 0;
                if (foregroundAlpha == 0)
                {
                    foregroundColor = fallbackMarkup.ColorKey;
                }

                description.AddUiForeground(foregroundColor);

                var glowColor = markup.GlowColorKey;
                var glowAlpha = ConfigWindow.ForegroundColors.Find(c => c.Index == glowColor)?.A ?? 0;
                description.AddUiGlow(glowColor);

                description.Append(text);

                description.AddUiGlowOff();

                description.AddUiForegroundOff();
            }

            if (note.Text.Length > 0)
            {
                if (config.NotePrefix)
                {
                    AppendMarkup(config.NotePrefixMarkup, "Note: ", Config.Markup.DefaultNotePrefix);
                }

                var noteMarkup = config.EnableStyles ? note.Markup : new();
                AppendMarkup(noteMarkup, note.Text, config.NoteMarkup);
                if (EnableDebug)
                {
                    Plugin.PluginLog?.Debug($"Note should be: {note.Text}");
                }

            }

            var hidePrevious = false;
            var labelSet = false;
            for (var i = 0; i < note.Labels.Count; i++)
            {
                var label = note.Labels[i];
                var labelConf = itemNote.Labels[label];
                var labelHide = labelConf.HideLabel;
                var labelMarkup = new Config.Markup();


                if (config.EnableStyles && itemNote.Labels.TryGetValue(label, out var labelConfig))
                {
                    labelMarkup = labelConfig.Markup;
                }

                if (i == 0)
                {
                    if (note.Text.Length > 0)
                    {
                        description.Append("\n");
                    }

                    if (config.LabelPrefix && !labelHide)
                    {
                        AppendMarkup(config.LabelPrefixMarkup, "Labels: ", Config.Markup.DefaultLabelPrefix);
                        labelSet = true;
                    }
                }
                else
                {
                    if (hidePrevious)
                    {
                        if (!labelSet)
                        {
                            AppendMarkup(config.LabelPrefixMarkup, "Labels: ", Config.Markup.DefaultLabelPrefix);
                            labelSet = true;
                        }

                        hidePrevious = false;

                    }
                    else
                    {
                        AppendMarkup(config.LabelMarkup, ", ", Config.Markup.DefaultLabel);
                    }

                }

                if (labelHide)
                {

                    hidePrevious = true;

                }
                else
                {
                    AppendMarkup(labelMarkup, label, config.LabelMarkup);
                    if (EnableDebug)
                    {
                        Plugin.PluginLog?.Debug($"Label: {label}");
                    }

                }

            }

            // If we prepend the note, add some newlines before the original data
            if (!appendNote)
            {
                description.Append("\n\n");
                if (originalData != null) description.Append(originalData);
            }

            // Modify the tooltip
            SetTooltipString(stringArrayData, field, description.Build());
            
        }
    

    }

    public TooltipLogic(Config config, ItemNote itemNote)
    {
        this.config = config;
        this.itemNote = itemNote;
    }
}

