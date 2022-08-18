﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MantaRay
{
    public class LogHelper
    {
        public static Dictionary<string, LogHelper> AllLogSystems = new Dictionary<string, LogHelper>();
        readonly List<LogEntry> logMessages = new List<LogEntry>();
        readonly Dictionary<Guid, LogEntry> currentTasks = new Dictionary<Guid, LogEntry>();
        public string Name;
        readonly object _logLock = new object();
        readonly object _taskLock = new object();

        public event EventHandler LogUpdated;

        public static LogHelper Default { get => GetLogHelper(); }

        public static LogHelper GetLogHelper(string name = null)
        {
            name = name ?? ConstantsHelper.ProjectName;
            if ((name == ConstantsHelper.ProjectName && !AllLogSystems.ContainsKey(ConstantsHelper.ProjectName))
               || name == null
               || String.IsNullOrEmpty(name))
            {
                return CreateLogHelper();
            }
            else
            {
                return AllLogSystems[name];
            }
        }


        public List<string> GetCurrentTasks(int number = 10, string filter = null)
        {
            List<string> msgs = new List<string>(number);
            IEnumerable<LogEntry> items;

            if (!string.IsNullOrEmpty(filter))
            {
                items = currentTasks.Values.OrderByDescending(lo => lo.Timestamp)
                    .Where(l => l.Name.Contains(filter) || l.Description.Contains(filter))
                    .Take(number);
            }
            else
            {
                items = currentTasks.Values.OrderByDescending(lo => lo.Timestamp)
                    .Take(number);
            }

            foreach (LogEntry l in items)
            {
                msgs.Add($"[{l.Timestamp:G}, {l.Name}, for {(DateTime.Now - l.Timestamp).ToReadableString()}]: {l.Description.Replace("\n", "        \n")}");
            }


            return msgs;
        }

        public Guid AddTask(string name, string description, Guid componentGuid = default, Guid guid = default)
        {
            if (guid == Guid.Empty)
            {
                guid = Guid.NewGuid();
            }
            lock (_taskLock)
            {
                currentTasks.Add(
                    guid,
                    new LogEntry()
                    {
                        Name = name,
                        Description = description,
                        ComponentGuid = componentGuid,
                        Guid = guid,
                        Timestamp = DateTime.Now
                    });
                    LogUpdated?.Invoke(this, new EventArgs());
            }

            return guid;

        }

        public void FinishTask(Guid guid)
        {
            lock (_taskLock)
            {
                var t = currentTasks[guid];
                Add(t.Name, t.Description + $"\nFinished in {(DateTime.Now - t.Timestamp).ToReadableString()}", t.ComponentGuid);
                currentTasks.Remove(guid);
            }
            LogUpdated?.Invoke(this, new EventArgs());
        }


        public List<string> GetLatestLogs(int number = 10, string filter = null)
        {
            List<string> msgs = new List<string>(number);
            IEnumerable<LogEntry> items;

            if (!string.IsNullOrEmpty(filter))
            {
                items = logMessages.OrderByDescending(lo => lo.Timestamp)
                    .Where(l => l.Name.Contains(filter) || l.Description.Contains(filter))
                    .Take(number);
            }
            else
            {
                items = logMessages.OrderByDescending(lo => lo.Timestamp)
                    .Take(number);
            }

            foreach (LogEntry l in items)
            {
                msgs.Add($"[{l.Timestamp:G}, {l.Name}]: {l.Description.Replace("\n", "        \n")}");
            }


            return msgs;
        }


        public Dictionary<string, LogHelper> LogHelpers { get; set; }

        public static LogHelper CreateLogHelper(string name = null, bool overwrite = false)
        {
            name = name ?? ConstantsHelper.ProjectName;
            LogHelper logHelper = new LogHelper()
            {
                Name = name
            };

            if (overwrite)
            {
                AllLogSystems.Remove(name);
                AllLogSystems.Add(name, logHelper);
            }
            else if (!AllLogSystems.ContainsKey(name))
            {
                AllLogSystems.Add(name, logHelper);
            }
            else
            {
                return AllLogSystems[name];
            }

            return logHelper;
        }

        public void Add(string name, string description, Guid guid = default)
        {
            lock (_logLock)
            {
                logMessages.Add(
                new LogEntry()
                {
                    Name = name,
                    Description = description,
                    ComponentGuid = guid,
                    Timestamp = DateTime.Now
                });
                LogUpdated?.Invoke(this, new EventArgs());
            }

        }

        public void CLear()
        {
            logMessages.Clear();
            LogUpdated?.Invoke(this, new EventArgs());
        }

        public class LogEntry
        {
            public string Name;
            public string Description;
            public DateTime Timestamp;
            public Guid Guid;
            public Guid ComponentGuid;
        }
    }
}
