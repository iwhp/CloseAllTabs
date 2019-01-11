﻿using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace CloseAllTabs
{
    public class DeleteBase
    {
        protected DTE2 _dte;
        protected Options _options;

        protected void DeleteFiles(params string[] folders)
        {
            IEnumerable<string> existingFolders = folders.Where(f => Directory.Exists(f));

            foreach (string folder in existingFolders)
            {
                IEnumerable<string> files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories);

                if (!files.Any(f => f.EndsWith(".refresh") || _dte.SourceControl.IsItemUnderSCC(f)))
                {
                    try
                    {
                        Directory.Delete(folder, true);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Write(ex);
                    }
                }
            }
        }

        protected IEnumerable<Project> GetAllProjects()
        {
            return _dte.Solution.Projects
                  .Cast<Project>()
                  .SelectMany(GetChildProjects)
                  .Union(_dte.Solution.Projects.Cast<Project>())
                  .Where(p => { try { return !string.IsNullOrEmpty(p.FullName); } catch { return false; } });
        }

        private static IEnumerable<Project> GetChildProjects(Project parent)
        {
            try
            {
                if (parent.Kind != ProjectKinds.vsProjectKindSolutionFolder && parent.Collection == null)  // Unloaded
                    return Enumerable.Empty<Project>();

                if (!string.IsNullOrEmpty(parent.FullName))
                    return new[] { parent };
            }
            catch (COMException)
            {
                return Enumerable.Empty<Project>();
            }

            return parent.ProjectItems
                    .Cast<ProjectItem>()
                    .Where(p => p.SubProject != null)
                    .SelectMany(p => GetChildProjects(p.SubProject));
        }

        public static string GetSolutionRootFolder(Solution solution)
        {
            if (!string.IsNullOrEmpty(solution.FullName))
                return File.Exists(solution.FullName) ? Path.GetDirectoryName(solution.FullName) : null;

            return null;
        }

        public static string GetProjectRootFolder(Project project)
        {
            if (string.IsNullOrEmpty(project.FullName))
                return null;

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;

            if (Directory.Exists(fullPath))
                return fullPath;

            if (File.Exists(fullPath))
                return Path.GetDirectoryName(fullPath);

            return null;
        }

        public static string GetIISExpressLogsFolder()
        {
            string fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IISExpress", "Logs");
            return Directory.Exists(fullPath) ? fullPath : null;
        }

        public static string GetIISExpressTraceLogFilesFolder()
        {
            string fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IISExpress", "TraceLogFiles");
            return Directory.Exists(fullPath) ? fullPath : null;
        }
    }
}
