using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WinIsland
{
    public partial class MainWindow
    {
        private bool _isFileStationActive = false;
        private List<string> _storedFiles = new List<string>();

        private void DynamicIsland_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
                _isFileStationActive = true;
                EnterFileStationMode(dragging: true);
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DynamicIsland_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            e.Handled = true;
        }

        private void DynamicIsland_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            if (_storedFiles.Count == 0 && !IsMouseOver)
            {
                _isFileStationActive = false;
                CheckCurrentSession(); 
            }
            else if (_storedFiles.Count > 0)
            {
                EnterFileStationMode(dragging: false);
            }
        }

        private void DynamicIsland_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    _storedFiles.AddRange(files);
                    UpdateFileStationUI();
                    PlaySuckInSequence();
                }
            }
        }

        private void PlaySuckInSequence()
        {
            var consumeAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.5 }
            };

            PlayIslandGlowEffect(Colors.Purple);

            consumeAnim.Completed += (s, ev) =>
            {
                _isFileStationActive = (_storedFiles.Count > 0);
                EnterFileStationMode(dragging: false);
            };

            BlackHoleScale.BeginAnimation(ScaleTransform.ScaleXProperty, consumeAnim);
            BlackHoleScale.BeginAnimation(ScaleTransform.ScaleYProperty, consumeAnim);
        }

        private void EnterFileStationMode(bool dragging)
        {
            HideAllPanels();

            FileStationPanel.Visibility = Visibility.Visible;
            DynamicIsland.Opacity = 1.0;
            SetClickThrough(false);

            if (dragging)
            {
                _widthSpring.Target = 150;
                _heightSpring.Target = 150;

                DropHintText.Opacity = 1;
                FileStackDisplay.Visibility = Visibility.Collapsed;

                var scaleAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400)) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                BlackHoleScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                BlackHoleScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

                var spinAnim = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(2)) { RepeatBehavior = RepeatBehavior.Forever };
                VortexRotation.BeginAnimation(RotateTransform.AngleProperty, spinAnim);
            }
            else
            {
                ShowFileStationState();
            }
        }

        private void ShowFileStationState()
        {
            HideAllPanels();

            FileStationPanel.Visibility = Visibility.Visible;
            DropHintText.Opacity = 0;

            BlackHoleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            BlackHoleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            BlackHoleScale.ScaleX = 0;
            BlackHoleScale.ScaleY = 0;

            FileStackDisplay.Visibility = Visibility.Visible;
            UpdateFileStationUI();

            _widthSpring.Target = 100;
            _heightSpring.Target = 35;
            DynamicIsland.Opacity = 1.0;
            SetClickThrough(false);
        }

        private void UpdateFileStationUI()
        {
            FileCountText.Text = _storedFiles.Count.ToString();
        }

        private void PlayBlackHoleSuckAnimation()
        {
            PlayIslandGlowEffect(Colors.Purple);
        }

        private void FileStack_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_storedFiles.Count > 0)
            {
                var data = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, _storedFiles.ToArray());
                System.Windows.DragDrop.DoDragDrop(FileStackDisplay, data, System.Windows.DragDropEffects.Copy | System.Windows.DragDropEffects.Move);

                _storedFiles.Clear();
                _isFileStationActive = false;

                CheckCurrentSession();
            }
        }
    }
}
