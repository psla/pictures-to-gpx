using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace PicturesToGpx
{
    public static class TelemetryOverlayRenderer
    {
        private const string PreferredFontFamily = "Segoe UI";
        private const string FallbackFontFamily = "Arial";

        /// <summary>
        /// Renders a modern, translucent telemetry bar across the bottom of the canvas.
        /// </summary>
        public static void DrawBottomBar(
            Graphics graphics,
            int canvasWidth,
            int canvasHeight,
            DateTimeOffset? dateTime,
            double? totalDistanceMeters,
            Color? dayColor = null,
            string dayText = null)
        {
            if (graphics == null || canvasWidth <= 0 || canvasHeight <= 0)
            {
                return;
            }

            if (!dateTime.HasValue && !totalDistanceMeters.HasValue && !dayColor.HasValue && string.IsNullOrEmpty(dayText))
            {
                return;
            }

            int barHeight = Math.Max(40, (int)(canvasHeight * 0.05));
            int barY = canvasHeight - barHeight;
            int marginX = Math.Max(24, (int)(canvasWidth * 0.02));

            var prevSmoothing = graphics.SmoothingMode;
            var prevTextRendering = graphics.TextRenderingHint;

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            try
            {
                // 1. Background Bar & Top Divider
                using (var bgBrush = new SolidBrush(Color.FromArgb(210, 15, 20, 28)))
                {
                    graphics.FillRectangle(bgBrush, 0, barY, canvasWidth, barHeight);
                }

                using (var borderPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
                {
                    graphics.DrawLine(borderPen, 0, barY, canvasWidth, barY);
                }

                string fontName = GetFontFamilyName();
                float regularFontSize = Math.Max(12f, barHeight * 0.34f);
                float boldFontSize = Math.Max(14f, barHeight * 0.42f);
                float unitFontSize = Math.Max(11f, barHeight * 0.30f);

                using (var regularFont = new Font(fontName, regularFontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var boldFont = new Font(fontName, boldFontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var unitFont = new Font(fontName, unitFontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var whiteBrush = new SolidBrush(Color.FromArgb(245, 245, 250)))
                using (var mutedBrush = new SolidBrush(Color.FromArgb(200, 210, 225)))
                {
                    // 2. Left Section: Local Date & Time (with minutes)
                    if (dateTime.HasValue)
                    {
                        string dateStr = dateTime.Value.ToString("ddd, d MMM  •  HH:mm");
                        var dateSize = graphics.MeasureString(dateStr, regularFont);
                        float dateY = barY + (barHeight - dateSize.Height) / 2f;
                        graphics.DrawString(dateStr, regularFont, whiteBrush, marginX, dateY);
                    }

                    // 3. Center Section: Active Day Accent Dot & Stage / Day Text
                    if (dayColor.HasValue || !string.IsNullOrEmpty(dayText))
                    {
                        float dotRadius = barHeight * 0.14f;
                        float dotDiameter = dotRadius * 2f;
                        float spacing = 8f;

                        SizeF textSize = SizeF.Empty;
                        if (!string.IsNullOrEmpty(dayText))
                        {
                            textSize = graphics.MeasureString(dayText, regularFont);
                        }

                        float totalCenterWidth = 0f;
                        if (dayColor.HasValue)
                        {
                            totalCenterWidth += dotDiameter;
                        }
                        if (dayColor.HasValue && !string.IsNullOrEmpty(dayText))
                        {
                            totalCenterWidth += spacing;
                        }
                        if (!string.IsNullOrEmpty(dayText))
                        {
                            totalCenterWidth += textSize.Width;
                        }

                        float currentX = (canvasWidth - totalCenterWidth) / 2f;

                        if (dayColor.HasValue)
                        {
                            float dotY = barY + (barHeight - dotDiameter) / 2f;
                            using (var dotBrush = new SolidBrush(dayColor.Value))
                            {
                                graphics.FillEllipse(dotBrush, currentX, dotY, dotDiameter, dotDiameter);
                            }
                            currentX += dotDiameter + spacing;
                        }

                        if (!string.IsNullOrEmpty(dayText))
                        {
                            float textY = barY + (barHeight - textSize.Height) / 2f;
                            graphics.DrawString(dayText, regularFont, mutedBrush, currentX, textY);
                        }
                    }

                    // 4. Right Section: Integer Cumulative Distance
                    if (totalDistanceMeters.HasValue)
                    {
                        int totalKm = (int)Math.Round(totalDistanceMeters.Value / 1000.0);
                        string numStr = string.Format("{0:N0}", totalKm);
                        string unitStr = " km";

                        var numSize = graphics.MeasureString(numStr, boldFont);
                        var unitSize = graphics.MeasureString(unitStr, unitFont);

                        float totalDistWidth = numSize.Width + unitSize.Width;
                        float rightX = canvasWidth - marginX - totalDistWidth;

                        float numY = barY + (barHeight - numSize.Height) / 2f;
                        float unitY = barY + (barHeight - unitSize.Height) / 2f + (numSize.Height - unitSize.Height) * 0.2f;

                        graphics.DrawString(numStr, boldFont, whiteBrush, rightX, numY);
                        graphics.DrawString(unitStr, unitFont, mutedBrush, rightX + numSize.Width, unitY);
                    }
                }
            }
            finally
            {
                graphics.SmoothingMode = prevSmoothing;
                graphics.TextRenderingHint = prevTextRendering;
            }
        }

        private static string GetFontFamilyName()
        {
            try
            {
                using (var testFont = new Font(PreferredFontFamily, 12, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    if (testFont.Name == PreferredFontFamily)
                    {
                        return PreferredFontFamily;
                    }
                }
            }
            catch
            {
                // Fallback to standard Arial if PreferredFontFamily cannot be instantiated
            }

            return FallbackFontFamily;
        }
    }
}
