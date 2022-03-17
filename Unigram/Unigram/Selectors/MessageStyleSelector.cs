﻿using Unigram.Common;
using Unigram.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unigram.Selectors
{
    public class MessageStyleSelector : StyleSelector
    {
        public Style ExpandedStyle { get; set; }
        public Style MessageStyle { get; set; }
        public Style ServiceStyle { get; set; }

        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            if (item is MessageViewModel message && message.IsService())
            {
                return ServiceStyle;
            }

            // Windows 11 Multiple selection mode looks nice
            if (ApiInfo.IsWindows11)
            {
                return MessageStyle;
            }

            // Legacy expanded style for Windows 10
            return ExpandedStyle;
        }
    }
}
