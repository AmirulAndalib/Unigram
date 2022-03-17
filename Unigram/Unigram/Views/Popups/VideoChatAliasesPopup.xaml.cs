﻿using System.Collections.Generic;
using System.Linq;
using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Controls.Cells;
using Unigram.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unigram.Views.Popups
{
    public sealed partial class VideoChatAliasesPopup : ContentPopup
    {
        private readonly IProtoService _protoService;

        public VideoChatAliasesPopup(IProtoService protoService, Chat chat, bool canSchedule, IList<MessageSender> senders)
        {
            InitializeComponent();

            _protoService = protoService;
            var already = senders.FirstOrDefault(x => x.IsEqual(chat.VideoChat.DefaultParticipantId));
            var channel = chat.Type is ChatTypeSupergroup super && super.IsChannel;

            Title = chat.VideoChat.GroupCallId != 0
                ? Strings.Resources.VoipGroupDisplayAs
                : channel
                ? Strings.Resources.StartVoipChannelTitle
                : Strings.Resources.VoipGroupStartAs;

            MessageLabel.Text = channel
                ? Strings.Resources.VoipGroupStartAsInfo
                : Strings.Resources.VoipGroupStartAsInfoGroup;

            List.ItemsSource = senders;
            List.SelectedItem = already ?? senders.FirstOrDefault();

            Schedule.Content = channel
                ? Strings.Resources.VoipChannelScheduleVoiceChat
                : Strings.Resources.VoipGroupScheduleVoiceChat;

            Schedule.Visibility = canSchedule
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (protoService.TryGetSupergroup(chat, out Supergroup supergroup))
            {
                StartWith.Visibility = canSchedule && supergroup.Status is ChatMemberStatusCreator
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else if (protoService.TryGetBasicGroup(chat, out BasicGroup basicGroup))
            {
                StartWith.Visibility = canSchedule && basicGroup.Status is ChatMemberStatusCreator
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else
            {
                StartWith.Visibility = Visibility.Collapsed;
            }

            PrimaryButtonText = Strings.Resources.Start;
            SecondaryButtonText = Strings.Resources.Close;
        }

        public bool IsScheduleSelected { get; private set; }

        public bool IsStartWithSelected { get; private set; }

        public MessageSender SelectedSender => List.SelectedItem as MessageSender;

        #region Recycle

        private void OnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            if (args.ItemContainer == null)
            {
                args.ItemContainer = new MultipleListViewItem(false);
                args.ItemContainer.Style = List.ItemContainerStyle;
                args.ItemContainer.ContentTemplate = List.ItemTemplate;
            }

            args.IsContainerPrepared = true;
        }

        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                return;
            }

            var content = args.ItemContainer.ContentTemplateRoot as ChatShareCell;
            var messageSender = args.Item as MessageSender;

            content.UpdateState(false, false);

            var photo = content.Photo;
            var title = content.Children[1] as TextBlock;

            if (_protoService.TryGetUser(messageSender, out User user))
            {
                photo.SetUser(_protoService, user, 36);
                title.Text = user.GetFullName();
            }
            else if (_protoService.TryGetChat(messageSender, out Chat chat))
            {
                photo.SetChat(_protoService, chat, 36);
                title.Text = _protoService.GetTitle(chat);
            }
        }

        #endregion

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (List.SelectedItem is MessageSender)
            {
                //if (_protoService.TryGetUser(messageSender, out User user))
                //{
                //    PrimaryButtonText = string.Format(Strings.Resources.VoipGroupContinueAs, user.GetFullName());
                //}
                //else if (_protoService.TryGetChat(messageSender, out Chat chat))
                //{
                //    PrimaryButtonText = string.Format(Strings.Resources.VoipGroupContinueAs, _protoService.GetTitle(chat));
                //}

                IsPrimaryButtonEnabled = true;
            }
            else
            {
                IsPrimaryButtonEnabled = false;
            }
        }

        private void Schedule_Click(object sender, RoutedEventArgs e)
        {
            IsScheduleSelected = true;
            Hide(ContentDialogResult.Primary);
        }

        private void StartWith_Click(object sender, RoutedEventArgs e)
        {
            IsStartWithSelected = true;
            Hide(ContentDialogResult.Primary);
        }
    }
}
