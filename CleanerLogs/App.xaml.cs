﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using CleanerLogs.ViewModel;
using CleanerLogs.ViewModels;

namespace CleanerLogs
{
  /// <summary>
  /// Логика взаимодействия для App.xaml
  /// </summary>
  public partial class App : Application
  {
    protected override void OnStartup(StartupEventArgs args)
    {
      base.OnStartup(args);
      var dataContext = new MainViewModel();
      dataContext.InitConfig();
      MainWindow = new Views.MainWindow
      {
        DataContext = dataContext
      };
      MainWindow.Show();
    }
  }
}
