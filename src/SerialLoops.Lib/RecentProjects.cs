using HaruhiChokuretsuLib.Util;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerialLoops.Lib
{
    public class RecentProjects
    {
        private const int MAX_RECENT_PROJECTS = 10;
        [JsonIgnore]
        public string RecentProjectsPath { get; set; }
        public List<string> Projects { get; set; }

        public void Save(ILogger log)
        {
            log.Log($"Saving recent projects to '{RecentProjectsPath}'...");
            IO.WriteStringFile(RecentProjectsPath, JsonSerializer.Serialize(this), log);
        }
        
        public static RecentProjects LoadRecentProjects(Config config, ILogger log)
        {
            string recentProjectsJson = Path.Combine(config.UserDirectory, "recent_projects.json");
            if (!File.Exists(recentProjectsJson))
            {
                log.Log($"Creating default recent projects cache at '{recentProjectsJson}'...");
                RecentProjects defaultRecentProjects = GetDefault();
                defaultRecentProjects.RecentProjectsPath = recentProjectsJson;
                IO.WriteStringFile(recentProjectsJson, JsonSerializer.Serialize(defaultRecentProjects), log);
                return defaultRecentProjects;
            }

            try
            {
                RecentProjects recentProjects = JsonSerializer.Deserialize<RecentProjects>(File.ReadAllText(recentProjectsJson));
                recentProjects.RecentProjectsPath = recentProjectsJson;
                return recentProjects;
            }
            catch (JsonException exc)
            {
                log.LogError($"Exception occurred while parsing recent_projects.json!\n{exc.Message}\n\n{exc.StackTrace}");
                RecentProjects defaultRecentProjects = GetDefault();
                IO.WriteStringFile(recentProjectsJson, JsonSerializer.Serialize(defaultRecentProjects), log);
                return defaultRecentProjects;
            }
        }
        
        private static RecentProjects GetDefault()
        {
            return new()
            {
                Projects = new List<string>()
            };
        }

        public void AddProject(string projectPath)
        {
            if (Projects.Contains(projectPath))
            {
                Projects.Remove(projectPath);
            }
            if (Projects.Count >= MAX_RECENT_PROJECTS)
            {
                Projects.RemoveAt(Projects.Count - 1);
            }
            Projects.Insert(0, projectPath);
        }
        
    }   
}