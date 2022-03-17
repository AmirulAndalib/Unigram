﻿using System;
using System.Threading.Tasks;
using Unigram.Services;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unigram.Controls
{
    public sealed partial class StartupSwitch : UserControl
    {
        public StartupSwitch()
        {
            InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.StartupTaskContract", 2))
            {
#if DESKTOP_BRIDGE
                FindName(nameof(ToggleMinimized));
                FindName(nameof(ToggleMinimizedSeparator));
#endif

                OnLoaded();
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        private async void OnLoaded()
        {
            Toggle.Checked -= OnToggled;
            Toggle.Unchecked -= OnToggled;

            if (ToggleMinimized != null)
            {
                ToggleMinimized.Checked -= Minimized_Toggled;
                ToggleMinimized.Unchecked -= Minimized_Toggled;
            }

            var task = await GetTaskAsync();
            if (task == null || task.State == StartupTaskState.DisabledByUser)
            {
                Toggle.IsChecked = false;
                Toggle.IsEnabled = false;

                if (ToggleMinimized != null)
                {
                    ToggleMinimized.IsChecked = false;
                    ToggleMinimized.Visibility = Visibility.Collapsed;
                }

                Headered.Footer = Strings.Resources.lng_settings_auto_start_disabled_uwp
                    .Replace("Telegram Desktop", "Unigram");

                Visibility = Visibility.Visible;
            }
            else if (task.State == StartupTaskState.Enabled)
            {
                Toggle.IsChecked = true;
                Toggle.IsEnabled = true;

                if (ToggleMinimized != null)
                {
                    ToggleMinimized.IsChecked = SettingsService.Current.IsLaunchMinimized;
                    ToggleMinimized.Visibility = Visibility.Visible;
                }

                Headered.Footer = string.Empty;

                Visibility = Visibility.Visible;
            }
            else if (task.State == StartupTaskState.Disabled)
            {
                Toggle.IsChecked = false;
                Toggle.IsEnabled = true;

                if (ToggleMinimized != null)
                {
                    ToggleMinimized.IsChecked = false;
                    ToggleMinimized.Visibility = Visibility.Collapsed;
                }

                Headered.Footer = string.Empty;

                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }

            Toggle.Checked += OnToggled;
            Toggle.Unchecked += OnToggled;

            if (ToggleMinimized != null)
            {
                ToggleMinimized.Checked += Minimized_Toggled;
                ToggleMinimized.Unchecked += Minimized_Toggled;
            }
        }

        private async void OnToggled(object sender, RoutedEventArgs e)
        {
            var task = await GetTaskAsync();
            if (task == null)
            {
                return;
            }

            if (Toggle.IsChecked is true)
            {
                await task.RequestEnableAsync();
            }
            else
            {
                task.Disable();
            }

            OnLoaded();
        }

        private async Task<StartupTask> GetTaskAsync()
        {
            try
            {
                return await StartupTask.GetAsync("Telegram");
            }
            catch
            {
                return null;
            }
        }

        private void Minimized_Toggled(object sender, RoutedEventArgs e)
        {
            SettingsService.Current.IsLaunchMinimized = ToggleMinimized.IsChecked is true;
        }
    }
}
