using NetSpace.Interfaces;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace NetSpace.Models
{
    internal class ApplicationContext:IDBService
    {
        private readonly SQLiteConnection _db;

        public ApplicationContext(string dbPath)
        {
            _db = new SQLiteConnection(dbPath);
            _db.CreateTable<DBTable>(); // Создаст таблицу, если её нет
          
        }

        public DBTable LoadTraffic() => _db.Table<DBTable>().FirstOrDefault()
                                     ?? new DBTable { LastUpdateDate = DateTime.Today };
  
        public void SaveTraffic(DBTable _record) => _db.InsertOrReplace(_record);
        public void ClearTraffic()
        {
            _db.DeleteAll<DBTable>();
            _db.Execute("VACUUM");
            string appPath = Environment.ProcessPath;

            if (appPath != null)
            {
                System.Diagnostics.Process.Start(appPath);
                System.Windows.Application.Current.Shutdown();
            }
            
        }

    }
}
