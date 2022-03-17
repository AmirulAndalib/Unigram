﻿using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Views.Chats;
using Windows.UI.Xaml.Controls;

namespace Unigram.Views.Users
{
    public sealed partial class UserCommonChatsPage : ChatSharedMediaPageBase
    {
        public UserCommonChatsPage()
        {
            InitializeComponent();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Chat chat)
            {
                ViewModel.NavigationService.NavigateToChat(chat);
            }
        }

        protected override void OnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            if (args.ItemContainer == null)
            {
                args.ItemContainer = new TextListViewItem();
                args.ItemContainer.Style = ScrollingHost.ItemContainerStyle;
            }

            args.ItemContainer.ContentTemplate = ScrollingHost.ItemTemplate;

            args.IsContainerPrepared = true;
        }

        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                return;
            }

            var content = args.ItemContainer.ContentTemplateRoot as Grid;
            var chat = args.Item as Chat;

            if (args.Phase == 0)
            {
                var title = content.Children[1] as TextBlock;
                title.Text = ViewModel.ProtoService.GetTitle(chat);
            }
            else if (args.Phase == 1)
            {

            }
            else if (args.Phase == 2)
            {
                var photo = content.Children[0] as ProfilePicture;
                photo.SetChat(ViewModel.ProtoService, chat, 36);
            }

            if (args.Phase < 2)
            {
                args.RegisterUpdateCallback(OnContainerContentChanging);
            }

            args.Handled = true;
        }
    }
}
