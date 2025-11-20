using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using iText.Kernel.Colors;

using Emgu.CV;
using Emgu.CV.Structure;



namespace CameraCalibration
{
    public class Report
    {
        private readonly string _logo;
        private readonly string _title;
        private readonly double _focus;
        private readonly double _focusThreshold;
        private readonly double _reproj;
        private readonly double _reprojectionErrorThreshold;

        private readonly double[,] _cameraMatrix;
        private readonly ColorBalance _colorData;
        private readonly List<(string Name, MCvScalar SampleBGR, MCvScalar ReferenceBGR)> _colorResults;
        private readonly double _intensity;
        private readonly (double Min, double Max) _intensityRange;

        private readonly double deltaThreshold = 5.0; //In the automotive industry the ΔE*CMC is rather stringent, often less than 0.5 under D65/10. In printing, the typical limit is 2.0 under D50, though some processes require up to 5.0.

        public Report(string title, double focus, double focusThreshold, double reproj, double reprojectionErrorThreshold, double[,] cameraMatrix, string logo, ColorCard card, List<(string Name, MCvScalar SampleBGR, MCvScalar ReferenceBGR)> colorResults, double instensity, (double Min, double Max) intensityRange)
        {
            _title = title;
            _focus = focus;
            _focusThreshold = focusThreshold;
            _reproj = reproj;
            _reprojectionErrorThreshold = reprojectionErrorThreshold;
            _cameraMatrix = cameraMatrix;
            _logo = logo;
            _colorData = new ColorBalance(card);
            _colorResults = colorResults;
            _intensity = instensity;
            _intensityRange = intensityRange;
        }

        public void Generate(string filePath)
        {
            // Initialize PDF writer and document
            using var writer = new PdfWriter(filePath);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Title
            document.Add(new Paragraph(_title)
                .SetFontSize(20)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetMarginBottom(20));

            var imgData = ImageDataFactory.Create(_logo);
            iText.Layout.Element.Image image = new iText.Layout.Element.Image(imgData)
                .ScaleToFit(100, 75)
                .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER)
                .SetMarginBottom(10);

            document.Add(image);

            // Timestamp
            document.Add(new Paragraph($"Generated on: {DateTime.Now:f}")
                .SetFontSize(20)
                .SetMarginBottom(10));
            document.Add(new Paragraph($"Using: {_colorData.GetColorCardName()}")
                    .SetFontSize(10)
                    .SetMarginBottom(10)
            );

            //Variables needed for Summary Table
            // Focus metric
            bool focusPass = _focus <= _focusThreshold;

            // Reprojection metric
            bool reprojPass = _reproj <= _reprojectionErrorThreshold;

            // Color metrics
            var colorMetrics = _colorResults.Select(r =>
            {
                byte rSample = (byte)Math.Clamp(r.SampleBGR.V2, 0, 255);
                byte gSample = (byte)Math.Clamp(r.SampleBGR.V1, 0, 255);
                byte bSample = (byte)Math.Clamp(r.SampleBGR.V0, 0, 255);

                byte rRef = (byte)Math.Clamp(r.ReferenceBGR.V2, 0, 255);
                byte gRef = (byte)Math.Clamp(r.ReferenceBGR.V1, 0, 255);
                byte bRef = (byte)Math.Clamp(r.ReferenceBGR.V0, 0, 255);

                double[] labSample = CIEDE2000.RgbToLab(new byte[] { rSample, gSample, bSample });
                double[] labRef = CIEDE2000.RgbToLab(new byte[] { rRef, gRef, bRef });

                double delta = CIEDE2000.DE00Difference(
                    labSample[0], labSample[1], labSample[2],
                    labRef[0], labRef[1], labRef[2]
                );

                bool pass = delta < deltaThreshold;

                return new
                {
                    Name = r.Name,
                    SampleBGR = r.SampleBGR,
                    ReferenceBGR = r.ReferenceBGR,
                    Delta = delta,
                    Pass = pass
                };
            }).ToList();

            // Overall color balance pass/fail
            bool colorPass = colorMetrics.All(c => c.Pass);

