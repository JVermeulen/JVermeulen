using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JVermeulen.App.Windows
{
    /// <summary>
    /// Manager for Windows Event Logs.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Returns false when not on Windows.")]
    public class EventLogManager
    {
        /// <summary>
        /// The windows registry key for event logs.
        /// </summary>
        public const string EventLogRegistryKey = @"SYSTEM\CurrentControlSet\Services\Eventlog\";

        /// <summary>
        /// The text 'Application', the default event log name.
        /// </summary>
        public const string Application = "Application";

        /// <summary>
        /// The available event logs.
        /// </summary>
        public List<EventLog> EventLogs { get; private set; }

        /// <summary>
        /// The available event sources.
        /// </summary>
        public List<EventLog> EventSources { get; private set; }

        /// <summary>
        /// The constructor of this class.
        /// </summary>
        public EventLogManager()
        {
            //
        }

        /// <summary>
        /// Try to load all event logs and sources.
        /// </summary>
        /// <param name="error">Error when loading failed.</param>
        public bool TryLoadEventLogs(out string error)
        {
            error = null;

            try
            {
                EventLogs = GetEventLogs();
                EventSources = GetEventSources();
            }
            catch (Exception ex)
            {
                error = $"Unable to load event sources. {ex.Message}";
            }

            return error == null;
        }

        /// <summary>
        /// Load all event logs.
        /// </summary>
        public List<EventLog> GetEventLogs()
        {
            var logKey = Registry.LocalMachine.OpenSubKey(EventLogRegistryKey);

            if (logKey == null)
                return default;

            return logKey?.GetSubKeyNames().Select(l => new EventLog(l)).ToList();
        }

        /// <summary>
        /// Load all event sources.
        /// </summary>
        public List<EventLog> GetEventSources()
        {
            var result = new List<EventLog>();
            var eventLogs = GetEventLogs();

            foreach (var eventLog in eventLogs)
            {
                if (TryGetEventSource(eventLog.Log, out List<EventLog> eventSources, out string getError))
                    result.AddRange(eventSources);
            }

            return result;
        }

        /// <summary>
        /// Try to get all event sources from the registry.
        /// </summary>
        /// <param name="logName">The log name.</param>
        /// <param name="eventSources">The result.</param>
        /// <param name="error">Error when loading failed.</param>
        public bool TryGetEventSource(string logName, out List<EventLog> eventSources, out string error)
        {
            eventSources = null;
            error = null;

            try
            {
                eventSources = GetEventSources(logName);
            }
            catch (Exception ex)
            {
                error = $"Unable to load event sources. {ex.Message}";
            }

            return error == null;
        }

        /// <summary>
        /// Get all sources from the registry.
        /// </summary>
        /// <param name="logName">The log name.</param>
        public List<EventLog> GetEventSources(string logName)
        {
            var eventLogKey = Registry.LocalMachine.OpenSubKey(EventLogRegistryKey + logName);
            var sourceNames = eventLogKey?.GetSubKeyNames();

            if (sourceNames == null)
                return default;

            return sourceNames.Select(s => new EventLog(logName, Environment.MachineName, s)).ToList();
        }

        /// <summary>
        /// Get the event log with the given log and source.
        /// </summary>
        /// <param name="log">The log name.</param>
        /// <param name="source">The source name.</param>
        public EventLog GetEventLog(string log = Application, string source = null)
        {
            if (EventLogs == null)
                EventLogs = GetEventLogs();

            if (EventSources == null)
                EventSources = GetEventSources();

            if (source == null)
                return EventLogs.Where(l => l.Log.Equals(log)).FirstOrDefault();
            else
                return EventSources.Where(l => l.Log.Equals(log, StringComparison.OrdinalIgnoreCase) && l.Source.Equals(source, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        /// <summary>
        /// Get the event log with the given log and source. If it does not exist, create it.
        /// </summary>
        /// <param name="log">The log name.</param>
        /// <param name="source">The source name.</param>
        public EventLog GetOrCreateEventLog(string log, string source)
        {
            var eventLog = GetEventLog(log, source);

            if (eventLog == null)
                eventLog = CreateEventLog(log, source);

            return eventLog;
        }

        /// <summary>
        /// Create the log with the given log and source.
        /// </summary>
        /// <param name="log">The log name.</param>
        /// <param name="source">The source name.</param>
        public EventLog CreateEventLog(string log, string source)
        {
            EventLog.CreateEventSource(source, log);

            EventLogs = GetEventLogs();
            EventSources = GetEventSources();

            return GetEventLog(log, source);
        }

        /// <summary>
        /// Try to delete the log with the given log and source.
        /// </summary>
        /// <param name="log">The log name.</param>
        /// <param name="source">The source name.</param>
        /// <param name="error">Error when deleting failed.</param>
        public bool TryDeleteEventLog(string log, string source, out string error)
        {
            error = null;

            try
            {
                DeleteEventLog(log, source);
            }
            catch (Exception ex)
            {
                error = $"Unable to delete event source. {ex.Message}";
            }

            return error == null;
        }

        /// <summary>
        /// Delete the log with the given log and source.
        /// </summary>
        /// <param name="log">The log name.</param>
        /// <param name="source">The source name.</param>
        public void DeleteEventLog(string log, string source)
        {
            var eventLog = GetEventLog(log, source);

            if (eventLog != null)
            {
                EventLog.DeleteEventSource(source, Environment.MachineName);

                EventLogs = GetEventLogs();
                EventSources = GetEventSources();
            }
        }
    }
}
