using JVermeulen.App.Windows;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace JVermeulen.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventLogController : ControllerBase
    {
        private static EventLogManager _EventLogManager;
        private static EventLogManager EventLogManager
        {
            get
            {
                if (_EventLogManager == null)
                    _EventLogManager = new EventLogManager();

                return _EventLogManager;

            }
        }

        private readonly DateTime Today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

        private static EventLog _ApplicationEventLog;
        private static EventLog ApplicationEventLog
        {
            get
            {
                if (_ApplicationEventLog == null)
                    _ApplicationEventLog = EventLogManager.GetEventLog(EventLogManager.Application);

                return _ApplicationEventLog;
            }
        }

        private readonly ILogger<EventLogController> _logger;

        public EventLogController(ILogger<EventLogController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<EventLogEntry> Get(string log, string source)
        {
            if (log == null)
                log = EventLogManager.Application;

            var eventLog = EventLogManager.GetEventLog(log, source);

            List<EventLogEntry> result = new List<EventLogEntry>();

            if (eventLog != null)
            {
                foreach (EventLogEntry entry in eventLog.Entries)
                {
                    if (entry.TimeGenerated > Today && entry.Source == eventLog.Source)
                    {
                        result.Add(entry);
                    }
                }
            }

            return result.ToArray();
        }
    }
}
