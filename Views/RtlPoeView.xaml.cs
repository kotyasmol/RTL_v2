using System;
using System.Windows;
using System.Windows.Controls;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using SkiaSharp.Extended;
using RTL.ViewModels;
using Svg.Skia;

namespace RTL.Views
{
    public partial class RtlPoeView : UserControl
    {
        private SKSvg _svg;

        public RtlPoeView()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                if (DataContext is RtlPoeViewModel vm)
                {
                    vm.SetLogListBox(LogListBox);
                }

                LoadSvg();
            };
        }

        private void LoadSvg()
        {
            _svg = new SKSvg();

            var uri = new Uri("pack://application:,,,/TFortisBoard;component/Resources/Images/PlateImages/rlt_poe_v2.svg");
            using var stream = Application.GetResourceStream(uri).Stream;
            _svg.Load(stream);

            SvgCanvas.InvalidateVisual(); // Перерисовать холст
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White); // фон

            if (_svg?.Picture != null)
            {
                var canvasSize = e.Info.Rect;
                var scale = Math.Min(canvasSize.Width / _svg.Picture.CullRect.Width,
                                     canvasSize.Height / _svg.Picture.CullRect.Height);
                var matrix = SKMatrix.CreateScale(scale, scale);
                canvas.DrawPicture(_svg.Picture, ref matrix);
            }
        }
    }
}