            //Summary Table
            document.Add(new Paragraph("Calibration Metrics")
                .SetFontSize(14)
                .SetMarginBottom(10));

            
            var table = new Table(2).UseAllAvailableWidth();

            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new Paragraph("Metric")));

            table.AddHeaderCell(new iText.Layout.Element.Cell().Add(new Paragraph("Pass or Fail")));

            var colorCell = new iText.Layout.Element.Cell().Add(new Paragraph(colorPass ? "PASS" : "FAIL"))
                .SetFontColor(colorPass ? ColorConstants.GREEN : ColorConstants.RED)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);

            table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph("Color Balance")));
            table.AddCell(colorCell);

            var focusCell = new iText.Layout.Element.Cell().Add(new Paragraph(focusPass ? "PASS" : "FAIL"))
                .SetFontColor(focusPass ? ColorConstants.GREEN : ColorConstants.RED)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);

            table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph("Focus")));
            table.AddCell(focusCell);

            var resolutionCell = new iText.Layout.Element.Cell().Add(new Paragraph(reprojPass ? "PASS" : "FAIL"))
                .SetFontColor(reprojPass ? ColorConstants.GREEN : ColorConstants.RED)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);

            table.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph("Resolution")));
            table.AddCell(resolutionCell);

            document.Add(table);
            //Focus Section
            document.Add(new Paragraph($"Focus Analysis")
                    .SetFontSize(14)
                    .SetMarginTop(10)
                    .SetMarginBottom(10));

            var focusTable = new Table(3).UseAllAvailableWidth();
            focusTable.AddHeaderCell("Value");
            focusTable.AddHeaderCell("Threshold");
            focusTable.AddHeaderCell("Pass or Fail");
            focusTable.AddCell($"{_focus:F2}");
            focusTable.AddCell($"{_focusThreshold:F2}");
            bool isPass = _focus >= _focusThreshold; 
            var cell = new iText.Layout.Element.Cell()
                .Add(new Paragraph(isPass ? "Pass" : "Fail"))
                .SetFontColor(isPass ? ColorConstants.GREEN : ColorConstants.RED)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
            focusTable.AddCell(cell);
            document.Add(focusTable);

            //Resolution Section
            document.Add(new Paragraph($"Resolution Analysis")
                    .SetFontSize(14)
                    .SetMarginTop(10)
                    .SetMarginBottom(10));

            var resolutionCoefficients = new Table(3).UseAllAvailableWidth();
            resolutionCoefficients.AddHeaderCell("Camera Matrix");

            resolutionCoefficients.AddCell($"{_cameraMatrix[0, 0]:F2}");
            resolutionCoefficients.AddCell($"{_cameraMatrix[0, 1]:F2}");
            resolutionCoefficients.AddCell($"{_cameraMatrix[0, 2]:F2}");
            resolutionCoefficients.AddCell($"{_cameraMatrix[1, 0]:F2}");
            resolutionCoefficients.AddCell($"{_cameraMatrix[1, 1]:F2}");
            resolutionCoefficients.AddCell($"{_cameraMatrix[1, 2]:F2}");
            resolutionCoefficients.AddCell($"{_cameraMatrix[2, 0]:F2}");
            resolutionCoefficients.AddCell($"{_cameraMatrix[2, 1]:F2}");
            resolutionCoefficients.AddCell($"{_cameraMatrix[2, 2]:F2}");
            document.Add(resolutionCoefficients);


            var reprojectionErrorTable = new Table(2).UseAllAvailableWidth();
            reprojectionErrorTable.AddHeaderCell("Reprojection Error");
            reprojectionErrorTable.AddHeaderCell("Pass or Fail");
            reprojectionErrorTable.AddCell($"{_reproj:F2}");
            isPass = _reproj <= 1.0; 
            cell = new iText.Layout.Element.Cell()
                .Add(new Paragraph(isPass ? "Pass" : "Fail"))
                .SetFontColor(isPass ? ColorConstants.GREEN : ColorConstants.RED)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
            reprojectionErrorTable.AddCell(cell);
            document.Add(reprojectionErrorTable);
            //Color Balance Section




            //Console.WriteLine($"Found {results.Count} color results.");




            // Add a header for the color
            document.Add(new Paragraph($"Color Balance Analysis")
                .SetFontSize(14)
                .SetMarginTop(10)
                .SetMarginBottom(10));


            // Create a 2-column table: Attribute | Value
            // Create one unified table for all colors
            document.Add(new Paragraph($"Light Intensity Analysis")
                .SetFontSize(10)
                .SetMarginTop(10)
                .SetMarginBottom(10));
            var intensityTable = new Table(2).UseAllAvailableWidth().SetMarginBottom(10);
            intensityTable.AddHeaderCell("Intensity Analysis").SetFontSize(10);
            intensityTable.AddHeaderCell("Pass or Fail").SetFontSize(10);

            

            intensityTable.AddCell($"{_intensity}").SetFontSize(10);  
            isPass = _intensity <= _intensityRange.Max && _intensity >= _intensityRange.Min;
            cell = new iText.Layout.Element.Cell()
                .Add(new Paragraph(isPass ? "Pass" : "Fail"))
                .SetFontColor(isPass ? ColorConstants.GREEN : ColorConstants.RED)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
            intensityTable.AddCell(cell).SetFontSize(10);
            document.Add(intensityTable);
            var colorTable = new Table(6).UseAllAvailableWidth().SetMarginBottom(10);

            // Add headers
            colorTable.AddHeaderCell("Color");
            colorTable.AddHeaderCell("HSV (H°, S%, V%)");
            colorTable.AddHeaderCell("Delta");
            colorTable.AddHeaderCell("Pass/Fail");
            colorTable.AddHeaderCell("Sample Color");
            colorTable.AddHeaderCell("Reference Color");


            foreach (var (name, sampleBGR, referenceBGR) in _colorResults)
            {
                // Convert BGR → RGB for visualization
                byte rSample = (byte)Math.Clamp(sampleBGR.V2, 0, 255);
                byte gSample = (byte)Math.Clamp(sampleBGR.V1, 0, 255);
                byte bSample = (byte)Math.Clamp(sampleBGR.V0, 0, 255);

                byte rRef = (byte)Math.Clamp(referenceBGR.V2, 0, 255);
                byte gRef = (byte)Math.Clamp(referenceBGR.V1, 0, 255);
                byte bRef = (byte)Math.Clamp(referenceBGR.V0, 0, 255);

                // Convert sample color to HSV for display
                double hue = 0, sat = 0, val = 0;
                using (var mat = new Mat(1, 1, Emgu.CV.CvEnum.DepthType.Cv8U, 3))
                {
                    mat.SetTo(new MCvScalar(bSample, gSample, rSample)); // BGR order
                    using (var hsvImg = mat.ToImage<Hsv, byte>())
                    {
                        Hsv hsv = hsvImg[0, 0];
                        hue = hsv.Hue;
                        sat = hsv.Satuation / 255.0 * 100.0;
                        val = hsv.Value / 255.0 * 100.0;
                    }
                }

                // Convert reference color to HSV for display
                double refhue = 0, refsat = 0, refval = 0;
                using (var mat = new Mat(1, 1, Emgu.CV.CvEnum.DepthType.Cv8U, 3))
                {
                    mat.SetTo(new MCvScalar(bRef, gRef, rRef)); // BGR order
                    using (var hsvImg = mat.ToImage<Hsv, byte>())
                    {
                        Hsv refhsv = hsvImg[0, 0];
                        refhue = refhsv.Hue;
                        refsat = refhsv.Satuation / 255.0 * 100.0;
                        refval = refhsv.Value / 255.0 * 100.0;
                    }
                }

                // Calculate Δ
                double delta = 0;
                double[] labSample = CIEDE2000.RgbToLab(new byte[] { rSample, gSample, bSample });
                double[] labRef = CIEDE2000.RgbToLab(new byte[] { rRef, gRef, bRef });


                // Compute CIEDE2000 color difference
                delta = CIEDE2000.DE00Difference(
                    labSample[0], labSample[1], labSample[2],
                    labRef[0], labRef[1], labRef[2]
                );

                // Determine pass/fail
                bool pass = delta < deltaThreshold;
                string passFail = pass ? "Pass" : "Fail";
                double cappedDelta = Math.Min(delta, 100.0);  // To prevent overflow if color is completely off

                colorTable.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(name)));
                colorTable.AddCell(new iText.Layout.Element.Cell().Add(
                    new Paragraph($"S: {hue:F1}°, {sat:F1}%, {val:F1}%\n" +   // sample HSV
                                $"Ref: {refhue:F1}°, {refsat:F1}%, {refval:F1}%")  // reference HSV
                            .SetMultipliedLeading(1.2f)));
                colorTable.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph($"{cappedDelta:F2}")));
                colorTable.AddCell(new iText.Layout.Element.Cell().Add(new Paragraph(passFail)).SetFontColor(pass ? ColorConstants.GREEN : ColorConstants.RED));


                // Sampled color visualization
                colorTable.AddCell(new iText.Layout.Element.Cell()
                    .Add(new Paragraph(" "))
                    .SetBackgroundColor(new DeviceRgb(rSample, gSample, bSample))
                    .SetHeight(20));

                // Reference color visualization
                colorTable.AddCell(new iText.Layout.Element.Cell()
                    .Add(new Paragraph(" "))
                    .SetBackgroundColor(new DeviceRgb(rRef, gRef, bRef))
                    .SetHeight(20));



            }
            document.Add(colorTable);

            // Footer
            document.Add(new Paragraph("\nReport Complete")
                .SetFontSize(10)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY));

            document.Close();
        }
    }
}
