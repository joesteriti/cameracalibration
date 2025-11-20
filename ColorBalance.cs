using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.OCR;
//Color card has to be fed through so that white as at the top left of the image
namespace CameraCalibration
{
    public class ColorBalance
    {
        private readonly ColorCard _colorCard;
        private List<Mat> _images = new List<Mat>();

        public Mat GridVisualization { get; private set; }

        public string GetColorCardName()
        {
            return _colorCard.Name;
        }

        public ColorBalance(ColorCard colorCard)
        {
            _colorCard = colorCard;
        }

        // Load single image, change to array
        public void LoadImage(string path)
        {
            _images.Clear();
            var img = CvInvoke.Imread(path, ImreadModes.ColorBgr);
            if (img.IsEmpty) throw new Exception($"Failed to load {path}");
            _images.Add(img);
        }

        public static double GetAverageLightIntensity(string imagePath)
        {
            Mat image = CvInvoke.Imread(imagePath, ImreadModes.ColorBgr);

            if (image.IsEmpty)
            {
                Console.WriteLine("Error: Image not found or invalid path.");
                return -1;
            }
  
            Mat gray = new Mat();
            CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);

            double avgLightIntensity = CvInvoke.Mean(gray).V0;

            Console.WriteLine($"💡 Average light intensity: {avgLightIntensity:F2}");

            return avgLightIntensity;
        }


        
        public List<(string Name, MCvScalar SampleBGR, MCvScalar ReferenceBGR)>
        AnalyzeColors(int upCount, int downCount, int leftCount, int rightCount, int pixelMargin = 5)
        {
            if (_images.Count == 0)
            {
                Console.WriteLine("⚠️ No images loaded — skipping color analysis.");
                return new List<(string Name, MCvScalar SampleBGR, MCvScalar ReferenceBGR)>();
            }
        

            Mat combined = _images[0]; // single image
        
            GridVisualization = combined.Clone();


            // Identify light blue anchor (purple seemed to be the most reliable color to detect)
            var ltBlueRef = _colorCard.ReferenceColors["Purple"];
            
            

            var mask = new Mat();
            double colorThreshold = 30;
            CvInvoke.InRange(combined,

            new ScalarArray(new MCvScalar(Math.Clamp(ltBlueRef.B - colorThreshold, 0, 255),
                                        Math.Clamp(ltBlueRef.G - colorThreshold, 0, 255),
                                        Math.Clamp(ltBlueRef.R - colorThreshold, 0, 255))),
            new ScalarArray(new MCvScalar(Math.Clamp(ltBlueRef.B + colorThreshold, 0, 255),
                                        Math.Clamp(ltBlueRef.G + colorThreshold, 0, 255),
                                        Math.Clamp(ltBlueRef.R + colorThreshold, 0, 255))),
            mask);

            using var contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(mask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            if (contours.Size == 0) throw new Exception("Light Blue anchor not found");

            // Find largest purple region
            int largestIndex = 0;
            double maxArea = 0;
            for (int i = 0; i < contours.Size; i++)
            {
                double area = CvInvoke.ContourArea(contours[i]);
                if (area > maxArea)
                {
                    maxArea = area;
                    largestIndex = i;
                }
            }

            var ltBlueRect = CvInvoke.BoundingRectangle(contours[largestIndex]);
            var ltBlueCenter = new System.Drawing.Point(ltBlueRect.X + ltBlueRect.Width / 2,
                                                        ltBlueRect.Y + ltBlueRect.Height / 2);

            CvInvoke.Circle(GridVisualization, ltBlueCenter, 15, new MCvScalar(255, 0, 255), 3); // Purple circle
            CvInvoke.PutText(GridVisualization, "Light Blue Anchor", new System.Drawing.Point(ltBlueCenter.X + 10, ltBlueCenter.Y),
                FontFace.HersheySimplex, 0.6, new MCvScalar(255, 255, 255), 2);

            CvInvoke.Imshow("Purple Detection", GridVisualization);
            CvInvoke.WaitKey(5000);
            CvInvoke.DestroyAllWindows();


            //Building grid rectangles based on anchor
            int gridWidth = ltBlueRect.Width + pixelMargin;
            int gridHeight = ltBlueRect.Height + pixelMargin;
            int imgWidth = combined.Width;
            int imgHeight = combined.Height;

            List<Rectangle> gridRects = new List<Rectangle>();
            for (int dx = -leftCount; dx <= rightCount; dx++)
            {
            for (int dy = -upCount; dy <= downCount; dy++)
            {
                int x = Math.Clamp(ltBlueCenter.X - gridWidth / 2 + dx * gridWidth, 0, imgWidth - gridWidth);
                int y = Math.Clamp(ltBlueCenter.Y - gridHeight / 2 + dy * gridHeight, 0, imgHeight - gridHeight);
                var rect = new Rectangle(x, y, gridWidth, gridHeight);
                gridRects.Add(rect);

                // Visualization (take this out later)
                CvInvoke.Rectangle(GridVisualization, rect, new MCvScalar(0, 255, 0), 2);
            }
        }

            var colorNamesOrdered = _colorCard.ReferenceColors.Keys.ToArray();
            int count = Math.Min(gridRects.Count, colorNamesOrdered.Length);

            
            //Assigning expected colors to rectangles
            List<(string Name, MCvScalar SampleBGR, MCvScalar ReferenceBGR)> results =
                new List<(string Name, MCvScalar SampleBGR, MCvScalar ReferenceBGR)>();

            for (int idx = 0; idx < count; idx++)
            {
                string expectedName = colorNamesOrdered[idx];
                var expectedColor = _colorCard.ReferenceColors[expectedName];

                var rect = gridRects[idx];
                using Mat roi = new Mat(combined, rect);
                var avg = CvInvoke.Mean(roi);

                results.Add((expectedName, avg, new MCvScalar(expectedColor.B, expectedColor.G, expectedColor.R)));

                // Draw expected color name on visualization (for debugging)
                CvInvoke.PutText(GridVisualization, expectedName,
                    new System.Drawing.Point(rect.X, rect.Y + 12),
                    FontFace.HersheySimplex, 0.4, new MCvScalar(0, 0, 255), 1);
            }

            // Visualization (also for debugging)
            CvInvoke.Imshow("Detected Grid", GridVisualization);
            CvInvoke.WaitKey(5000);
            CvInvoke.DestroyWindow("Detected Grid");

            return results;
        }


        public static (byte R, byte G, byte B) BgrToRgb(MCvScalar bgr)
        {
            return ((byte)Math.Clamp(bgr.V2, 0, 255),
                    (byte)Math.Clamp(bgr.V1, 0, 255),
                    (byte)Math.Clamp(bgr.V0, 0, 255));
        }

        public static void DetectText(string imagePath)
        {
            // Load image
            Mat img = CvInvoke.Imread(imagePath, Emgu.CV.CvEnum.ImreadModes.ColorBgr);
            if (img.IsEmpty)
                throw new Exception("Image not found or invalid path.");

            Console.Write("1");

            using Tesseract ocr = new Tesseract(imagePath, "eng", OcrEngineMode.LstmOnly);
            Console.Write("2");

            // Set image
            ocr.SetImage(img);

            // Recognize text
            ocr.Recognize();

            // Get recognized words (no 'using' because it's not disposable)
            Tesseract.Word[] words = ocr.GetWords();

            Console.WriteLine("Detected words and regions:");
            foreach (var word in words)
            {
                Console.WriteLine($"Text: {word.Text}");
                Console.WriteLine($"Region: {word.Region}"); // Rectangle
            }
        }
    }
}
