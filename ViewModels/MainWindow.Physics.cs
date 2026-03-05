using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace WinIsland
{
    public partial class MainWindow
    {
        // 物理动画弹簧
        private Spring _widthSpring = null!;
        private Spring _heightSpring = null!;
        private DateTime _lastFrameTime;

        private void InitializePhysics()
        {
            _widthSpring = new Spring(120);
            _heightSpring = new Spring(35);
            _lastFrameTime = DateTime.Now;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (DynamicIsland == null) return;

            var now = DateTime.Now;
            var dt = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;
            if (dt > 0.05) dt = 0.05;

            var newWidth = _widthSpring.Update(dt);
            var newHeight = _heightSpring.Update(dt);

            // Optimization: Only update layout if values actually changed materially
            if (Math.Abs(DynamicIsland.Width - newWidth) > 0.1)
                DynamicIsland.Width = Math.Max(1, newWidth);
            
            if (Math.Abs(DynamicIsland.Height - newHeight) > 0.1)
                DynamicIsland.Height = Math.Max(1, newHeight);

            if (DynamicIsland.Height > 0)
                DynamicIsland.CornerRadius = new CornerRadius(DynamicIsland.Height / 2);

            if (Bar1 != null && VisualizerContainer.Visibility == Visibility.Visible)
                UpdateVisualizer();
        }

        // --- Common Animation Helpers ---

        private void PlayIslandGlowEffect(System.Windows.Media.Color glowColor)
        {
            if (DynamicIsland.Effect is DropShadowEffect shadow)
            {
                var colorAnim = new ColorAnimation(glowColor, TimeSpan.FromMilliseconds(300))
                {
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2),
                    FillBehavior = FillBehavior.Stop
                };

                var blurAnim = new DoubleAnimation(shadow.BlurRadius, 30, TimeSpan.FromMilliseconds(300))
                {
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(2),
                    FillBehavior = FillBehavior.Stop
                };

                shadow.BeginAnimation(DropShadowEffect.ColorProperty, colorAnim);
                shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
            }
        }

        private void PlayContentEntranceAnimation(FrameworkElement element)
        {
            TranslateTransform? translate = null;
            if (element.RenderTransform is TranslateTransform tt)
            {
                translate = tt;
            }
            else if (element.RenderTransform is TransformGroup tg)
            {
                foreach (var t in tg.Children)
                {
                    if (t is TranslateTransform transform)
                    {
                        translate = transform;
                    }
                }
            }

            if (translate == null)
            {
                translate = new TranslateTransform();
                element.RenderTransform = translate;
            }

            translate.Y = 20;
            element.Opacity = 0;

            var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(300))
            {
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slideUp = new DoubleAnimation(0, TimeSpan.FromMilliseconds(400))
            {
                BeginTime = TimeSpan.FromMilliseconds(150),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            translate.BeginAnimation(TranslateTransform.YProperty, slideUp);
        }

        private void PlayFlipAnimation()
        {
            var flipAnimation = new DoubleAnimationUsingKeyFrames();
            flipAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            flipAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            });
            flipAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1.2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 }
            });
            flipAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            var scaleYAnimation = new DoubleAnimationUsingKeyFrames();
            scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1.1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
            scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            NotificationIconScale.BeginAnimation(ScaleTransform.ScaleXProperty, flipAnimation);
            NotificationIconScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        private void PlayIslandPulseAnimation(System.Windows.Media.Color pulseColor)
        {
            var dropShadow = (DropShadowEffect)DynamicIsland.Effect;
            if (dropShadow == null) return;

            var colorAnim = new ColorAnimation(pulseColor, TimeSpan.FromMilliseconds(500));
            var blurAnim = new DoubleAnimation(10, 50, TimeSpan.FromMilliseconds(800));
            blurAnim.AutoReverse = true;
            blurAnim.RepeatBehavior = new RepeatBehavior(5);
            
            var opacityAnim = new DoubleAnimation(0.3, 0.8, TimeSpan.FromMilliseconds(800));
            opacityAnim.AutoReverse = true;
            opacityAnim.RepeatBehavior = new RepeatBehavior(5);

            blurAnim.Completed += (s, e) => StopIslandPulseAnimation();

            dropShadow.BeginAnimation(DropShadowEffect.ColorProperty, colorAnim);
            dropShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
            dropShadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
        }

        private void StopIslandPulseAnimation()
        {
            var dropShadow = (DropShadowEffect)DynamicIsland.Effect;
            if (dropShadow == null) return;

            dropShadow.BeginAnimation(DropShadowEffect.ColorProperty, null);
            dropShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
            dropShadow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
            
            dropShadow.Color = Colors.Black;
            dropShadow.BlurRadius = 10;
            dropShadow.Opacity = 0.3;
        }
    }
}
