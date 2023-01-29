using Dalamud.Logging;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Plugin;
using Dalamud.Configuration;
using Dalamud.Game.Text.SeStringHandling;

namespace NotesPlugin
{
    public class Notes
    {
        private DalamudPluginInterface pluginInterface;

        public class Markup
        {
            public ushort ColorKey = ushort.MaxValue;
            public bool Glow = false;
        }

        public class Label
        {
            public string Name = "";
            public bool ShowInMenu = false;
            public Markup Markup = new();
        }

        public class Note
        {
            public string Text = "";
            public Markup Markup = new();
            public List<string> Labels = new();
        }

        private class Data : IPluginConfiguration
        {
            public int Version { get; set; } = 0;

            public bool CharacterSpecific = true;
            public bool GlamourSpecific = true;
            public bool EnableStyles = false;
            public Dictionary<string, Label> Labels = new();
            public readonly Dictionary<string, Note> Notes = new();
        }

        private readonly Data data = new();

        public bool CharacterSpecific { get => data.CharacterSpecific; set => data.CharacterSpecific = value; }
        public bool GlamourSpecific { get => data.GlamourSpecific; set => data.GlamourSpecific = value; }
        public bool EnableStyles { get => data.EnableStyles; set => data.EnableStyles = value; }
        public Dictionary<string, Label> Labels
        {
            get => data.Labels;
            set
            {
                data.Labels = value;
                Save();
            }
        }

        public Notes(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            try
            {
                var pluginConfig = pluginInterface.GetPluginConfig();
                if (pluginConfig is Data d)
                    data = d;
                PluginLog.Debug("Configuration loaded successfully!");
            }
            catch
            {
                PluginLog.Error("Configuration could not be loaded");
            }
        }

        public void Save()
        {
            try
            {
                pluginInterface.SavePluginConfig(data);
                PluginLog.Debug("Configuration saved successfully!");
            }
            catch
            {
                PluginLog.Error("Configuration could not be saved");
            }
        }

        public string this[string noteKey]
        {
            get
            {
                var note = data.Notes[noteKey];
                return note.Text;
            }
            set
            {
                data.Notes[noteKey] = new Note
                {
                    Text = value,
                };
                Save();
            }
        }

        public bool ContainsKey(string notekey)
        {
            return data.Notes.ContainsKey(notekey);
        }

        public bool TryGetValue(string notekey, [MaybeNullWhen(false)] out string value)
        {
            if (!data.Notes.TryGetValue(notekey, out var note))
            {
                value = null;
                return false;
            }

            value = note.Text;
            return true;
        }

        public bool Remove(string noteKey)
        {
            var removed = data.Notes.Remove(noteKey);
            Save();
            return removed;
        }
    }
}
