using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace WinIsland
{
    public partial class FocusOverlayWindow : Window
    {
        private Storyboard _breathingStoryboard;

        public FocusOverlayWindow()
        {
            InitializeComponent();
            _breathingStoryboard = (Storyboard)FindResource("BreathingAnimation");
        }

        public event Action? OnScreenClicked;

        private void Window_MouseDown(object? sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnScreenClicked?.Invoke();
        }

        public void StartBreathing()
        {
            this.Show();
            _breathingStoryboard.Begin();
        }

        public void StopBreathing()
        {
            _breathingStoryboard.Stop();
            this.Hide();
        }
    }
}
