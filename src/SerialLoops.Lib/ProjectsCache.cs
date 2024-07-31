using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib
{
    public class ProjectsCache
    {
        private const int MAX_RECENT_PROJECTS = 10;
        [JsonIgnore]
        public string CacheFilePath { get; set; }
        public List<string> RecentProjects { get; set; }
        public Dictionary<string, List<string>> RecentWorkspaces { get; set; }
        public bool HadProjectOpenOnLastClose { get; set; }

        public void Save(ILogger log)
        {
            log.Log($"Caching recent projects and workspaces to '{CacheFilePath}'...");
            IO.WriteStringFile(CacheFilePath, JsonSerializer.Serialize(this), log);
        }
        
        public static ProjectsCache LoadCache(Config config, ILogger log)
        {
            string recentProjectsJson = Path.Combine(config.UserDirectory, "projects_cache.json");
            if (!File.Exists(recentProjectsJson))
            {
                log.Log($"Creating default recent project and workspaces cache at '{recentProjectsJson}'...");
                ProjectsCache defaultRecentProjects = GetDefault();
                defaultRecentProjects.CacheFilePath = recentProjectsJson;
                IO.WriteStringFile(recentProjectsJson, JsonSerializer.Serialize(defaultRecentProjects), log);
                return defaultRecentProjects;
            }

            try
            {
                ProjectsCache recentProjects = JsonSerializer.Deserialize<ProjectsCache>(File.ReadAllText(recentProjectsJson));
                recentProjects.CacheFilePath = recentProjectsJson;
                return recentProjects;
            }
            catch (JsonException exc)
            {
                log.LogException("Exception occurred while parsing projects_cache.json!", exc);
                ProjectsCache defaultRecentProjects = GetDefault();
                IO.WriteStringFile(recentProjectsJson, JsonSerializer.Serialize(defaultRecentProjects), log);
                return defaultRecentProjects;
            }
        }
        
        private static ProjectsCache GetDefault()
        {
            return new()
            {
                RecentProjects = [],
                RecentWorkspaces = []
            };
        }

        public void CacheRecentProject(string projectPath, List<string> workspaceItems)
        {
            if (RecentProjects.Contains(projectPath))
            {
                RecentProjects.Remove(projectPath);
                RecentWorkspaces.Remove(projectPath);
            }
            if (RecentProjects.Count >= MAX_RECENT_PROJECTS)
            {
                string lastProject = RecentProjects[RecentProjects.Count - 1];
                RecentProjects.Remove(lastProject);
                RecentWorkspaces.Remove(lastProject);
            }
            RecentProjects.Insert(0, projectPath);
            RecentWorkspaces.Add(projectPath, workspaceItems);
        }
        
    }
}
