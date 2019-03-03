using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SVN.Drawing.Helpers
{
    public class ImageData : IDisposable
    {
        private const double SCORE_BRIGHTNESS = 10;
        private const double SCORE_NOISE = 20;

        private Bitmap Bitmap { get; set; }
        public double Brightness { get; private set; }
        public double[] Brightnesses { get; private set; }
        public double[] BrightnessesHorizontal { get; private set; }
        public double[] BrightnessesVertical { get; private set; }
        public double Noise { get; private set; }
        public double[] Noises { get; private set; }
        public double[] NoisesHorizontal { get; private set; }
        public double[] NoisesVertical { get; private set; }

        public int Width
        {
            get => this.Bitmap.Width;
        }

        public int Height
        {
            get => this.Bitmap.Height;
        }

        public double BrightnessHorizontalDiff
        {
            get
            {
                var min = this.BrightnessesHorizontal.Min();
                var max = this.BrightnessesHorizontal.Max();
                var diff = max - min;
                return diff;
            }
        }

        public double BrightnessVerticalDiff
        {
            get
            {
                var min = this.BrightnessesVertical.Min();
                var max = this.BrightnessesVertical.Max();
                var diff = max - min;
                return diff;
            }
        }

        public double BrightnessTopToBottomDiff
        {
            get
            {
                var top = this.BrightnessesVertical.First();
                var bot = this.BrightnessesVertical.Last();
                var diff = top - bot + 1;
                return diff;
            }
        }

        public double BrightnessBottomToTopDiff
        {
            get
            {
                var top = this.BrightnessesVertical.First();
                var bot = this.BrightnessesVertical.Last();
                var diff = bot - top + 1;
                return diff;
            }
        }

        public double NoisesHorizontalDiff
        {
            get
            {
                var min = this.NoisesHorizontal.Min();
                var max = this.NoisesHorizontal.Max();
                var diff = max - min;
                return diff;
            }
        }

        public double NoisesVerticalDiff
        {
            get
            {
                var min = this.NoisesVertical.Min();
                var max = this.NoisesVertical.Max();
                var diff = max - min;
                return diff;
            }
        }

        public double NoisesTopToBottomDiff
        {
            get
            {
                var top = this.NoisesVertical.First();
                var bot = this.NoisesVertical.Last();
                var diff = top - bot + 1;
                return diff;
            }
        }

        public double NoisesBottomToTopDiff
        {
            get
            {
                var top = this.NoisesVertical.First();
                var bot = this.NoisesVertical.Last();
                var diff = bot - top + 1;
                return diff;
            }
        }

        public double ScoreRotateNone
        {
            get
            {
                var result = default(double);
                result += this.BrightnessVerticalDiff * ImageData.SCORE_BRIGHTNESS;
                result += this.NoisesVerticalDiff * ImageData.SCORE_NOISE;
                return result;
            }
        }

        public double ScoreRotate90
        {
            get
            {
                var result = default(double);
                result += this.BrightnessHorizontalDiff * ImageData.SCORE_BRIGHTNESS;
                result += this.NoisesHorizontalDiff * ImageData.SCORE_NOISE;
                return result;
            }
        }

        public double ScoreFlipNone
        {
            get
            {
                var result = default(double);
                result += this.BrightnessTopToBottomDiff * ImageData.SCORE_BRIGHTNESS;
                result += this.NoisesBottomToTopDiff * ImageData.SCORE_NOISE;
                return result;
            }
        }

        public double ScoreFlipVertical
        {
            get
            {
                var result = default(double);
                result += this.BrightnessBottomToTopDiff * ImageData.SCORE_BRIGHTNESS;
                result += this.NoisesTopToBottomDiff * ImageData.SCORE_NOISE;
                return result;
            }
        }

        private ImageData(Bitmap bitmap)
        {
            this.Bitmap = bitmap;
            this.Bitmap.ExifRotate();
            this.CalculateValues();
        }

        public void Dispose()
        {
            this.Bitmap.Dispose();
            this.Bitmap = null;
        }

        public static ImageData FromBitmap(Bitmap bitmap)
        {
            return new ImageData(bitmap);
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

        private void CalculateValues()
        {
            this.Brightness = this.Bitmap.Brightness();
            this.Brightnesses = this.Bitmap.Brightnesses();
            this.BrightnessesHorizontal = this.Bitmap.BrightnessesHorizontal();
            this.BrightnessesVertical = this.Bitmap.BrightnessesVertical();
            this.Noise = this.Bitmap.Noise();
            this.Noises = this.Bitmap.Noises();
            this.NoisesHorizontal = this.Bitmap.NoisesHorizontal();
            this.NoisesVertical = this.Bitmap.NoisesVertical();
        }

        private void ApplyScoreRotation()
        {
            var rotateNone = this.ScoreRotateNone;
            var rotate90 = this.ScoreRotate90;

            if (rotateNone < rotate90)
            {
                this.Bitmap.Rotate90();
                this.CalculateValues();
            }
        }

        private void ApplyScoreFlip()
        {
            var flipNone = this.ScoreFlipNone;
            var flipVertical = this.ScoreFlipVertical;

            if (flipNone < flipVertical)
            {
                this.Bitmap.FlipVertical();
                this.CalculateValues();
            }
        }

        public void ApplyScore()
        {
            if (500 < this.Width || 500 < this.Height)
            {
                var size = new Size(500, 500);
                this.Bitmap.ApplyRatio(size);

                var bytes = this.Bitmap.ToBytes();
                bytes = bytes.Resize(size);

                this.Bitmap.Dispose();
                this.Bitmap = bytes.ToImage() as Bitmap;
            }

            this.ApplyScoreRotation();
            this.ApplyScoreFlip();
        }
    }
}