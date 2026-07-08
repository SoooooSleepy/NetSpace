using NetSpace.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetSpace.Interfaces
{
    internal interface IDBService
    {
     
        DBTable LoadTraffic();
        void SaveTraffic(DBTable _record);
        void ClearTraffic();
    }
}
