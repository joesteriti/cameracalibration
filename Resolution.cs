using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CameraCalibration
{
    public class Resolution
    {
        public static (bool Found, int CornerCount, double AvgCornerDist, System.Drawing.Size ImageResolution) FindChessboardCornersAndMetrics(string imagePath, System.Drawing.Size patternSize)
    {
        // 1. Check if file exists
        if (!File.Exists(imagePath))
        {
            Console.WriteLine("ERROR: Image file does not exist!");
            return (false, 0, 0, new System.Drawing.Size(0, 0));
        }

        // 2. Load image safely
        Mat gray = null;
        try
        {
            gray = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
            if (gray.IsEmpty)
            {
                Console.WriteLine("ERROR: Image failed to load or is empty.");
                return (false, 0, 0, new System.Drawing.Size(0, 0));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR loading image: {ex.Message}");
            return (false, 0, 0, new System.Drawing.Size(0, 0));
        }

        Console.WriteLine($"Image loaded: {gray.Width}x{gray.Height}");

        // 3. Validate image size for chessboard
        if (gray.Width < patternSize.Width || gray.Height < patternSize.Height)
        {
            Console.WriteLine("ERROR: Image too small for chessboard detection!");
            return (false, 0, 0, gray.Size);
        }

        // 4. Container for corners
        using var corners = new VectorOfPointF();

        // 5. Attempt to detect corners
        bool found = false;
        try
        {
            Console.WriteLine("Attempting to find chessboard corners...");
            found = CvInvoke.FindChessboardCorners(
                gray,
                patternSize,
                corners,
                CalibCbType.AdaptiveThresh | CalibCbType.NormalizeImage
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR during FindChessboardCorners: {ex.Message}");
            return (false, 0, 0, gray.Size);
        }

        Console.WriteLine($"Chessboard found? {found}");

        if (!found)
            return (false, 0, 0, gray.Size);

        // 6. Refine corners
        var termCriteria = new MCvTermCriteria(30, 0.1);
        CvInvoke.CornerSubPix(
            gray,
            corners,
            new System.Drawing.Size(11, 11),
            new System.Drawing.Size(-1, -1),
            termCriteria
        );

        // 7. Analyze corner distances
        var cornerArray = corners.ToArray();
        double totalDist = 0;
        int distCount = 0;

        for (int y = 0; y < patternSize.Height; y++)
        {
            for (int x = 0; x < patternSize.Width - 1; x++)
            {
                int idx = y * patternSize.Width + x;
                double dx = cornerArray[idx + 1].X - cornerArray[idx].X;
                double dy = cornerArray[idx + 1].Y - cornerArray[idx].Y;
                totalDist += Math.Sqrt(dx * dx + dy * dy);
                distCount++;
            }
        }

        double avgDist = totalDist / Math.Max(distCount, 1);

        return (true, cornerArray.Length, avgDist, gray.Size);
    }
        public static void calibrateResolution(List<string> imgPathList, float squareSize = 1.0f, bool showPics = true)
        {
            int nRows = 9;
            int nCols = 6;
            MCvTermCriteria terminationCriteria = new MCvTermCriteria(30, 0.1);

            List<MCvPoint3D32f[]> worldPtsList = new List<MCvPoint3D32f[]>();
            List<VectorOfPointF> imgPtsList = new List<VectorOfPointF>();

            // Build world coordinate system
            MCvPoint3D32f[] worldPts = new MCvPoint3D32f[nRows * nCols];
            for (int i = 0; i < nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    worldPts[i * nCols + j] = new MCvPoint3D32f(j * squareSize, i * squareSize, 0);
                }
            }

            System.Drawing.Size patternSize = new System.Drawing.Size(nCols, nRows);
            Console.WriteLine($"Pattern size: {patternSize.Width} x {patternSize.Height}");

            if (imgPathList == null || imgPathList.Count == 0)
            {
                Console.WriteLine("No image paths provided.");
                return;
            }

            // Load first image to get size
            Mat firstImg = CvInvoke.Imread(imgPathList[0], ImreadModes.ColorBgr);
            if (firstImg.IsEmpty)
            {
                Console.WriteLine($"Could not load the first image: {imgPathList[0]}");
                return;
            }
            else
            {
                Console.WriteLine($"Loaded first image: {imgPathList[0]}");
            }

            System.Drawing.Size imageSize = new System.Drawing.Size(firstImg.Width, firstImg.Height);
            Console.WriteLine($"Image size: {imageSize.Width} x {imageSize.Height}");

            // Loop through all images
            foreach (string imgPath in imgPathList)
            {
                if (!File.Exists(imgPath))
                {
                    Console.WriteLine($"File not found: {imgPath}");
                    continue;
                }
                else
                {
                    Console.WriteLine($"Processing image: {imgPath}");
                }


                Mat img = CvInvoke.Imread(imgPath, ImreadModes.ColorBgr);
                if (img.IsEmpty)
                {
                    Console.WriteLine($"Could not read image {imgPath}");
                    continue;
                }
                else
                {
                    Console.WriteLine($"Successfully loaded image: {imgPath}");
                }

                Mat gray = new Mat();
                VectorOfPointF corners = null;

                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
                corners = new VectorOfPointF();



                bool found = false;
                // assume img is the color Mat loaded successfully
                CvInvoke.EqualizeHist(gray, gray);                                // boost contrast
                CvInvoke.GaussianBlur(gray, gray, new System.Drawing.Size(5, 5), 1.5);            // reduce noise

                Mat binary = new Mat();
                CvInvoke.AdaptiveThreshold(gray, binary, 255, AdaptiveThresholdType.GaussianC,
                    ThresholdType.Binary, 11, 2);                                 // make high-contrast


                // ensure contiguous safe buffer
                Mat safe = binary.Clone();
                try
                {
                    found = CvInvoke.FindChessboardCornersSB(gray, new System.Drawing.Size(nCols, nRows), corners, CalibCbType.AdaptiveThresh | CalibCbType.NormalizeImage);
                    Console.WriteLine($"FindChessboardCornersSB returned: {found}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error finding chessboard corners in {imgPath}: {ex.Message}");
                    continue;
                }

                if (found)
                {
                    Console.WriteLine($"Detected {corners.Size} corners in {Path.GetFileName(imgPath)}");

                    CvInvoke.CornerSubPix(gray, corners, new System.Drawing.Size(11, 11), new System.Drawing.Size(-1, -1), terminationCriteria);

                    if (corners.Size != worldPts.Length)
                    {
                        Console.WriteLine($"Corner count mismatch in {Path.GetFileName(imgPath)}. Expected {worldPts.Length}, found {corners.Size}. Skipping this image.");
                        continue;
                    }

                    imgPtsList.Add(corners);
                    worldPtsList.Add((MCvPoint3D32f[])worldPts.Clone());
                    Console.WriteLine($"Chessboard found in {Path.GetFileName(imgPath)} ({corners.Size} corners)");
                }
                else
                {
                    Console.WriteLine($"Chessboard not found in {Path.GetFileName(imgPath)}");
                }
            }

            CvInvoke.DestroyAllWindows();

            // Safety checks before calibration
            if (imgPtsList.Count == 0)
            {
                Console.WriteLine("No chessboard corners detected in any image. Calibration aborted.");
                return;
            }

            if (imgPtsList.Count != worldPtsList.Count)
            {
                Console.WriteLine($"Mismatch: {imgPtsList.Count} image sets vs {worldPtsList.Count} world sets.");
                return;
            }

            for (int i = 0; i < imgPtsList.Count; i++)
            {
                if (worldPtsList[i].Length != imgPtsList[i].Size)
                {
                    Console.WriteLine($"Corner count mismatch at image {i}: world {worldPtsList[i].Length}, image {imgPtsList[i].Size}");
                    return;
                }
            }

            // Preallocate matrices correctly
            Mat cameraMatrix = new Mat(3, 3, DepthType.Cv64F, 1);
            Mat distCoefficients = new Mat(8, 1, DepthType.Cv64F, 1);
            Mat[] rvecs, tvecs;

            System.Drawing.PointF[][] imagePointsArray = new System.Drawing.PointF[imgPtsList.Count][];
            for (int i = 0; i < imgPtsList.Count; i++)
            {
                imagePointsArray[i] = imgPtsList[i].ToArray();
            }

            Console.WriteLine("📸 Starting camera calibration...");
            Console.WriteLine($"Images with detected corners: {imgPtsList.Count}");
            for (int i = 0; i < imgPtsList.Count; i++)
            {
                Console.WriteLine($"Image {i}: worldPts={worldPtsList[i].Length}, imgPts={imgPtsList[i].Size}");
                if (worldPtsList[i].Length != imgPtsList[i].Size)
                {
                    Console.WriteLine($"❌ Corner mismatch at image {i}. Skipping this set.");
                    worldPtsList.RemoveAt(i);
                    imgPtsList.RemoveAt(i);
                    i--;
                }
            }

            // worldPtsList is List<MCvPoint3D32f[]>
            MCvPoint3D32f[][] objectPoints = worldPtsList.ToArray();

            // imagePointsArray is System.Drawing.PointF[][]
            for (int i = 0; i < imgPtsList.Count; i++)
                imagePointsArray[i] = imgPtsList[i].ToArray();

            try
            {

                double reprojectionError = CvInvoke.CalibrateCamera(
                    objectPoints,
                    imagePointsArray,
                    imageSize,
                    cameraMatrix,
                    distCoefficients,
                    CalibType.Default,
                    terminationCriteria,
                    out rvecs,
                    out tvecs
                );

                // --- Output Intrinsics ---
                Console.WriteLine("\n===== INTRINSICS =====");
                double[,] camData = (double[,])cameraMatrix.GetData();
                Console.WriteLine($"Camera Matrix (3x3):\n" +
                                $"[{camData[0, 0]:F3}, {camData[0, 1]:F3}, {camData[0, 2]:F3}]\n" +
                                $"[{camData[1, 0]:F3}, {camData[1, 1]:F3}, {camData[1, 2]:F3}]\n" +
                                $"[{camData[2, 0]:F3}, {camData[2, 1]:F3}, {camData[2, 2]:F3}]");

                double[] distData = (double[])distCoefficients.GetData();
                Console.WriteLine($"\nDistortion Coefficients: {string.Join(", ", Array.ConvertAll(distData, x => x.ToString("F6")))}");

                Console.WriteLine($"\nReprojection Error (pixels): {reprojectionError:F4}");

                // --- Output Extrinsics ---
                Console.WriteLine("\n===== EXTRINSICS (per image) =====");
                for (int i = 0; i < rvecs.Length; i++)
                {
                    Mat rotationMatrix = new Mat();
                    CvInvoke.Rodrigues(rvecs[i], rotationMatrix); // convert Rodrigues to 3x3

                    double[,] R = (double[,])rotationMatrix.GetData();
                    double[,] T = (double[,])tvecs[i].GetData();

                    Console.WriteLine($"\nImage {i + 1}:");
                    Console.WriteLine($"Rotation Matrix:\n" +
                                    $"[{R[0, 0]:F4}, {R[0, 1]:F4}, {R[0, 2]:F4}]\n" +
                                    $"[{R[1, 0]:F4}, {R[1, 1]:F4}, {R[1, 2]:F4}]\n" +
                                    $"[{R[2, 0]:F4}, {R[2, 1]:F4}, {R[2, 2]:F4}]");

                    Console.WriteLine($"Translation Vector: [{T[0, 0]:F4}, {T[1, 0]:F4}, {T[2, 0]:F4}]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Calibration failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
    }
    
}
