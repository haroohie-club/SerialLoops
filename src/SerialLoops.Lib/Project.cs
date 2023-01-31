using SerialLoops.Lib.Items;
using SerialLoops.Lib.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SerialLoops.Lib
{
    public class Project
    {
        public string Name { get; set; }
        public string LangCode { get; set; }
        public string MainDirectory { get; }
        public string BaseDirectory => Path.Combine(MainDirectory, "base");
        public string IterativeDirectory => Path.Combine(MainDirectory, "iterative");

        public Project(string name, string langCode, Config config, ILogger log)
        {
            Name = name;
            LangCode = langCode;
            MainDirectory = Path.Combine(config.ProjectsDirectory, name);
            log.Log("Creating project directories...");
            try
            {
                Directory.CreateDirectory(MainDirectory);
                File.WriteAllText(Path.Combine(MainDirectory, $"{Name}.seproj"), JsonSerializer.Serialize(this));
                Directory.CreateDirectory(BaseDirectory);
                Directory.CreateDirectory(IterativeDirectory);
            }
            catch (Exception exc)
            {
                log.LogError($"Exception occurred while attempting to create project directories.\n{exc.Message}\n\n{exc.StackTrace}");
            }
        }

        public List<ItemDescription> GetItems()
        {
            //todo Return list of items (representing editable project parts, like maps, etc)
            List<ItemDescription> items = new()
            {
                new ("map_1", ItemDescription.ItemType.Map),
                new ("map_2", ItemDescription.ItemType.Map),
                new ("dialogue_1", ItemDescription.ItemType.Dialogue),
                new ("dialogue_2", ItemDescription.ItemType.Dialogue),
                new ("dialogue_3", ItemDescription.ItemType.Dialogue)
            };
            return items;
        }

        public ItemDescription? FindItem(string name)
        {
            foreach (ItemDescription item in GetItems())
            {
                if (item.Name == name)
                {
                    return item;
                }
            }
            return null;
        }

        public static Project OpenProject(string projFile, Config config, ILogger log)
        {
            log.Log($"Loading project from '{projFile}'...");
            Project project = JsonSerializer.Deserialize<Project>(projFile);
            return project;
        }
    }
}
