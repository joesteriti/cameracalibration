using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CameraCalibration
{
    public class Focus
    {
        public static List<Mat> images = new List<Mat>();

        public Focus(List<string> imagePaths)
        {
            Console.WriteLine("Focus: Initializing and loading images...");
            images = new List<Mat>();
            int idx = 0;
            foreach (var path in imagePaths)
            {
                idx++;
                Console.WriteLine($"Loading image {idx}: {path}");
                var img = CvInvoke.Imread(path, ImreadModes.ColorBgr);
                if (img == null || img.IsEmpty)
                {
                    Console.WriteLine($"Warning: Could not load image {idx} at path: {path}");
                    continue;
                }
                images.Add(img);
                Console.WriteLine($"Loaded image {idx}: size = {img.Width}x{img.Height}");
            }

            Console.WriteLine($"Focus: Finished loading. Successfully loaded {images.Count} images.");
            if (images.Count == 0)
            {
                Console.WriteLine("Focus: No valid images loaded. Ensure paths are correct.");
            }
        }
        //Another method (currently NOT used) to measure focus
        public static double getLocalVariance(Mat img)
        {
            using (var gray = new Mat())
            using (var grayF = new Mat())
            using (var mean = new Mat())
            using (var graySquared = new Mat())
            using (var squaredMean = new Mat())
            using (var meanPow = new Mat())
            using (var variance = new Mat())
            {
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
                gray.ConvertTo(grayF, DepthType.Cv32F);

                int ksize = 5;
                CvInvoke.Blur(grayF, mean, new System.Drawing.Size(ksize, ksize), new System.Drawing.Point(-1, -1));
                CvInvoke.Multiply(grayF, grayF, graySquared);
                CvInvoke.Blur(graySquared, squaredMean, new System.Drawing.Size(ksize, ksize), new System.Drawing.Point(-1, -1));
                CvInvoke.Multiply(mean, mean, meanPow);
                CvInvoke.Subtract(squaredMean, meanPow, variance);

                MCvScalar meanVariance = CvInvoke.Mean(variance);
                double focusValue = meanVariance.V0;
                return focusValue;
            }
        }
        //Method (currently used) to measure focus
        public static double getSobelVarianceCombo(Mat img)
        {
            using (var gray = new Mat())
            using (var sobel_x = new Mat())
            using (var sobel_y = new Mat())
            using (var magnitude = new Mat())
            using (var angle = new Mat())
            {
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
                CvInvoke.Sobel(gray, sobel_x, DepthType.Cv64F, 1, 0, 3);
                CvInvoke.Sobel(gray, sobel_y, DepthType.Cv64F, 0, 1, 3);
                CvInvoke.CartToPolar(sobel_x, sobel_y, magnitude, angle, false);

                double meanMag = CvInvoke.Mean(magnitude).V0;

                // MeanStdDev requires Mat and outputs mean/stdDev as MCvScalar refs
                MCvScalar mean = new MCvScalar();
                MCvScalar stdDev = new MCvScalar();
                CvInvoke.MeanStdDev(gray, ref mean, ref stdDev);
                var variance = stdDev.V0 * stdDev.V0;

                double focusValue = meanMag * variance;
                return focusValue;
            }
        }

        public double focusAnalysis()
        {
            double focusValue = 0.0;
            if (images == null || images.Count == 0)
            {
                Console.WriteLine("Focus.run: No images available to analyze. Aborting.");
                return focusValue;
            }

            var methods = new Dictionary<string, Func<Mat, double>>()
            {
                //{"Local Variance", getLocalVariance},
                {"Sobel Variance Combo", getSobelVarianceCombo}
            };

            Console.WriteLine("Focus.run: Starting focus analysis for each method...");

            foreach (var method in methods)
            {
                List<double> focusValues = new List<double>();
                Console.WriteLine($"\n=== Running method: {method.Key} ===");
                for (int i = 0; i < images.Count; i++)
                {
                    var img = images[i];
                    if (img == null || img.IsEmpty)
                    {
                        Console.WriteLine($"Image {i + 1}: skipped (empty)");
                        continue;
                    }

                    Console.WriteLine($"Image {i + 1}: computing {method.Key}...");
                    double value = 0.0;
                    try
                    {
                        value = method.Value(img);
                        focusValues.Add(value);
                        Console.WriteLine($"Image {i + 1}: {method.Key} value = {value:F4}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Image {i + 1}: Error computing {method.Key}: {ex.Message}");
                    }
                }

                if (focusValues.Count == 0)
                {
                    Console.WriteLine($"{method.Key}: No valid focus values computed.");
                    continue;
                }

                //Sort descending and take top 3
                var sorted = focusValues.OrderByDescending(f => f).ToList();
                int take = Math.Min(3, sorted.Count);
                double sumTop = 0;
                for (int t = 0; t < take; t++)
                {
                    Console.WriteLine($"{method.Key}: Top {t + 1} value = {sorted[t]:F4}");
                    sumTop += sorted[t];
                }

                double averageTop = sumTop / take;
                Console.WriteLine($"{method.Key}: Focus Value Threshold (average of top {take}): {averageTop:F4}");

                //Summary given the method
                double min = sorted.Min();
                double max = sorted.Max();
                double meanAll = sorted.Average();
                Console.WriteLine($"{method.Key}: Summary: Count: {sorted.Count}, Min: {min:F4}, Max: {max:F4}, Mean: {meanAll:F4}");
                focusValue = averageTop;

                // Suggest best image
                double bestValue = sorted[0];
                int bestIndex = focusValues.IndexOf(bestValue); // returns first index in original list
                Console.WriteLine($"{method.Key}: Suggested best image (first occurrence) has value {bestValue:F4} (index in processed list: {bestIndex})");
            }

            Console.WriteLine("\nFocus.run: Analysis complete.");
            return focusValue;
        }
    }
}
