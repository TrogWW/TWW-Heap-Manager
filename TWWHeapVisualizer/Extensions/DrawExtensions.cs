using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.Extensions
{
    public static class DrawExtensions
    {
        public static Color Lighten(this Color color, int amount)
        {
            int r = Math.Min(255, Math.Max(0, color.R + amount));
            int g = Math.Min(255, Math.Max(0, color.G + amount));
            int b = Math.Min(255, Math.Max(0, color.B + amount));

            return Color.FromArgb(r, g, b);
        }
        public static List<Color> GetFurthestColors(int X)
        {
            List<Color> furthestColors = new List<Color>();

            // Start with an initial color (e.g., white)
            Color initialColor = Color.White;
            furthestColors.Add(initialColor);

            // While we haven't reached the desired number of colors
            while (furthestColors.Count < X)
            {
                double maxMinDistance = 0;
                Color bestColor = Color.Black;

                // Find the color with the maximum minimum distance to already selected colors
                foreach (Color candidateColor in GetCandidateColors(51,650))
                {
                    double minDistance = double.MaxValue;
                    foreach (Color selectedColor in furthestColors)
                    {
                        double distance = ColorDistance(candidateColor, selectedColor);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                        }
                    }

                    if (minDistance > maxMinDistance)
                    {
                        maxMinDistance = minDistance;
                        bestColor = candidateColor;
                    }
                }

                // Add the best color to the list
                furthestColors.Add(bestColor);
            }

            return furthestColors;
        }

        // Generate a set of candidate colors (we can optimize this)
        private static IEnumerable<Color> GetCandidateColors(int skip, int minBrightness)
        {
            for (int r = 0; r <= 255; r += skip) // Skip by 51 to reduce the number of candidates
            {
                for (int g = 0; g <= 255; g += skip)
                {
                    for (int b = 0; b <= 255; b += skip)
                    {
                        // Ensure that the color is bright enough to be visible against black text
                        if ((r + g + b) > minBrightness) // Adjust as needed
                        {
                            yield return Color.FromArgb(r, g, b);
                        }
                    }
                }
            }
        }

        // Calculate the Euclidean distance between two RGB colors
        public static double ColorDistance(Color color1, Color color2)
        {
            double dr = color1.R - color2.R;
            double dg = color1.G - color2.G;
            double db = color1.B - color2.B;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}
