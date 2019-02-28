using System.Drawing;

namespace SVN.Drawing
{
    public static class Extensions
    {
        public static double Brightness(this Color param)
        {
            var value = param.R + param.G + param.B;
            var valueMax = byte.MaxValue * 3;
            return (double)value / valueMax;
        }
    }
}