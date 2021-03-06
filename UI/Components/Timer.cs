﻿using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class Timer : IComponent
    {
        public SimpleLabel BigTextLabel { get; set; }
        public SimpleLabel SmallTextLabel { get; set; }
        protected SimpleLabel BigMeasureLabel { get; set; }
        protected ShortTimeFormatter Formatter { get; set; }

        protected Font TimerDecimalPlacesFont { get; set; }
        protected Font TimerFont { get; set; }
        protected float PreviousDecimalsSize { get; set; }

        protected String CurrentFormat { get; set; }
        protected TimeAccuracy CurrentAccuracy { get; set; }
        protected TimeFormat CurrentTimeFormat { get; set; }

        public GraphicsCache Cache { get; set; }

        public TimerSettings Settings { get; set; }
        public float ActualWidth { get; set; }

        public string ComponentName
        {
            get { return "Timer"; }
        }

        public float VerticalHeight
        {
            get { return Settings.TimerHeight; }
        }

        public float MinimumWidth
        {
            get { return 20; }
        }

        public float HorizontalWidth
        {
            get { return Settings.TimerWidth; }
        }

        public float MinimumHeight
        {
            get { return 20; }
        }

        public float PaddingTop { get { return 0f; } }
        public float PaddingLeft { get { return 7f; } }
        public float PaddingBottom { get { return 0f; } }
        public float PaddingRight { get { return 7f; } }

        public IDictionary<string, Action> ContextMenuControls
        {
            get { return null; }
        }

        public Timer()
        {
            BigTextLabel = new SimpleLabel()
            {
                HorizontalAlignment = StringAlignment.Far,
                VerticalAlignment = StringAlignment.Near,
                Width = 493,
                Text = "0",
            };

            SmallTextLabel = new SimpleLabel()
            {
                HorizontalAlignment = StringAlignment.Near,
                VerticalAlignment = StringAlignment.Near,
                Width = 257,
                Text = "0",
            };


            BigMeasureLabel = new SimpleLabel()
            {
                Text = "88:88:88",
                IsMonospaced = true
            };

            Formatter = new ShortTimeFormatter();
            Settings = new TimerSettings();
            UpdateTimeFormat();
            Cache = new GraphicsCache();
        }

        private void DrawGeneral(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.ToArgb() != Color.Transparent.ToArgb()
            || Settings.BackgroundGradient != GradientType.Plain
            && Settings.BackgroundColor2.ToArgb() != Color.Transparent.ToArgb())
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }

            if (state.LayoutSettings.TimerFont != TimerFont || Settings.DecimalsSize != PreviousDecimalsSize)
            {
                TimerFont = state.LayoutSettings.TimerFont;
                TimerDecimalPlacesFont = new Font(TimerFont.FontFamily.Name, (TimerFont.Size / 50f) * (Settings.DecimalsSize), TimerFont.Style, GraphicsUnit.Pixel);
                PreviousDecimalsSize = Settings.DecimalsSize;
            }

            BigTextLabel.Font = BigMeasureLabel.Font = TimerFont;
            SmallTextLabel.Font = TimerDecimalPlacesFont;

            BigMeasureLabel.SetActualWidth(g);
            SmallTextLabel.SetActualWidth(g);

            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            var oldMatrix = g.Transform;
            var unscaledWidth = (float)(Math.Max(10, BigMeasureLabel.ActualWidth + SmallTextLabel.ActualWidth + 11));
            var unscaledHeight = 45f;
            var widthFactor = (width - 14) / (unscaledWidth - 14);
            var heightFactor = height / unscaledHeight;
            var adjustValue = !Settings.CenterTimer ? 7f : 0f;
            var scale = Math.Min(widthFactor, heightFactor);
            g.TranslateTransform(width - adjustValue, height / 2);
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-unscaledWidth + adjustValue, -0.5f * unscaledHeight);
            if (Settings.CenterTimer)
                g.TranslateTransform((-(width - unscaledWidth * scale) / 2f) / scale, 0);
            DrawUnscaled(g, state, unscaledWidth, unscaledHeight);
            ActualWidth = scale * (SmallTextLabel.ActualWidth + BigTextLabel.ActualWidth);
            g.Transform = oldMatrix;
            if (!state.LayoutSettings.AntiAliasing)
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }

        public void DrawUnscaled(Graphics g, LiveSplitState state, float width, float height)
        {
            BigTextLabel.ShadowColor = state.LayoutSettings.ShadowsColor;
            SmallTextLabel.ShadowColor = state.LayoutSettings.ShadowsColor;
            BigTextLabel.HasShadow
                = SmallTextLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            if (CurrentFormat != Settings.TimerFormat)
                UpdateTimeFormat();

            var smallFont = TimerDecimalPlacesFont;
            var bigFont = TimerFont;
            var sizeMultiplier = bigFont.Size / bigFont.FontFamily.GetEmHeight(bigFont.Style);
            var smallSizeMultiplier = smallFont.Size / bigFont.FontFamily.GetEmHeight(bigFont.Style);
            var ascent = sizeMultiplier * bigFont.FontFamily.GetCellAscent(bigFont.Style);
            var descent = sizeMultiplier * bigFont.FontFamily.GetCellDescent(bigFont.Style);
            var smallAscent = smallSizeMultiplier * smallFont.FontFamily.GetCellAscent(smallFont.Style);
            var shift = (height - ascent - descent) / 2f;

            BigTextLabel.X = width - 499 - SmallTextLabel.ActualWidth;
            SmallTextLabel.X = width - SmallTextLabel.ActualWidth - 6;
            BigTextLabel.Y = shift;
            SmallTextLabel.Y = shift + ascent - smallAscent;
            BigTextLabel.Height = 150f;
            SmallTextLabel.Height = 150f;

            BigTextLabel.IsMonospaced = true;
            SmallTextLabel.IsMonospaced = true;

            if (Settings.ShowGradient && BigTextLabel.Brush is SolidBrush)
            {
                var originalColor = (BigTextLabel.Brush as SolidBrush).Color;
                double h, s, v;
                originalColor.ToHSV(out h, out s, out v);

                var bottomColor = ColorExtensions.FromHSV(h, s, 0.8 * v);
                var topColor = ColorExtensions.FromHSV(h, 0.5 * s, Math.Min(1, 1.5 * v + 0.1));

                var bigTimerGradiantBrush = new LinearGradientBrush(
                    new PointF(BigTextLabel.X, BigTextLabel.Y),
                    new PointF(BigTextLabel.X, BigTextLabel.Y + ascent + descent),
                    topColor,
                    bottomColor);
                var smallTimerGradiantBrush = new LinearGradientBrush(
                    new PointF(SmallTextLabel.X, SmallTextLabel.Y),
                    new PointF(SmallTextLabel.X, SmallTextLabel.Y + ascent + descent + smallFont.Size - bigFont.Size),
                    topColor,
                    bottomColor);

                BigTextLabel.Brush = bigTimerGradiantBrush;
                SmallTextLabel.Brush = smallTimerGradiantBrush;
            }

            BigTextLabel.Draw(g);
            SmallTextLabel.Draw(g);
        }

        protected void UpdateTimeFormat()
        {
            CurrentFormat = Settings.TimerFormat;
            if (CurrentFormat == "1.23")
            {
                CurrentTimeFormat = TimeFormat.Seconds;
                CurrentAccuracy = TimeAccuracy.Hundredths;
            }
            else if (CurrentFormat == "1.2")
            {
                CurrentTimeFormat = TimeFormat.Seconds;
                CurrentAccuracy = TimeAccuracy.Tenths;
            }
            else if (CurrentFormat == "1")
            {
                CurrentTimeFormat = TimeFormat.Seconds;
                CurrentAccuracy = TimeAccuracy.Seconds;
            }
            else if (CurrentFormat == "00:01.23")
            {
                CurrentTimeFormat = TimeFormat.Minutes;
                CurrentAccuracy = TimeAccuracy.Hundredths;
            }
            else if (CurrentFormat == "00:01.2")
            {
                CurrentTimeFormat = TimeFormat.Minutes;
                CurrentAccuracy = TimeAccuracy.Tenths;
            }
            else if (CurrentFormat == "00:01")
            {
                CurrentTimeFormat = TimeFormat.Minutes;
                CurrentAccuracy = TimeAccuracy.Seconds;
            }
            else if (CurrentFormat == "0:00:01.23")
            {
                CurrentTimeFormat = TimeFormat.Hours;
                CurrentAccuracy = TimeAccuracy.Hundredths;
            }
            else if (CurrentFormat == "0:00:01.2")
            {
                CurrentTimeFormat = TimeFormat.Hours;
                CurrentAccuracy = TimeAccuracy.Tenths;
            }
            else if (CurrentFormat == "0:00:01")
            {
                CurrentTimeFormat = TimeFormat.Hours;
                CurrentAccuracy = TimeAccuracy.Seconds;
            }
        }

        public virtual TimeSpan? GetTime(LiveSplitState state, TimingMethod method)
        {
            if (state.CurrentPhase == TimerPhase.NotRunning)
                return state.Run.Offset;
            else
                return state.CurrentTime[method];
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawGeneral(g, state, width, VerticalHeight);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawGeneral(g, state, HorizontalWidth, height);
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            Cache.Restart();

            var timingMethod = state.CurrentTimingMethod;
            if (Settings.TimingMethod == "Real Time")
                timingMethod = TimingMethod.RealTime;
            else if (Settings.TimingMethod == "Game Time")
                timingMethod = TimingMethod.GameTime;

            var timeValue = GetTime(state, timingMethod);
            if (timeValue != null)
            {
                var timeString = Formatter.Format(timeValue, CurrentTimeFormat);
                int dotIndex = timeString.IndexOf(".");
                BigTextLabel.Text = timeString.Substring(0, dotIndex);
                if (CurrentAccuracy == TimeAccuracy.Hundredths)
                    SmallTextLabel.Text = timeString.Substring(dotIndex);
                else if (CurrentAccuracy == TimeAccuracy.Tenths)
                    SmallTextLabel.Text = timeString.Substring(dotIndex, 2);
                else
                    SmallTextLabel.Text = "";
            }
            else
            {
                SmallTextLabel.Text = "-";
                BigTextLabel.Text = "";
            }

            if (Settings.OverrideSplitColors)
            {
                BigTextLabel.ForeColor = Settings.TimerColor;
                SmallTextLabel.ForeColor = Settings.TimerColor;
            }
            else if (state.CurrentPhase == TimerPhase.NotRunning || state.CurrentTime[timingMethod] < TimeSpan.Zero)
            {
                BigTextLabel.ForeColor = state.LayoutSettings.NotRunningColor;
                SmallTextLabel.ForeColor = state.LayoutSettings.NotRunningColor;
            }
            else if (state.CurrentPhase == TimerPhase.Paused)
            {
                BigTextLabel.ForeColor = SmallTextLabel.ForeColor = state.LayoutSettings.PausedColor;
            }
            else if (state.CurrentPhase == TimerPhase.Ended)
            {
                if (state.Run.Last().Comparisons[state.CurrentComparison][timingMethod] == null || state.CurrentTime[timingMethod] < state.Run.Last().Comparisons[state.CurrentComparison][timingMethod])
                {
                    BigTextLabel.ForeColor = state.LayoutSettings.PersonalBestColor;
                    SmallTextLabel.ForeColor = state.LayoutSettings.PersonalBestColor;
                }
                else
                {
                    BigTextLabel.ForeColor = state.LayoutSettings.BehindLosingTimeColor;
                    SmallTextLabel.ForeColor = state.LayoutSettings.BehindLosingTimeColor;
                }
            }
            else if (state.CurrentPhase == TimerPhase.Running)
            {
                Color timerColor;
                if (state.CurrentSplit.Comparisons[state.CurrentComparison][timingMethod] != null)
                {
                    timerColor = LiveSplitStateHelper.GetSplitColor(state, state.CurrentTime[timingMethod] - state.CurrentSplit.Comparisons[state.CurrentComparison][timingMethod],
                        state.CurrentSplitIndex, true, false, state.CurrentComparison, timingMethod).Value;
                }
                else
                    timerColor = state.LayoutSettings.AheadGainingTimeColor;
                BigTextLabel.ForeColor = timerColor;
                SmallTextLabel.ForeColor = timerColor;
            }

            Cache["TimerText"] = BigTextLabel.Text + SmallTextLabel.Text;
            if (BigTextLabel.Brush != null && invalidator != null)
            {
                Cache["TimerColor"] = BigTextLabel.ForeColor.ToArgb();
            }

            if (invalidator != null && Cache.HasChanged)
            {
                invalidator.Invalidate(0, 0, width, height);
            }
        }

        public void Dispose()
        {
        }
    }
}
