using Dalamud.Logging;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace NotesPlugin
{
    public class Notes
    {
        private string path;
        private readonly Dictionary<string, string> data = new Dictionary<string, string>(); // TODO: more generic annotation class?

        public Notes(string notesFilePath)
        {
            path = notesFilePath;

            try
            {
                if (File.Exists(path))
                {
                    string jsonString = File.ReadAllText(path);
                    var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                    if (deserialized == null)
                        throw new NullReferenceException();
                    data = deserialized;
                    PluginLog.Debug("Notes.json loaded successfully");
                }
            }
            catch
            {
                PluginLog.Error("Notes.json couldn't be loaded or doesn't exist(should resolve upon adding a note");
            }
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(path, json);
                PluginLog.Debug("Notes successfully edited");
            }
            catch
            {
                PluginLog.Error("Error saving notes");
            }
        }

        public Dictionary<string,string> Data()
        {
            return data;
        }
        public string this[string noteKey]
        {
            get
            {
                return data[noteKey];
            }
            set
            {
                data[noteKey] = value;
                Save();
            }
        }

        public bool ContainsKey(string notekey)
        {
            return data.ContainsKey(notekey);
        }

        public bool TryGetValue(string notekey, [MaybeNullWhen(false)] out string value)
        {
            return data.TryGetValue(notekey, out value);
        }

        public bool Remove(string noteKey)
        {
            var removed = data.Remove(noteKey);
            Save();
            return removed;
        }
    }
}
