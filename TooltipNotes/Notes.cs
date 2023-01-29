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

            public static Markup DefaultPrefix => new()
            {
                ColorKey = 60,
                Glow = true,
            };

            public static Markup DefaultNote => new()
            {
                ColorKey = 1,
                Glow = false,
            };
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
            public Markup PrefixMarkup = Markup.DefaultPrefix;
            public Markup DefaultMarkup = Markup.DefaultNote;
            public Dictionary<string, Label> Labels = new();
            public readonly Dictionary<string, Note> Notes = new();
        }

        private readonly Data data = new();

        public bool CharacterSpecific { get => data.CharacterSpecific; set => data.CharacterSpecific = value; }
        public bool GlamourSpecific { get => data.GlamourSpecific; set => data.GlamourSpecific = value; }
        public bool EnableStyles { get => data.EnableStyles; set => data.EnableStyles = value; }
        public Markup PrefixMarkup { get => data.PrefixMarkup; set => data.PrefixMarkup = value; }
        public Markup DefaultMarkup { get => data.DefaultMarkup; set => data.DefaultMarkup = value; }

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

        public static T DeepClone<T>(T object2Copy)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };
            var json = JsonSerializer.Serialize(object2Copy, options);
            if (json == null)
                throw new NullReferenceException();
            var obj = JsonSerializer.Deserialize<T>(json, options);
            if (obj == null)
                throw new NullReferenceException();
            return obj;
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

        public Note this[string noteKey]
        {
            get
            {
                return data.Notes[noteKey];
            }
            set
            {
                data.Notes[noteKey] = value;
                Save();
            }
        }

        public bool ContainsKey(string notekey)
        {
            return data.Notes.ContainsKey(notekey);
        }

        public bool TryGetValue(string notekey, [MaybeNullWhen(false)] out Note value)
        {
            return data.Notes.TryGetValue(notekey, out value);
        }

        public bool Remove(string noteKey)
        {
            var removed = data.Notes.Remove(noteKey);
            Save();
            return removed;
        }
    }
}
