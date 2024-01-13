using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace NotesPlugin;

public class ItemNote
{
    [NonSerialized]
    public string ConfigDirectory;

    public class Label
    {
        public string Name = "";
        public bool ShowInMenu = false;
        public  bool HideLabel = false;
        public Config.Markup Markup = new();
    }

    public class Note
    {
        public string Text = "";
        public Config.Markup Markup = new();
        public List<string> Labels = new();
    }
    public Dictionary<string, Label> Labels = new();
    public Dictionary<string, Note> Notes = new();
    
    
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

    public bool TryGetValue(string notekey, [MaybeNullWhen(false)] out ItemNote.Note value)
    {
        return Notes.TryGetValue(notekey, out value);
    }

    public bool Remove(string noteKey)
    {
        var removed = Notes.Remove(noteKey);
        Save();
        return removed;
    }

    public void Save()
    {
        string fileName = "ItemNotes.json";
        string path = System.IO.Path.Combine(ConfigDirectory, fileName);
        try
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };
            var json = JsonSerializer.Serialize(this, options);
            if (json == null)
                throw new NullReferenceException();
            System.IO.File.WriteAllText(path, json);
            Plugin.PluginLog?.Debug("Configuration saved successfully!");
        }
        catch
        {
            Plugin.PluginLog?.Error("Configuration could not be saved");
        }
    }
    public static ItemNote Load(string configDirectory)
    {
        string fileName = "ItemNotes.json";
        string path = System.IO.Path.Combine(configDirectory, fileName);
        try
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            }; 
            var json = System.IO.File.ReadAllText(path);
            if (json == null)
                throw new NullReferenceException();
            var obj = JsonSerializer.Deserialize<ItemNote>(json, options);
            if (obj == null)
                throw new NullReferenceException();
            obj.ConfigDirectory = configDirectory;
            Plugin.PluginLog.Debug("Configuration loaded successfully!");
            return obj;
        }
        
        catch
        {
            Plugin.PluginLog?.Error("Configuration could not be loaded");
            return new ItemNote();
        }
    }
}
