﻿using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UnityLauncherPro
{
    public partial class ThemeEditor : Window
    {
        static ObservableCollection<ThemeColor> themeColors = new ObservableCollection<ThemeColor>();
        static ObservableCollection<ThemeColor> themeColorsOrig = new ObservableCollection<ThemeColor>();

        string previousSaveFileName = null;

        // hack for adjusting slider, without triggering onchange..
        bool forceValue = false;

        // for single undo
        Slider previousSlider;
        int previousValue = -1;

        public ThemeEditor()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            themeColors.Clear();
            themeColorsOrig.Clear();

            // get original colors to collection
            foreach (DictionaryEntry item in Application.Current.Resources.MergedDictionaries[0])
            {
                // take currently used colors
                var currentColor = (SolidColorBrush)Application.Current.Resources[item.Key];

                var themeColorPair = new ThemeColor();
                themeColorPair.Key = item.Key.ToString();
                themeColorPair.Brush = currentColor;
                themeColors.Add(themeColorPair);

                // take backup copy
                var themeColorPair2 = new ThemeColor();
                themeColorPair2.Key = item.Key.ToString();
                themeColorPair2.Brush = currentColor;
                themeColorsOrig.Add(themeColorPair2);
            }
            // display current theme keys and values
            gridThemeColors.ItemsSource = themeColors;

            // sort by key sa default
            gridThemeColors.Items.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));

            gridThemeColors.SelectedIndex = 0;
        }

        void UpdateColorPreview()
        {
            var newColor = new Color();
            newColor.R = (byte)sliderRed.Value;
            newColor.G = (byte)sliderGreen.Value;
            newColor.B = (byte)sliderBlue.Value;
            newColor.A = (byte)sliderAlpha.Value;
            var newColorBrush = new SolidColorBrush(newColor);
            rectSelectedColor.Fill = newColorBrush;

            // set new color into our collection values
            themeColors[themeColors.IndexOf((ThemeColor)gridThemeColors.SelectedItem)].Brush = newColorBrush;

            gridThemeColors.Items.Refresh();

            // apply color changes to mainwindow
            var item = gridThemeColors.SelectedItem as ThemeColor;
            Application.Current.Resources[item.Key] = newColorBrush;
            forceValue = false;
        }

        void SetSlider(Slider target, double color)
        {
            forceValue = true;
            target.Value = color;
            forceValue = false;
        }

        private void GridThemeColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridThemeColors.SelectedIndex == -1) return;

            var item = gridThemeColors.SelectedItem as ThemeColor;
            if (item == null) return;

            // update preview box
            rectSelectedColor.Fill = item.Brush;

            // update RGBA sliders
            SetSlider(sliderRed, item.Brush.Color.R);
            SetSlider(sliderGreen, item.Brush.Color.G);
            SetSlider(sliderBlue, item.Brush.Color.B);
            SetSlider(sliderAlpha, item.Brush.Color.A);
        }

        private void BtnSaveTheme_Click(object sender, RoutedEventArgs e)
        {
            // 1) Determine the default filename (with .ini)
            string defaultName = string.IsNullOrEmpty(previousSaveFileName)
                ? "custom.ini"
                : previousSaveFileName + ".ini";

            // 2) Ask the helper for a safe full path
            string initialFullPath = Tools.GetSafeFilePath("Themes", defaultName);
            string initialDir = Path.GetDirectoryName(initialFullPath);
            string initialFile = Path.GetFileNameWithoutExtension(initialFullPath);

            // 3) Configure the save dialog
            var saveFileDialog = new SaveFileDialog
            {
                FileName = initialFile,   // no extension here
                DefaultExt = ".ini",
                Filter = "Theme files (.ini)|*.ini",
                InitialDirectory = initialDir,
                RestoreDirectory = true
            };

            // 4) Show and, if confirmed, write out the INI
            if (saveFileDialog.ShowDialog() == true)
            {
                // Build INI lines
                var iniRows = new List<string>
        {
            "# Created with UnityLauncherPro built-in theme editor "
              + DateTime.Now.ToString("dd/MM/yyyy")
        };
                // original-style loop
                for (int i = 0; i < themeColors.Count; i++)
                {
                    iniRows.Add(themeColors[i].Key + "=" + themeColors[i].Brush.ToString());
                }

                // Get the chosen path & ensure its folder exists
                string themePath = saveFileDialog.FileName;
                previousSaveFileName = Path.GetFileNameWithoutExtension(themePath);
                Directory.CreateDirectory(Path.GetDirectoryName(themePath));

                // Write out
                File.WriteAllLines(themePath, iniRows);
                Console.WriteLine("Saved theme: " + themePath);
            }
        }

        private void BtnResetTheme_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < themeColorsOrig.Count; i++)
            {
                // reset collection colors
                themeColors[i].Brush = themeColorsOrig[i].Brush;

                // reset application colors
                Application.Current.Resources[themeColors[i].Key] = themeColorsOrig[i].Brush;
            }

            // reset current color
            if (gridThemeColors.SelectedItem != null)
            {
                var item = gridThemeColors.SelectedItem as ThemeColor;
                SetSlider(sliderRed, item.Brush.Color.R);
                SetSlider(sliderGreen, item.Brush.Color.G);
                SetSlider(sliderBlue, item.Brush.Color.B);
                SetSlider(sliderAlpha, item.Brush.Color.A);
            }

            UpdateColorPreview();
            gridThemeColors.Items.Refresh();
        }

        private void SliderRed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // onchanged is called before other components are ready..thanks wpf :D
            if (forceValue == true || txtRed == null) return;
            UpdateColorPreview();
        }

        private void SliderGreen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (forceValue == true || txtGreen == null) return;
            UpdateColorPreview();
        }

        private void SliderBlue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (forceValue == true || txtBlue == null) return;
            UpdateColorPreview();
        }

        public void Executed_Undo(object sender, ExecutedRoutedEventArgs e)
        {
            // restore previous color
            SetSlider(previousSlider, previousValue);
            UpdateColorPreview();
        }

        public void CanExecute_Undo(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = previousValue > -1;
        }

        private void SliderAlpha_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (forceValue == true) return;
            if (txtAlpha == null) return;
            UpdateColorPreview();
        }

        public void Executed_Save(object sender, ExecutedRoutedEventArgs e)
        {
            BtnSaveTheme_Click(null, null);
        }

        public void CanExecute_Save(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void SliderRed_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetUndoValues(sender, txtRed);
        }

        private void SliderGreen_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetUndoValues(sender, txtGreen);
        }

        private void SliderBlue_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetUndoValues(sender, txtBlue);
        }

        private void SliderAlpha_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetUndoValues(sender, txtAlpha);
        }

        void SetUndoValues(Object sender, TextBox textBox)
        {
            previousSlider = (Slider)sender;
            previousValue = (int)previousSlider.Value;
        }

        private void TxtRed_KeyUp(object sender, KeyEventArgs e)
        {
            GetColorFromTextBox((TextBox)sender, sliderRed);
        }

        private void TxtGreen_KeyUp(object sender, KeyEventArgs e)
        {
            GetColorFromTextBox((TextBox)sender, sliderGreen);
        }

        private void TxtBlue_KeyUp(object sender, KeyEventArgs e)
        {
            GetColorFromTextBox((TextBox)sender, sliderBlue);
        }

        private void TxtAlpha_KeyUp(object sender, KeyEventArgs e)
        {
            GetColorFromTextBox((TextBox)sender, sliderAlpha);
        }

        void GetColorFromTextBox(TextBox source, Slider target)
        {
            int col = 0;
            if (int.TryParse(source.Text, out col))
            {
                bool overWrite = false;
                if (col < 0) { col = 0; overWrite = true; }
                if (col > 255) { col = 255; overWrite = true; }

                source.Text = col + "";
                target.Value = col;

                if (overWrite == true) source.SelectAll();
            }
        }

        private void TxtColorField_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape: // undo current textbox edit
                    ((TextBox)sender).Undo();
                    break;
            }
        }
    } // class
} // namespace