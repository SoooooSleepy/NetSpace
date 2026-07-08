using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetSpace.Models
{
    internal class DBTable
    {
        [PrimaryKey]
        public int Id { get; set; } = 1; 
        public double TotalDownload { get; set; }
        public double TotalUpload { get; set; }
        public double TodayDownload { get; set; }
        public double TodayUpload { get; set; }
        public DateTime LastUpdateDate { get; set; }
    }
    
   
}  
