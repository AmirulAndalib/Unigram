﻿using Unigram.Common;
using Unigram.Controls;
using Unigram.ViewModels;
using Unigram.Views.Host;
using Unigram.Views.Settings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unigram.Views
{
    public sealed partial class LogOutPage : HostedPage
    {
        public LogOutViewModel ViewModel => DataContext as LogOutViewModel;

        public LogOutPage()
        {
            InitializeComponent();
        }

        private void AddAnotherAccount_Click(object sender, RoutedEventArgs e)
        {
            if (Window.Current.Content is RootPage root)
            {
                root.Create();
            }
        }

        private void SetPasscode_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPasscodePage));
        }

        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsStoragePage));
        }

        private async void ChangePhoneNumber_Click(object sender, RoutedEventArgs e)
        {
            var popup = new ChangePhoneNumberPopup();
            var change = await popup.ShowQueuedAsync();
            if (change != ContentDialogResult.Primary)
            {
                return;
            }

            var confirm = await MessagePopup.ShowAsync(Strings.Resources.PhoneNumberAlert, Strings.Resources.AppName, Strings.Resources.OK, Strings.Resources.Cancel);
            if (confirm == ContentDialogResult.Primary)
            {
                Frame.Navigate(typeof(SettingsPhonePage));
            }
        }
    }
}
