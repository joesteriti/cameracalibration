using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;

namespace CameraCalibration
{
    public class Program()
    {
        static void Main(string[] args)
        {
            Mat newImg = new Mat();
            Mat img = CvInvoke.Imread("/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_14-59-55.csv.tif", Emgu.CV.CvEnum.ImreadModes.ColorBgr);
            Console.Write(img.Depth);
            Mat doubleimg = new Mat();
            Console.WriteLine(img.Depth);

            CvInvoke.CvtColor(img, newImg, ColorConversion.Bgr2Hsv);
            newImg.ConvertTo(doubleimg, DepthType.Cv32F, 1.0/255.0, 0);

            var split = doubleimg.Split();
            var hue = split[0];
            var sat = split[1];
            var val = split[2];
            Console.WriteLine(val.Depth);
            bool both= false;

            Mat thresholdimg = val.Clone();
            
            //Sat gray rgb(106, 106, 106)
            CvInvoke.Threshold(val,thresholdimg, 0.2, 1.0, Emgu.CV.CvEnum.ThresholdType.Binary);

            CvInvoke.Imshow("Binary", thresholdimg);
            CvInvoke.Imshow("Original", img);
            CvInvoke.Imshow("Val", val);
            CvInvoke.Imshow("Sat", sat);
            CvInvoke.WaitKey(0);
            CvInvoke.DestroyAllWindows();

            SimpleBlobDetectorParams parameters = new SimpleBlobDetectorParams
            {
                FilterByColor = true,
                blobColor = 255, //white
            };
            SimpleBlobDetector blob = new SimpleBlobDetector(parameters);
            Mat whiteMask = new Mat();
            var points = blob.Detect(whiteMask);


            
            Environment.Exit(0);
            //Variables for program
            string title = "Camera Calibration Report";

            double focusValue = 0;
            double focusThreshold = 150000.0; // From running focus analysis, the mean of the top 3 focused images was 15000 for sobel variance

            double[,] cameraMatrix = new double[3, 3]; 
            double reprojectionError = 0.0;
            double reprojectionErrorThreshold = 1.0; //Threshold for reprojection error should be 1px but some sources say less than 0.5px
            
            double intensity = 0.0;
            (double Min, double Max) intensityRange = (120.0, 180.0); //Set to this for adequate lighting conditions between this range
            List<(string Name, MCvScalar SampleBGR, MCvScalar ReferenceBGR)> results =
            new List<(string Name, MCvScalar SampleBGR, MCvScalar ReferenceBGR)>
            {
                ("PlaceholderColor", new MCvScalar(0, 0, 0), new MCvScalar(255, 255, 255))
            };
            
            string logoPath = "/Users/josephsteriti/Desktop/c#code/cameraCalibration/Platypus-Vision.jpg";

            //This can be taken out because it was for inital focus threshold acquirement
            var imagePaths = new List<string>{
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/Focus Images/6.bmp",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/Focus Images/4.bmp",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/cameraCalibration/stitched_vertical.bmp",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_14-56-29.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_14-59-55.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-01-22.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-02-25.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-03-17.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-04-02.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-05-00.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-05-51.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-06-19.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-07-06.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-08-13.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-09-29.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-10-20.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_15-10-31.csv.tif",

            };

            //Focus Analysis
            try
            {
                Console.WriteLine("Focus analysis starting.");
                var focusCheck = new Focus(imagePaths);
                focusValue = focusCheck.focusAnalysis();
                Console.WriteLine("Focus analysis finished successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error computing focus metric: {ex.Message}");
            }

            Console.WriteLine("Starting camera calibration...");

            var resolutionImagePaths = new List<string>
            {
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-13-41.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-22-23.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-22-48.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-23-03.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-24-07.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-24-37.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-25-21.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-25-35.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-25-50.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-26-12.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-26-30.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-26-45.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-27-00.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-27-23.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-27-39.csv.tif",
                "/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-27-58.csv.tif"
            };
            //End of Focus Analysis


            //Resolution calibration attempt
            // float squareSize = 2.5f; // checkerboard square = 2.5 cm
            // try
            // {
            //     var pattern = new System.Drawing.Size(6, 9);
            //     var result = Resolution.FindChessboardCornersAndMetrics("/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Chess board/chess1.png", pattern);
            //     if (result.Found)
            //     {
            //         Console.WriteLine($"Corners Found: {result.CornerCount}");
            //         Console.WriteLine($"Avg Corner Distance: {result.AvgCornerDist:F2} px");
            //         Console.WriteLine($"Image Resolution: {result.ImageResolution.Width}x{result.ImageResolution.Height}");
            //     }
            //     else
            //     {
            //         Console.WriteLine( Chessboard not detected.");
            //     }

            //     // Resolution.calibrateResolution(
            //     //     imgPathList: resolutionImagePaths,
            //     //     squareSize: squareSize,
            //     //     showPics: true  // shows chessboard detections if supported
            //     // );

            //     Console.WriteLine("Calibration process completed successfully.");
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine($"Calibration failed: {ex.Message}");
            //     Console.WriteLine("→ Using placeholder values for camera calibration matrix and distortion coefficients.");

            //     // Provide fallback defaults so the rest of your pipeline continues
            //     double[,] placeholderCameraMatrix = new double[3, 3]
            //     {
            //         { 1000, 0, 640 },
            //         { 0, 1000, 360 },
            //         { 0, 0, 1 }
            //     };

            //     // Assign these placeholders to your existing cameraMatrix variable if needed
            //     cameraMatrix = placeholderCameraMatrix;
            // }

            //Color Balance Analysis Attempt
            Console.WriteLine($"Starting color analysis");
            intensity = ColorBalance.GetAverageLightIntensity("/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Big Card/Characterization_2025-11-06_15-35-42.csv.tif");


            var colorBalance = new ColorBalance(ColorCard.small24ColorCard);//SWAP OUT COLORCARD HERE
            try
            {
                colorBalance.LoadImage("/Users/josephsteriti/Desktop/c#code/cameraCalibration/cameraCalibration/stitched_vertical copy 2.bmp");
                //colorBalance.LoadImage("/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_14-59-55.csv.tif");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
                return;
            }

            try
            {
                results = colorBalance.AnalyzeColors(4, 1, 3, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Color analysis failed: {ex.Message}");
                Console.WriteLine("Filling placeholders with (0, 0, 0).");

                results = ColorCard.small24ColorCard.ReferenceColors
                    .Select(kv => (kv.Key, new MCvScalar(0, 0, 0), new MCvScalar(kv.Value.B, kv.Value.G, kv.Value.R)))
                    .ToList();
            }

            // 5. Display results
            foreach (var (name, sampleBGR, referenceBGR) in results)
            {
                var (rSample, gSample, bSample) = ColorBalance.BgrToRgb(sampleBGR);
                var (rRef, gRef, bRef) = ColorBalance.BgrToRgb(referenceBGR);
                using (var mat = new Mat(1, 1, Emgu.CV.CvEnum.DepthType.Cv8U, 3))
                {
                    mat.SetTo(new MCvScalar(bSample, gSample, rSample)); // BGR order
                    using (var hsvImg = mat.ToImage<Hsv, byte>())
                    {
                        Hsv hsv = hsvImg[0, 0];
                        // double hue = hsv.Hue;
                        // double sat = hsv.Satuation / 255.0 * 100.0;
                        // double val = hsv.Value / 255.0 * 100.0;



                        Console.WriteLine($"{name,-15}  Sample: ({rSample}, {gSample}, {bSample}) Sample HSV: ({hue,6:F1}°, {sat,6:F1}%, {val,6:F1}%)  Ref: ({rRef}, {gRef}, {bRef})");
                    }
                }
            }
            // Optional: visualize the grid
            CvInvoke.Imshow("Detected Grid", colorBalance.GridVisualization);
            CvInvoke.WaitKey(0);
            CvInvoke.DestroyAllWindows();

            Console.Write("Hi");
            ColorBalance.DetectText("/Users/josephsteriti/Desktop/c#code/cameraCalibration/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_14-59-55.csv.tif");
            Console.Write("Bye");
            //Creating a new report object
            var report = new Report(
                title,
                focusValue,
                focusThreshold,
                reprojectionError,
                reprojectionErrorThreshold,
                cameraMatrix,
                logoPath,
                ColorCard.small24ColorCard,
                results,
                intensity,
                intensityRange
                );
            report.Generate("CalibrationReport.pdf");

            Console.WriteLine("PDF report generated successfully!");
        
        }
    }
}
