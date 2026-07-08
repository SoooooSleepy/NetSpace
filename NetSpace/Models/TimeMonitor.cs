using NetSpace.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Swift;
using System.Text;

namespace NetSpace.Models
{
    internal class TimeMonitor: ITimeService
    {
        public DateTime _start = DateTime.Now;

        public string GetUptimeString()
        {
            TimeSpan _Different = DateTime.Now - _start;
            return Formatter(_Different);
        }
        private string Formatter(TimeSpan _mathTime) => _mathTime switch
        {
            { TotalDays: >= 1 } => $"{_mathTime.Days}д {_mathTime.Hours}ч",
            { TotalHours: >= 1 } => $"{_mathTime.Hours}ч {_mathTime.Minutes}м",
            _ => $"{_mathTime.Minutes}м {_mathTime.Seconds}c",
        };
    }
}
