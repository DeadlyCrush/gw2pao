﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GW2PAO.API.Services.Interfaces;
using GW2PAO.Infrastructure;
using GW2PAO.Infrastructure.Interfaces;
using GW2PAO.Infrastructure.ViewModels;
using GW2PAO.Interfaces;
using GW2PAO.Properties;
using GW2PAO.Utility;
using GW2PAO.Views;
using Microsoft.Practices.Prism.Mvvm;
using Microsoft.Practices.Prism.PubSubEvents;
using NLog;

namespace GW2PAO.ViewModels
{
    [Export]
    public class ShellViewModel : BindableBase, IPartImportsSatisfiedNotification
    {
        /// <summary>
        /// Default logger
        /// </summary>
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [ImportMany]
        private Lazy<IMenuItem, IOrderMetadata>[] unorderedMainMenu { get; set; }

        /// <summary>
        /// MEF container
        /// </summary>
        private CompositionContainer container;

        /// <summary>
        /// Collection of menu items that make up the application's main menu
        /// </summary>
        public ObservableCollection<IMenuItem> MainMenu { get; private set; }

        /// <summary>
        /// True if the overlay menu icon is visible, else false
        /// </summary>
        public bool IsOverlayMenuIconVisible
        {
            get { return Settings.Default.IsOverlayIconVisible; }
            set
            {
                if (Settings.Default.IsOverlayIconVisible != value)
                {
                    Settings.Default.IsOverlayIconVisible = value;
                    Settings.Default.Save();
                    this.OnPropertyChanged(() => this.IsOverlayMenuIconVisible);
                }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        [ImportingConstructor]
        public ShellViewModel(
            ISystemService systemService,
            HotkeySettingsViewModel hotkeySettingsVm,
            ISettingsViewController settingsViewController,
            CompositionContainer container,
            EventAggregator eventAggregator)
        {
            this.MainMenu = new ObservableCollection<IMenuItem>();
            this.container = container;
            hotkeySettingsVm.InitializeHotkeys();
            settingsViewController.Initialize();

            // Initialize the process monitor
            GW2PAO.Views.OverlayWindow.ProcessMonitor = new ProcessMonitor(systemService, eventAggregator);
        }

        /// <summary>
        /// Called when a part's imports have been satisfied and it is safe to use.
        /// </summary>
        public void OnImportsSatisfied()
        {
            // Re-order all menu items according to their Order metadata.
            foreach (var menuItem in this.unorderedMainMenu.OrderBy(menu => menu.Metadata.Order))
            {
                var menu = menuItem.Value;
                this.MainMenu.Add(menu);
            }

            // Add the application-specific menu items
            this.MainMenu.Add(null); // Null for separator

            // Settings
            this.MainMenu.Add(new MenuItem(GW2PAO.Properties.Resources.Settings, () => Commands.OpenGeneralSettingsCommand.Execute(null)));

            // About
            this.MainMenu.Add(new MenuItem(GW2PAO.Properties.Resources.About, () => new GW2PAO.Views.AboutView().Show()));
            
            // Exit
            this.MainMenu.Add(new MenuItem(GW2PAO.Properties.Resources.Exit, () => 
                {
                    Task.Factory.StartNew(() =>
                        {
                            Commands.ApplicationShutdownCommand.Execute(null);
                            Application.Current.Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Normal);
                        });
                }));

            // At this point, the program startup should be completed
            logger.Info("Program startup complete");
        }
    }
}
