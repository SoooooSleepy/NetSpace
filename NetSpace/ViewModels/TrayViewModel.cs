using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetSpace.Interfaces;
using NetSpace.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace NetSpace.ViewModels
{
    internal class TrayViewModel:ObservableObject
    {
        public IRelayCommand OpenCommand { get; }
        public IRelayCommand ExitCommand { get; }
       
        
        public TrayViewModel()
        {
            OpenCommand = new RelayCommand(Open);
            ExitCommand = new RelayCommand(Exit);
     
        }

        private void Open()
        {
            var wnd = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (wnd == null)
            {
                wnd = new MainWindow();
                wnd.Show();
            }
            else
            {
                wnd.Show();
                wnd.Activate();
            }
        }

        private void Exit() => System.Windows.Application.Current.Shutdown();
      
    }
}
