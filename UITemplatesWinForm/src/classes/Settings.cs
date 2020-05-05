﻿/***************************************************************************
 *                                                                         *
 *                                                                         *
 * Copyright(c) 2020, REGATA Experiment at FLNP|JINR                       *
 * Author: [Boris Rumyantsev](mailto:bdrum@jinr.ru)                        *
 * All rights reserved                                                     *
 *                                                                         *
 *                                                                         *
 ***************************************************************************/

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Collections.ObjectModel;

namespace Regata.UITemplates
{
    public enum Status { Info, Processing, Success, Canceled };
    public enum Languages { Russian, English };

    // NOTE: perhaps internal localization tools in winforms is better, but how I iderstood it doesn't allow to switch language during the runtime

    public class Settings
    {
        private static bool _isFirstreading = true;
        private readonly string _path;
        public static string AssemblyName;
        
        private Languages _currentLanguage;

        public Languages CurrentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                _currentLanguage = value;
                Labels.CurrentLanguage = _currentLanguage;
                LanguageChanged?.Invoke();
                SaveSettings();
            }
        }

        public event Action LanguageChanged;

        public ObservableCollection<string> NonDisplayedColumns { get; set; }

        private void ReadSettings()
        {
            try
            {
                if (_isFirstreading)
                {
                    _isFirstreading = false; // this fix stack overflow in json desirialize bellow
                    if (File.Exists(_path))
                    {
                        var options = new JsonSerializerOptions();
                        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                        var set = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path), options);
                        _currentLanguage = set.CurrentLanguage;
                        NonDisplayedColumns = set.NonDisplayedColumns;

                        if (NonDisplayedColumns == null)
                            NonDisplayedColumns = new ObservableCollection<string>();

                        NonDisplayedColumns.CollectionChanged += NonDisplayedColumns_CollectionChanged;
                    }
                    else
                        ResetFileSettings();
                }
            }
            catch (JsonException)
            {
                ResetFileSettings();
            }
        }

        private void NonDisplayedColumns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SaveSettings();
        }

        private void ResetFileSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            using (var f = File.CreateText(_path))
            { }
            
            CurrentLanguage = Languages.English;
            NonDisplayedColumns = new ObservableCollection<string>();
        }

        public Settings()
        {
            if (string.IsNullOrEmpty(AssemblyName)) throw new ArgumentNullException("You must specify name of calling assembly. Just use 'System.Reflection.Assembly.GetExecutingAssembly().GetName().Name' as argument.");
            _path = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Regata\\{AssemblyName}\\settings.json";
            ReadSettings();
        }

        public void SaveSettings()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.WriteIndented = true;
            File.WriteAllText(_path, JsonSerializer.Serialize(this, options));
        }
    }

    public abstract class Labels
    {
        public static Languages CurrentLanguage;

        #region ControlLabels

        public static string MenuItemMenu
        {
            get
            {
                switch (CurrentLanguage)
                {
                    case Languages.Russian:
                        return "Menu";
                    case Languages.English:
                        return "Menu";
                    default: return "";
                }
            }
        }

        public static string MenuItemMenuLang
        {
            get
            {
                switch (CurrentLanguage)
                {
                    case Languages.Russian:
                        return "Language";
                    case Languages.English:
                        return "Language";
                    default: return "";
                }
            }
        }

        public static string MenuItemMenuLangRus
        {
            get
            {
                switch (CurrentLanguage)
                {
                    case Languages.Russian:
                        return "Russian";
                    case Languages.English:
                        return "Russian";
                    default: return "";
                }
            }
        }

        public static string MenuItemMenuLangEng
        {
            get
            {
                switch (CurrentLanguage)
                {
                    case Languages.Russian:
                        return "English";
                    case Languages.English:
                        return "English";
                    default: return "";
                }
            }
        }

        public static string MenuItemViewShowColumns
        { 
            get
            {
                switch (CurrentLanguage)
                {
                    case Languages.Russian:
                        return "Показывать столбцы";
                    case Languages.English:
                        return "Show columns";
                    default:
                        return "Show columns";
                }
            }
        }

        public static string MenuItemView
        {
            get
            {
                switch (CurrentLanguage)
                {
                    case Languages.Russian:
                        return "Вид";
                    case Languages.English:
                        return "View";
                    default:
                        return "View";
                }
            }
        }
        #endregion

    }
}
