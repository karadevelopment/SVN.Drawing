using System;
using System.Drawing;
using System.IO;

namespace SVN.Drawing.Helpers
{
    public class ImageData : IDisposable
    {
        private Bitmap Bitmap { get; set; }

        public int Width
        {
            get => this.Bitmap.Width;
        }

        public int Height
        {
            get => this.Bitmap.Height;
        }

        public double Brightness
        {
            get => this.Bitmap.Brightness();
        }

        public double[] Brightnesses
        {
            get => this.Bitmap.Brightnesses();
        }

        public double Noise
        {
            get => this.Bitmap.Noise();
        }

        public double[] Noises
        {
            get => this.Bitmap.Noises();
        }

        public double[] NoisesHorizontal
        {
            get => this.Bitmap.NoisesHorizontal();
        }

        public double[] NoisesVertical
        {
            get => this.Bitmap.NoisesVertical();
        }

        private ImageData()
        {
        }

        public void Dispose()
        {
            this.Bitmap.Dispose();
            this.Bitmap = null;
        }

        public static ImageData FromBitmap(Bitmap bitmap)
        {
            return new ImageData
            {
                Bitmap = bitmap,
            };
        }

        public static ImageData FromFile(string path)
        {
            return ImageData.FromBitmap(new Bitmap(path));
        }

        public void ToFile(string path)
        {
            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            this.Bitmap.Save(path);
        }

        public void ApplyConvolutionFilter(double factor = 1, int bias = 0, bool grayscale = true)
        {
            var bitmap1 = this.Bitmap;
            
            using (var bitmap2 = new Bitmap(bitmap1))
            {
                this.Bitmap = bitmap2.ConvolutionFilter(factor, bias, grayscale);
            }

            bitmap1.Dispose();
        }
    }
}