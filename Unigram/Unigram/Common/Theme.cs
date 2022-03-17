﻿using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Telegram.Td.Api;
using Unigram.Services;
using Unigram.Services.Settings;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Unigram.Common
{
    public class Theme : ResourceDictionary
    {
        [ThreadStatic]
        public static Theme Current;

        private readonly ApplicationDataContainer isolatedStore;

        public Theme()
        {
            var main = Current == null;

            try
            {
                isolatedStore = ApplicationData.Current.LocalSettings.CreateContainer("Theme", ApplicationDataCreateDisposition.Always);
                Current ??= this;

                this.Add("MessageFontSize", GetValueOrDefault("MessageFontSize", 14d));

                var emojiSet = SettingsService.Current.Appearance.EmojiSet;
                switch (emojiSet.Id)
                {
                    case "microsoft":
                        this.Add("EmojiThemeFontFamily", new FontFamily($"XamlAutoFontFamily"));
                        break;
                    case "apple":
                        this.Add("EmojiThemeFontFamily", new FontFamily($"ms-appx:///Assets/Emoji/{emojiSet.Id}.ttf#Segoe UI Emoji"));
                        break;
                    default:
                        this.Add("EmojiThemeFontFamily", new FontFamily($"ms-appdata:///local/emoji/{emojiSet.Id}.{emojiSet.Version}.ttf#Segoe UI Emoji"));
                        break;
                }

                this.Add("ThreadStackLayout", new StackLayout());
            }
            catch { }

            MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Themes/ThemeGreen.xaml") });

            if (main)
            {
                UpdateAcrylicBrushes();
                Initialize();
            }
        }

        public ChatTheme ChatTheme => _lastTheme;

        public void Initialize()
        {
            Initialize(SettingsService.Current.Appearance.GetCalculatedApplicationTheme());
        }

        public void Initialize(ApplicationTheme requested)
        {
            Initialize(requested == ApplicationTheme.Dark ? TelegramTheme.Dark : TelegramTheme.Light);
        }

        public void Initialize(ElementTheme requested)
        {
            Initialize(requested == ElementTheme.Dark ? TelegramTheme.Dark : TelegramTheme.Light);
        }

        public void Initialize(TelegramTheme requested)
        {
            var settings = SettingsService.Current.Appearance;
            if (settings[requested].Type == TelegramThemeType.Custom && System.IO.File.Exists(settings[requested].Custom))
            {
                Update(ThemeCustomInfo.FromFile(settings[requested].Custom));
            }
            else if (ThemeAccentInfo.IsAccent(settings[requested].Type))
            {
                Update(ThemeAccentInfo.FromAccent(settings[requested].Type, settings.Accents[settings[requested].Type]));
            }
            else
            {
                Update(requested);
            }
        }

        public void Initialize(string path)
        {
            Update(ThemeCustomInfo.FromFile(path));
        }

        private int? _lastAccent;
        private long? _lastBackground;

        private ChatTheme _lastTheme;

        public bool Update(ElementTheme elementTheme, ChatTheme theme)
        {
            var updated = false;
            var requested = elementTheme == ElementTheme.Dark ? TelegramTheme.Dark : TelegramTheme.Light;

            var settings = requested == TelegramTheme.Light ? theme?.LightSettings : theme?.DarkSettings;
            if (settings != null)
            {
                if (_lastAccent != settings.AccentColor)
                {
                    _lastTheme = theme;

                    var tint = SettingsService.Current.Appearance[requested].Type;
                    if (tint == TelegramThemeType.Classic || (tint == TelegramThemeType.Custom && requested == TelegramTheme.Light))
                    {
                        tint = TelegramThemeType.Day;
                    }
                    else if (tint == TelegramThemeType.Custom)
                    {
                        tint = TelegramThemeType.Tinted;
                    }

                    var accent = settings.AccentColor.ToColor();
                    var outgoing = settings.OutgoingMessageAccentColor.ToColor();

                    var info = ThemeAccentInfo.FromAccent(tint, accent, outgoing);
                    ThemeOutgoing.Update(info.Parent, info.Values);
                    ThemeIncoming.Update(info.Parent, info.Values);
                }
                if (_lastBackground != settings.Background?.Id)
                {
                    updated = true;
                }

                _lastAccent = settings.AccentColor;
                _lastBackground = settings.Background?.Id;
            }
            else
            {
                if (_lastAccent != null)
                {
                    _lastTheme = null;

                    var options = SettingsService.Current.Appearance;
                    if (options[requested].Type == TelegramThemeType.Custom && System.IO.File.Exists(options[requested].Custom))
                    {
                        var info = ThemeCustomInfo.FromFile(options[requested].Custom);
                        ThemeOutgoing.Update(info.Parent, info.Values);
                        ThemeIncoming.Update(info.Parent, info.Values);
                    }
                    else if (ThemeAccentInfo.IsAccent(options[requested].Type))
                    {
                        var info = ThemeAccentInfo.FromAccent(options[requested].Type, options.Accents[options[requested].Type]);
                        ThemeOutgoing.Update(info.Parent, info.Values);
                        ThemeIncoming.Update(info.Parent, info.Values);
                    }
                    else
                    {
                        ThemeOutgoing.Update(requested);
                        ThemeIncoming.Update(requested);
                    }
                }
                if (_lastBackground != null)
                {
                    updated = true;
                }

                _lastAccent = null;
                _lastBackground = null;
            }

            return updated;
        }

        public void Update(ThemeInfoBase info)
        {
            if (info is ThemeCustomInfo custom)
            {
                Update(info.Parent, custom);
            }
            else if (info is ThemeAccentInfo colorized)
            {
                Update(info.Parent, colorized);
            }
            else
            {
                Update(info.Parent);
            }
        }

        private void Update(TelegramTheme requested, ThemeAccentInfo info = null)
        {
            try
            {
                ThemeOutgoing.Update(requested, info?.Values);
                ThemeIncoming.Update(requested, info?.Values);

                var values = info?.Values;
                var shades = info?.Shades;

                var target = MergedDictionaries[0].ThemeDictionaries[requested == TelegramTheme.Light ? "Light" : "Dark"] as ResourceDictionary;
                var lookup = ThemeService.GetLookup(requested);

                if (shades != null && shades.TryGetValue(AccentShade.Default, out Color accentResource))
                {
                    target["Accent"] = accentResource;
                }
                else
                {
                    target["Accent"] = ThemeInfoBase.Accents[TelegramThemeType.Day][AccentShade.Default];
                }

                foreach (var item in lookup)
                {
                    if (target.TryGet(item.Key, out SolidColorBrush brush))
                    {
                        Color value;
                        if (item.Value is AccentShade shade)
                        {
                            if (shades != null && shades.TryGetValue(shade, out Color accent))
                            {
                                value = accent;
                            }
                            else
                            {
                                value = ThemeInfoBase.Accents[TelegramThemeType.Day][shade];
                            }
                        }
                        else if (values != null && values.TryGetValue(item.Key, out Color themed))
                        {
                            value = themed;
                        }
                        else if (item.Value is Color color)
                        {
                            value = color;
                        }

                        if (brush.Color == value)
                        {
                            continue;
                        }

                        try
                        {
                            brush.Color = value;
                        }
                        catch
                        {
                            // Some times access denied is thrown,
                            // not sure why, but I can't see other options.
                            target[item.Key] = new SolidColorBrush(value);

                            // The best thing here would be to notify
                            // AppearanceSettings about this and
                            // refresh the whole theme by switching it.
                        }
                    }
                }
            }
            catch { }
        }

        #region Acrylic patch

        private void UpdateAcrylicBrushes()
        {
            UpdateAcrylicBrushesLightTheme(MergedDictionaries[0].ThemeDictionaries["Light"] as ResourceDictionary);
            UpdateAcrylicBrushesDarkTheme(MergedDictionaries[0].ThemeDictionaries["Dark"] as ResourceDictionary);
        }

        private void UpdateAcrylicBrushesLightTheme(ResourceDictionary dictionary)
        {
            if (dictionary.TryGet("AcrylicBackgroundFillColorDefaultBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicBackgroundFillColorDefaultBrush))
            {
                acrylicBackgroundFillColorDefaultBrush.TintLuminosityOpacity = 0.85;
            }
            if (dictionary.TryGet("AcrylicInAppFillColorDefaultBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicInAppFillColorDefaultBrush))
            {
                acrylicInAppFillColorDefaultBrush.TintLuminosityOpacity = 0.85;
            }
            if (dictionary.TryGet("AcrylicBackgroundFillColorDefaultInverseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicBackgroundFillColorDefaultInverseBrush))
            {
                acrylicBackgroundFillColorDefaultInverseBrush.TintLuminosityOpacity = 0.96;
            }
            if (dictionary.TryGet("AcrylicInAppFillColorDefaultInverseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicInAppFillColorDefaultInverseBrush))
            {
                acrylicInAppFillColorDefaultInverseBrush.TintLuminosityOpacity = 0.96;
            }
            if (dictionary.TryGet("AcrylicBackgroundFillColorBaseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicBackgroundFillColorBaseBrush))
            {
                acrylicBackgroundFillColorBaseBrush.TintLuminosityOpacity = 0.9;
            }
            if (dictionary.TryGet("AcrylicInAppFillColorBaseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicInAppFillColorBaseBrush))
            {
                acrylicInAppFillColorBaseBrush.TintLuminosityOpacity = 0.9;
            }
            if (dictionary.TryGet("AccentAcrylicBackgroundFillColorDefaultBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush accentAcrylicBackgroundFillColorDefaultBrush))
            {
                accentAcrylicBackgroundFillColorDefaultBrush.TintLuminosityOpacity = 0.9;
            }
            if (dictionary.TryGet("AccentAcrylicInAppFillColorDefaultBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush accentAcrylicInAppFillColorDefaultBrush))
            {
                accentAcrylicInAppFillColorDefaultBrush.TintLuminosityOpacity = 0.9;
            }
            if (dictionary.TryGet("AccentAcrylicBackgroundFillColorBaseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush accentAcrylicBackgroundFillColorBaseBrush))
            {
                accentAcrylicBackgroundFillColorBaseBrush.TintLuminosityOpacity = 0.9;
            }
            if (dictionary.TryGet("AccentAcrylicInAppFillColorBaseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush accentAcrylicInAppFillColorBaseBrush))
            {
                accentAcrylicInAppFillColorBaseBrush.TintLuminosityOpacity = 0.9;
            }
        }

        private void UpdateAcrylicBrushesDarkTheme(ResourceDictionary dictionary)
        {
            if (dictionary.TryGet("AcrylicBackgroundFillColorDefaultBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicBackgroundFillColorDefaultBrush))
            {
                acrylicBackgroundFillColorDefaultBrush.TintLuminosityOpacity = 0.96;
            }
            if (dictionary.TryGet("AcrylicInAppFillColorDefaultBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicInAppFillColorDefaultBrush))
            {
                acrylicInAppFillColorDefaultBrush.TintLuminosityOpacity = 0.96;
            }
            if (dictionary.TryGet("AcrylicBackgroundFillColorDefaultInverseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicBackgroundFillColorDefaultInverseBrush))
            {
                acrylicBackgroundFillColorDefaultInverseBrush.TintLuminosityOpacity = 0.85;
            }
            if (dictionary.TryGet("AcrylicInAppFillColorDefaultInverseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicInAppFillColorDefaultInverseBrush))
            {
                acrylicInAppFillColorDefaultInverseBrush.TintLuminosityOpacity = 0.85;
            }
            if (dictionary.TryGet("AcrylicBackgroundFillColorBaseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicBackgroundFillColorBaseBrush))
            {
                acrylicBackgroundFillColorBaseBrush.TintLuminosityOpacity = 0.96;
            }
            if (dictionary.TryGet("AcrylicInAppFillColorBaseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush acrylicInAppFillColorBaseBrush))
            {
                acrylicInAppFillColorBaseBrush.TintLuminosityOpacity = 0.96;
            }
            if (dictionary.TryGet("AccentAcrylicBackgroundFillColorDefaultBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush accentAcrylicBackgroundFillColorDefaultBrush))
            {
                accentAcrylicBackgroundFillColorDefaultBrush.TintLuminosityOpacity = 0.8;
            }
            if (dictionary.TryGet("AccentAcrylicInAppFillColorDefaultBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush accentAcrylicInAppFillColorDefaultBrush))
            {
                accentAcrylicInAppFillColorDefaultBrush.TintLuminosityOpacity = 0.8;
            }
            if (dictionary.TryGet("AccentAcrylicBackgroundFillColorBaseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush accentAcrylicBackgroundFillColorBaseBrush))
            {
                accentAcrylicBackgroundFillColorBaseBrush.TintLuminosityOpacity = 0.8;
            }
            if (dictionary.TryGet("AccentAcrylicInAppFillColorBaseBrush", out Microsoft.UI.Xaml.Media.AcrylicBrush accentAcrylicInAppFillColorBaseBrush))
            {
                accentAcrylicInAppFillColorBaseBrush.TintLuminosityOpacity = 0.8;
            }
        }

        #endregion

        #region Settings

        private int? _messageFontSize;
        public int MessageFontSize
        {
            get
            {
                if (_messageFontSize == null)
                {
                    _messageFontSize = (int)GetValueOrDefault("MessageFontSize", 14d);
                }

                return _messageFontSize ?? 14;
            }
            set
            {
                _messageFontSize = value;
                AddOrUpdateValue("MessageFontSize", (double)value);
            }
        }

        public bool AddOrUpdateValue(string key, object value)
        {
            bool valueChanged = false;

            if (isolatedStore.Values.ContainsKey(key))
            {
                if (isolatedStore.Values[key] != value)
                {
                    isolatedStore.Values[key] = value;
                    valueChanged = true;
                }
            }
            else
            {
                isolatedStore.Values.Add(key, value);
                valueChanged = true;
            }

            if (valueChanged)
            {
                try
                {
                    if (this.ContainsKey(key))
                    {
                        this[key] = value;
                    }
                    else
                    {
                        this.Add(key, value);
                    }
                }
                catch { }
            }

            return valueChanged;
        }

        public valueType GetValueOrDefault<valueType>(string key, valueType defaultValue)
        {
            valueType value;

            if (isolatedStore.Values.ContainsKey(key))
            {
                value = (valueType)isolatedStore.Values[key];
            }
            else
            {
                value = defaultValue;
            }

            return value;
        }

        #endregion
    }

    public class ThemeOutgoing : ResourceDictionary
    {
        [ThreadStatic]
        private static Dictionary<string, (Color Color, SolidColorBrush Brush)> _light;
        public static Dictionary<string, (Color Color, SolidColorBrush Brush)> Light => _light ??= new()
        {
            { "MessageForegroundBrush", (Color.FromArgb(0xFF, 0x00, 0x00, 0x00), new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00))) },
            { "MessageForegroundLinkBrush", (Color.FromArgb(0xFF, 0x16, 0x8A, 0xCD), new SolidColorBrush(Color.FromArgb(0xFF, 0x16, 0x8A, 0xCD))) },
            { "MessageBackgroundBrush", (Color.FromArgb(0xFF, 0xF0, 0xFD, 0xDF), new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xFD, 0xDF))) },
            { "MessageSubtleLabelBrush", (Color.FromArgb(0xFF, 0x6D, 0xC2, 0x64), new SolidColorBrush(Color.FromArgb(0xFF, 0x6D, 0xC2, 0x64))) },
            { "MessageSubtleGlyphBrush", (Color.FromArgb(0xFF, 0x5D, 0xC4, 0x52), new SolidColorBrush(Color.FromArgb(0xFF, 0x5D, 0xC4, 0x52))) },
            { "MessageSubtleForegroundBrush", (Color.FromArgb(0xFF, 0x6D, 0xC2, 0x64), new SolidColorBrush(Color.FromArgb(0xFF, 0x6D, 0xC2, 0x64))) },
            { "MessageHeaderForegroundBrush", (Color.FromArgb(0xFF, 0x3A, 0x8E, 0x26), new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x8E, 0x26))) },
            { "MessageHeaderBorderBrush", (Color.FromArgb(0xFF, 0x5D, 0xC4, 0x52), new SolidColorBrush(Color.FromArgb(0xFF, 0x5D, 0xC4, 0x52))) },
            { "MessageMediaForegroundBrush", (Color.FromArgb(0xFF, 0xF0, 0xFD, 0xDF), new SolidColorBrush(Color.FromArgb(0xFF, 0xF0, 0xFD, 0xDF))) },
            { "MessageMediaBackgroundBrush", (Color.FromArgb(0xFF, 0x78, 0xC6, 0x7F), new SolidColorBrush(Color.FromArgb(0xFF, 0x78, 0xC6, 0x7F))) },
            { "MessageOverlayBackgroundBrush", (Color.FromArgb(0x54, 0x00, 0x00, 0x00), new SolidColorBrush(Color.FromArgb(0x54, 0x00, 0x00, 0x00))) },
            { "MessageCallForegroundBrush", (Color.FromArgb(0xFF, 0x2A, 0xB3, 0x2A), new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0xB3, 0x2A))) },
            { "MessageCallMissedForegroundBrush", (Color.FromArgb(0xFF, 0xDD, 0x58, 0x49), new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0x58, 0x49))) },
            { "MessageReactionBackgroundBrush", (Color.FromArgb(0xFF, 0xD5, 0xF1, 0xC9), new SolidColorBrush(Color.FromArgb(0xFF, 0xD5, 0xF1, 0xC9))) },
            { "MessageReactionForegroundBrush", (Color.FromArgb(0xFF, 0x45, 0xA3, 0x2D), new SolidColorBrush(Color.FromArgb(0xFF, 0x45, 0xA3, 0x2D))) },
            { "MessageReactionChosenBackgroundBrush", (Color.FromArgb(0xFF, 0x5F, 0xBE, 0x67), new SolidColorBrush(Color.FromArgb(0xFF, 0x5F, 0xBE, 0x67))) },
            { "MessageReactionChosenForegroundBrush", (Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF))) },
        };

        [ThreadStatic]
        private static Dictionary<string, (Color Color, SolidColorBrush Brush)> _dark;
        public static Dictionary<string, (Color Color, SolidColorBrush Brush)> Dark => _dark ??= new()
        {
            { "MessageForegroundBrush", (Color.FromArgb(0xFF, 0xE4, 0xEC, 0xF2), new SolidColorBrush(Color.FromArgb(0xFF, 0xE4, 0xEC, 0xF2))) },
            { "MessageForegroundLinkBrush", (Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF))) },
            { "MessageBackgroundBrush", (Color.FromArgb(0xFF, 0x2B, 0x52, 0x78), new SolidColorBrush(Color.FromArgb(0xFF, 0x2B, 0x52, 0x78))) },
            { "MessageSubtleLabelBrush", (Color.FromArgb(0xFF, 0x7D, 0xA8, 0xD3), new SolidColorBrush(Color.FromArgb(0xFF, 0x7D, 0xA8, 0xD3))) },
            { "MessageSubtleGlyphBrush", (Color.FromArgb(0xFF, 0x72, 0xBC, 0xFD), new SolidColorBrush(Color.FromArgb(0xFF, 0x72, 0xBC, 0xFD))) },
            { "MessageSubtleForegroundBrush", (Color.FromArgb(0xFF, 0x7D, 0xA8, 0xD3), new SolidColorBrush(Color.FromArgb(0xFF, 0x7D, 0xA8, 0xD3))) },
            { "MessageHeaderForegroundBrush", (Color.FromArgb(0xFF, 0x90, 0xCA, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0x90, 0xCA, 0xFF))) },
            { "MessageHeaderBorderBrush", (Color.FromArgb(0xFF, 0x65, 0xB9, 0xF4), new SolidColorBrush(Color.FromArgb(0xFF, 0x65, 0xB9, 0xF4))) },
            { "MessageMediaForegroundBrush", (Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF))) },
            { "MessageMediaBackgroundBrush", (Color.FromArgb(0xFF, 0x4C, 0x9C, 0xE2), new SolidColorBrush(Color.FromArgb(0xFF, 0x4C, 0x9C, 0xE2))) },
            { "MessageOverlayBackgroundBrush", (Color.FromArgb(0x54, 0x00, 0x00, 0x00), new SolidColorBrush(Color.FromArgb(0x54, 0x00, 0x00, 0x00))) },
            { "MessageCallForegroundBrush", (Color.FromArgb(0xFF, 0x49, 0xA2, 0xF0), new SolidColorBrush(Color.FromArgb(0xFF, 0x49, 0xA2, 0xF0))) },
            { "MessageCallMissedForegroundBrush", (Color.FromArgb(0xFF, 0xED, 0x50, 0x50), new SolidColorBrush(Color.FromArgb(0xFF, 0xED, 0x50, 0x50))) },
            { "MessageReactionBackgroundBrush", (Color.FromArgb(0xFF, 0x2B, 0x41, 0x53), new SolidColorBrush(Color.FromArgb(0xFF, 0x2B, 0x41, 0x53))) },
            { "MessageReactionForegroundBrush", (Color.FromArgb(0xFF, 0x7A, 0xC3, 0xF4), new SolidColorBrush(Color.FromArgb(0xFF, 0x7A, 0xC3, 0xF4))) },
            { "MessageReactionChosenBackgroundBrush", (Color.FromArgb(0xFF, 0x31, 0x8E, 0xE4), new SolidColorBrush(Color.FromArgb(0xFF, 0x31, 0x8E, 0xE4))) },
            { "MessageReactionChosenForegroundBrush", (Color.FromArgb(0xFF, 0x33, 0x39, 0x3F), new SolidColorBrush(Color.FromArgb(0xFF, 0x33, 0x39, 0x3F))) },
        };

        public ThemeOutgoing()
        {
            var light = new ResourceDictionary();
            var dark = new ResourceDictionary();

            foreach (var item in Light)
            {
                light[item.Key] = item.Value.Brush;
            }

            foreach (var item in Dark)
            {
                dark[item.Key] = item.Value.Brush;
            }

            ThemeDictionaries["Light"] = light;
            ThemeDictionaries["Default"] = dark;
        }

        public static void Update(TelegramTheme parent, IDictionary<string, Color> values = null)
        {
            if (values == null)
            {
                Update(parent);
                return;
            }

            var target = parent == TelegramTheme.Dark ? Dark : Light;

            foreach (var value in target)
            {
                var key = value.Key.Substring(0, value.Key.Length - "Brush".Length);
                if (values.TryGetValue($"{key}Outgoing", out Color color))
                {
                    value.Value.Brush.Color = color;
                }
                else
                {
                    value.Value.Brush.Color = value.Value.Color;
                }
            }
        }

        public static void Update(TelegramTheme parent)
        {
            if (parent == TelegramTheme.Light)
            {
                foreach (var value in Light)
                {
                    value.Value.Brush.Color = value.Value.Color;
                }
            }
            else
            {
                foreach (var value in Dark)
                {
                    value.Value.Brush.Color = value.Value.Color;
                }
            }
        }
    }

    public class ThemeIncoming : ResourceDictionary
    {
        [ThreadStatic]
        private static Dictionary<string, (Color Color, SolidColorBrush Brush)> _light;
        public static Dictionary<string, (Color Color, SolidColorBrush Brush)> Light => _light ??= new()
        {
            { "MessageForegroundBrush", (Color.FromArgb(0xFF, 0x00, 0x00, 0x00), new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00))) },
            { "MessageForegroundLinkBrush", (Color.FromArgb(0xFF, 0x16, 0x8A, 0xCD), new SolidColorBrush(Color.FromArgb(0xFF, 0x16, 0x8A, 0xCD))) },
            { "MessageBackgroundBrush", (Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF))) },
            { "MessageSubtleLabelBrush", (Color.FromArgb(0xFF, 0xA1, 0xAD, 0xB6), new SolidColorBrush(Color.FromArgb(0xFF, 0xA1, 0xAD, 0xB6))) },
            { "MessageSubtleGlyphBrush", (Color.FromArgb(0xFF, 0xA1, 0xAD, 0xB6), new SolidColorBrush(Color.FromArgb(0xFF, 0xA1, 0xAD, 0xB6))) },
            { "MessageSubtleForegroundBrush", (Color.FromArgb(0xFF, 0xA1, 0xAD, 0xB6), new SolidColorBrush(Color.FromArgb(0xFF, 0xA1, 0xAD, 0xB6))) },
            { "MessageHeaderForegroundBrush", (Color.FromArgb(0xFF, 0x15, 0x8D, 0xCD), new SolidColorBrush(Color.FromArgb(0xFF, 0x15, 0x8D, 0xCD))) },
            { "MessageHeaderBorderBrush", (Color.FromArgb(0xFF, 0x37, 0xA4, 0xDE), new SolidColorBrush(Color.FromArgb(0xFF, 0x37, 0xA4, 0xDE))) },
            { "MessageMediaForegroundBrush", (Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF))) },
            { "MessageMediaBackgroundBrush", (Color.FromArgb(0xFF, 0x40, 0xA7, 0xE3), new SolidColorBrush(Color.FromArgb(0xFF, 0x40, 0xA7, 0xE3))) },
            { "MessageOverlayBackgroundBrush", (Color.FromArgb(0x54, 0x00, 0x00, 0x00), new SolidColorBrush(Color.FromArgb(0x54, 0x00, 0x00, 0x00))) },
            { "MessageCallForegroundBrush", (Color.FromArgb(0xFF, 0x2A, 0xB3, 0x2A), new SolidColorBrush(Color.FromArgb(0xFF, 0x2A, 0xB3, 0x2A))) },
            { "MessageCallMissedForegroundBrush", (Color.FromArgb(0xFF, 0xDD, 0x58, 0x49), new SolidColorBrush(Color.FromArgb(0xFF, 0xDD, 0x58, 0x49))) },
            { "MessageReactionBackgroundBrush", (Color.FromArgb(0xFF, 0xE8, 0xF5, 0xFC), new SolidColorBrush(Color.FromArgb(0xFF, 0xE8, 0xF5, 0xFC))) },
            { "MessageReactionForegroundBrush", (Color.FromArgb(0xFF, 0x16, 0x8D, 0xCD), new SolidColorBrush(Color.FromArgb(0xFF, 0x16, 0x8D, 0xCD))) },
            { "MessageReactionChosenBackgroundBrush", (Color.FromArgb(0xFF, 0x40, 0xA7, 0xE3), new SolidColorBrush(Color.FromArgb(0xFF, 0x40, 0xA7, 0xE3))) },
            { "MessageReactionChosenForegroundBrush", (Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF))) },
        };

        [ThreadStatic]
        private static Dictionary<string, (Color Color, SolidColorBrush Brush)> _dark;
        public static Dictionary<string, (Color Color, SolidColorBrush Brush)> Dark => _dark ??= new()
        {
            { "MessageForegroundBrush", (Color.FromArgb(0xFF, 0xF5, 0xF5, 0xF5), new SolidColorBrush(Color.FromArgb(0xFF, 0xF5, 0xF5, 0xF5))) },
            { "MessageForegroundLinkBrush", (Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF))) },
            { "MessageBackgroundBrush", (Color.FromArgb(0xFF, 0x18, 0x25, 0x33), new SolidColorBrush(Color.FromArgb(0xFF, 0x18, 0x25, 0x33))) },
            { "MessageSubtleLabelBrush", (Color.FromArgb(0xFF, 0x6D, 0x7F, 0x8F), new SolidColorBrush(Color.FromArgb(0xFF, 0x6D, 0x7F, 0x8F))) },
            { "MessageSubtleGlyphBrush", (Color.FromArgb(0xFF, 0x6D, 0x7F, 0x8F), new SolidColorBrush(Color.FromArgb(0xFF, 0x6D, 0x7F, 0x8F))) },
            { "MessageSubtleForegroundBrush", (Color.FromArgb(0xFF, 0x6D, 0x7F, 0x8F), new SolidColorBrush(Color.FromArgb(0xFF, 0x6D, 0x7F, 0x8F))) },
            { "MessageHeaderForegroundBrush", (Color.FromArgb(0xFF, 0x71, 0xBA, 0xFA), new SolidColorBrush(Color.FromArgb(0xFF, 0x71, 0xBA, 0xFA))) },
            { "MessageHeaderBorderBrush", (Color.FromArgb(0xFF, 0x42, 0x9B, 0xDB), new SolidColorBrush(Color.FromArgb(0xFF, 0x42, 0x9B, 0xDB))) },
            { "MessageMediaForegroundBrush", (Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF))) },
            { "MessageMediaBackgroundBrush", (Color.FromArgb(0xFF, 0x3F, 0x96, 0xD0), new SolidColorBrush(Color.FromArgb(0xFF, 0x3F, 0x96, 0xD0))) },
            { "MessageOverlayBackgroundBrush", (Color.FromArgb(0x54, 0x00, 0x00, 0x00), new SolidColorBrush(Color.FromArgb(0x54, 0x00, 0x00, 0x00))) },
            { "MessageCallForegroundBrush", (Color.FromArgb(0xFF, 0x49, 0xA2, 0xF0), new SolidColorBrush(Color.FromArgb(0xFF, 0x49, 0xA2, 0xF0))) },
            { "MessageCallMissedForegroundBrush", (Color.FromArgb(0xFF, 0xED, 0x50, 0x50), new SolidColorBrush(Color.FromArgb(0xFF, 0xED, 0x50, 0x50))) },
            { "MessageReactionBackgroundBrush", (Color.FromArgb(0xFF, 0x3A, 0x47, 0x54), new SolidColorBrush(Color.FromArgb(0xFF, 0x3A, 0x47, 0x54))) },
            { "MessageReactionForegroundBrush", (Color.FromArgb(0xFF, 0x67, 0xBB, 0xF3), new SolidColorBrush(Color.FromArgb(0xFF, 0x67, 0xBB, 0xF3))) },
            { "MessageReactionChosenBackgroundBrush", (Color.FromArgb(0xFF, 0x6E, 0xB2, 0xEE), new SolidColorBrush(Color.FromArgb(0xFF, 0x6E, 0xB2, 0xEE))) },
            { "MessageReactionChosenForegroundBrush", (Color.FromArgb(0xFF, 0x33, 0x39, 0x3F), new SolidColorBrush(Color.FromArgb(0xFF, 0x33, 0x39, 0x3F))) },
        };

        public ThemeIncoming()
        {
            var light = new ResourceDictionary();
            var dark = new ResourceDictionary();

            foreach (var item in Light)
            {
                light[item.Key] = item.Value.Brush;
            }

            foreach (var item in Dark)
            {
                dark[item.Key] = item.Value.Brush;
            }

            ThemeDictionaries["Light"] = light;
            ThemeDictionaries["Default"] = dark;
        }

        public static void Update(TelegramTheme parent, IDictionary<string, Color> values = null)
        {
            if (values == null)
            {
                Update(parent);
                return;
            }

            var target = parent == TelegramTheme.Dark ? Dark : Light;

            foreach (var value in target)
            {
                var key = value.Key.Substring(0, value.Key.Length - "Brush".Length);
                if (values.TryGetValue($"{key}Incoming", out Color color))
                {
                    value.Value.Brush.Color = color;
                }
                else
                {
                    value.Value.Brush.Color = value.Value.Color;
                }
            }
        }

        public static void Update(TelegramTheme parent)
        {
            if (parent == TelegramTheme.Light)
            {
                foreach (var value in Light)
                {
                    value.Value.Brush.Color = value.Value.Color;
                }
            }
            else
            {
                foreach (var value in Dark)
                {
                    value.Value.Brush.Color = value.Value.Color;
                }
            }
        }
    }
}
