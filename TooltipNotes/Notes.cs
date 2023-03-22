using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Plugin;
using Dalamud.Configuration;


namespace NotesPlugin
{
    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        [NonSerialized]
        public DalamudPluginInterface? PluginInterface;

        public class Markup
        {
            public ushort ColorKey = 0;
            public ushort GlowColorKey = 0;

            public static Markup DefaultNotePrefix => new()
            {
                ColorKey = 1,
                GlowColorKey = 60,
            };

            public static Markup DefaultNote => new()
            {
                ColorKey = 1,
            };

            public static Markup DefaultLabelPrefix => new()
            {
                ColorKey = 1,
                GlowColorKey = 60,
            };

            public static Markup DefaultLabel => new()
            {
                ColorKey = 1,
            };
        }

        public class Label
        {
            public string Name = "";
            public bool ShowInMenu = false;
            public  bool HideLabel = false;
            public Markup Markup = new();
        }

        public class Note
        {
            public string Text = "";
            public Markup Markup = new();
            public List<string> Labels = new();
        }

        public bool CharacterSpecific = true;
        public bool GlamourSpecific = true;
        public bool EnableStyles = false;
        public bool NotePrefix = true;
        public Markup NotePrefixMarkup = Markup.DefaultNotePrefix;
        public Markup NoteMarkup = Markup.DefaultNote;
        public bool LabelPrefix = true;
        public Markup LabelPrefixMarkup = Markup.DefaultLabelPrefix;
        public Markup LabelMarkup = Markup.DefaultLabel;
        // TODO: replace with ordered dictionary
        public Dictionary<string, Label> Labels = new();
        public readonly Dictionary<string, Note> Notes = new();

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
                PluginInterface?.SavePluginConfig(this);
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
                return Notes[noteKey];
            }
            set
            {
                Notes[noteKey] = value;
                Save();
            }
        }

        public Dictionary<string, Note> NoteDict()
        {
            return Notes;
        }
        public bool ContainsKey(string notekey)
        {
            return Notes.ContainsKey(notekey);
        }

        public bool TryGetValue(string notekey, [MaybeNullWhen(false)] out Note value)
        {
            return Notes.TryGetValue(notekey, out value);
        }

        public bool Remove(string noteKey)
        {
            var removed = Notes.Remove(noteKey);
            Save();
            return removed;
        }
    }
}
