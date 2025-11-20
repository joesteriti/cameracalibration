from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image


from resolution import Resolution
from focus import Focus
from colorbalance import Color
from colorcards import small24_color_card
import cv2
from datetime import datetime


class Pdf:
    @staticmethod
    def report():

        # Thresholds
        #How many rows and columns are on the color card being used
        rows = 6
        cols = 4
        color_card = small24_color_card
        summary_row_index = 1  
        colorDistanceThreshold = 40.0 #Using Euclidean Distance in RGB space for color difference
        reproductionErrorThreshold = 1.0
        min_intensity, max_intensity = 120.0, 180.0
        focusThreshold = 150000.0
        #These thresholds can be adjusted based on requirements 
        imgPath = cv2.imread("/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/stitched_vertical copy 2.bmp", cv2.IMREAD_COLOR)
        imgPathForLightIntensity = cv2.imread("/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Big Card/Characterization_2025-11-06_15-36-42.csv.tif", cv2.IMREAD_COLOR)
        colorcardImg = cv2.imread("/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Small card/Characterization_2025-11-06_14-59-55.csv.tif", cv2.IMREAD_COLOR) #Feed in color card so white is top left for best results 
        #Image paths can be changed to test different images. Resolution needs to have at least 10 images of chessboard pattern in the specified folder, the rest can be single images.

        # Report Setup
        doc = SimpleDocTemplate("CameraCalibrationReport.pdf",
                                pagesize=A4,
                                leftMargin=40,
                                rightMargin=40,
                                topMargin=40,
                                bottomMargin=40)

        styles = getSampleStyleSheet()
        elements = []

        # Title
        title = Paragraph("<b>Camera Detection Test</b>", styles["Title"])
        elements.append(title)
        elements.append(Spacer(1, 12))

        # Logo 
        logo = Image("/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Platypus-Vision.jpg", width=100, height=75)
        elements.append(logo)
        elements.append(Spacer(1, 12))

        # Card name
        card_info = Paragraph(
            f"<b>Using:</b> {color_card.get_color_card_name()}",
            styles["Normal"]
        )
        elements.append(card_info)
        elements.append(Spacer(1, 20))

        # Time Stamp of Test Generation
        now = datetime.now()
        formatted_time = now.strftime("%A, %d %B %Y %H:%M:%S")
        time_stamp = Paragraph(
            f"<b>Generated on:</b> {formatted_time}",
            styles["Normal"]
        )
        elements.append(time_stamp)
        elements.append(Spacer(1, 20))

        # Helper: Make a styled table
        def make_table(data, col_widths=None):
            table = Table(data, colWidths=col_widths)
            table.setStyle(TableStyle([
                ('BACKGROUND', (0, 0), (-1, 0), colors.lightgrey),
                ('TEXTCOLOR', (0, 0), (-1, 0), colors.black),
                ('GRID', (0, 0), (-1, -1), 0.5, colors.black),
                ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
                ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
                ('FONTNAME', (0, 0), (-1, -1), 'Helvetica'),
            ]))
            return table
        
        # Calibration Metrics (placeholder table for it to be updated later to have correct pass/fail)
        elements.append(Paragraph("<b>Calibration Summary</b>", styles["Heading2"]))
        summary_data = [
            ["Metric", "Pass/Fail"],
            ["Focus Value", "---"],
            ["Resolution", "---"],
            ["Light Intensity", "---"],
            ["Color Balance", "---"]
        ]
        summary_table = Table(summary_data, colWidths=[150, 150, 100], rowHeights=25)

        summary_table.setStyle(TableStyle([
            ('GRID', (0,0), (-1,-1), 1, colors.black),
            ('BACKGROUND', (0,0), (-1,0), colors.grey),
        ]))

        elements.append(summary_table)
        elements.append(Spacer(1, 20))

        # Focus Analysis
        elements.append(Paragraph("<b>Focus Analysis</b>", styles["Heading2"]))
        try:
            focusVal = Focus.computeSobelVarianceCombo(imgPath)
        except Exception as e:
            print("Error computing focus value:", e)
            focusVal = -1.0 
            focusThreshold = focusThreshold
        else:   
            print("Computed focus value:", focusVal)

        isFocusPass = focusVal >= focusThreshold
        summary_table._cellvalues[summary_row_index][1] = "Pass" if isFocusPass else "Fail"
        if isFocusPass:     summary_table.setStyle(TableStyle([('TEXTCOLOR', (1,summary_row_index), (1,summary_row_index), colors.green)]))
        else:               summary_table.setStyle(TableStyle([('TEXTCOLOR', (1,summary_row_index), (1,summary_row_index), colors.red)]))
        summary_row_index+=1


        focus_result_text = "Pass" if isFocusPass else "Fail"
        focus_text_color = "green" if isFocusPass else "red"
        focus_result_para = Paragraph(f'<font color="{focus_text_color}">{focus_result_text}</font>', styles["Normal"])
 
        elements.append(make_table([
            ["Focus Value", "Threshold", "Pass/Fail"],
            [f"{focusVal:.2f}", f"{focusThreshold}", focus_result_para]
        ], col_widths=[120, 120, 120]))
        elements.append(Spacer(1, 20))

        # Resolution Analysis
        elements.append(Paragraph("<b>Resolution Analysis</b>", styles["Heading2"]))

        try:
            cameraMatrix, reproductionError, distCoefficients = Resolution.calibrate()
        except Exception as e:
            print("Error during resolution calibration:", e)
            cameraMatrix = [[0.0, 0.0, 0.0], [0.0, 0.0, 0.0], [0.0, 0.0, 0.0]]
            reproductionError = float('inf')
            distCoefficients = [0.0, 0.0, 0.0, 0.0, 0.0]
        else:
            print("Resolution calibration successful.")

        cam_rows = [[f"{v:.3f}" for v in row] for row in cameraMatrix]

        elements.append(make_table(
            [["Camera Matrix", "", ""]] + cam_rows,
            col_widths=[120, 120, 120]
        ))
        elements.append(Spacer(1, 20))

        # Distortion Coefficients
        distCoefficients = distCoefficients.flatten()
        dist_rows = [[f"{v:.5f}"] for v in distCoefficients] 

        dist_rows.insert(0, ["Distortion Coefficients"])

        elements.append(make_table(
            dist_rows,
            col_widths=[120] 
        ))
        elements.append(Spacer(1, 20))

        # Reprojection Error
        elements.append(Paragraph("<b>Reprojection Error</b>", styles["Heading2"]))
        isReprojectPass = reproductionError < reproductionErrorThreshold
        summary_table._cellvalues[summary_row_index][1] = "Pass" if isReprojectPass else "Fail"
        if isReprojectPass:     summary_table.setStyle(TableStyle([('TEXTCOLOR', (1,summary_row_index), (1,summary_row_index), colors.green)]))
        else:               summary_table.setStyle(TableStyle([('TEXTCOLOR', (1,summary_row_index), (1,summary_row_index), colors.red)]))
        summary_row_index+=1


        reproduction_result_text = "Pass" if isReprojectPass else "Fail"
        reproduction_text_color = "green" if isReprojectPass else "red"
        reproduction_result_para = Paragraph(f'<font color="{reproduction_text_color}">{reproduction_result_text}</font>', styles["Normal"])
 
        elements.append(make_table([
            ["Reprojection Error", "Threshold", "Pass/Fail"],
            [f"{reproductionError:.4f}", reproductionErrorThreshold, reproduction_result_para]
        ], col_widths=[120, 120, 120]))
        elements.append(Spacer(1, 20))

        # Light Intensity
        elements.append(Paragraph("<b>Light Intensity Analysis</b>", styles["Heading2"]))

        try:
            intensity = Color.get_average_light_intensity(imgPathForLightIntensity)
        except Exception as e:
            print("Error computing light intensity:", e)
            intensity = -1.0 # Invalid value to indicate error
        else:   
            print("Computed light intensity:", intensity)

        isIntensityPass = min_intensity <= intensity <= max_intensity
        summary_table._cellvalues[summary_row_index][1] = "Pass" if isIntensityPass else "Fail"
        if isIntensityPass:     summary_table.setStyle(TableStyle([('TEXTCOLOR', (1,summary_row_index), (1,summary_row_index), colors.green)]))
        else:               summary_table.setStyle(TableStyle([('TEXTCOLOR', (1,summary_row_index), (1,summary_row_index), colors.red)]))
        summary_row_index+=1


        intensity_result_text = "Pass" if isIntensityPass else "Fail"
        intensity_text_color = "green" if isIntensityPass else "red"
        intensity_result_para = Paragraph(f'<font color="{intensity_text_color}">{intensity_result_text}</font>', styles["Normal"])
 
        elements.append(make_table([
            ["Light Intensity", "Good Range", "Pass/Fail"],
            [f"{intensity:.2f}", f"({min_intensity}, {max_intensity})", intensity_result_para]
        ], col_widths=[120, 150, 100]))
        elements.append(Spacer(1, 20))

        # Color Analysis 
        elements.append(Paragraph("<b>Color Balance Analysis</b>", styles["Heading2"]))

        color_dict = color_card.reference_colors
        summary_result = True

        try:
            measured_bgr = Color.colorbalance(colorcardImg, rows, cols)
        except Exception as e:
            print("Error during color balance analysis:", e)
            measured_bgr = [(0, 0, 0)] * len(color_dict)
        else:
            print("Color balance analysis successful.")

        data = [["Color", "HSV (H°, S%, V%)", "Sample", "Reference", "Pass/Fail"]]

        for i, (name, ref_rgb) in enumerate(color_dict.items()):
            meas_bgr = measured_bgr[i] if i < len(measured_bgr) else (0, 0, 0)
            b,g,r = meas_bgr
            meas_rgb = (r, g, b)  # Convert BGR to RGB

            meas_hsv = Color.rgb_to_hsv_tuple(meas_rgb)
            distance = Color.color_distance(meas_rgb, ref_rgb)
            h,s,v = meas_hsv
           
            colorPass = distance <= colorDistanceThreshold

            # If any color fails, summary_result becomes False for overall pass/fail
            if not colorPass:
                summary_result = False

            result = "Pass" if colorPass else "Fail"
            text_color = "green" if result == "Pass" else "red"
            result_para = Paragraph(f'<font color="{text_color}">{result}</font>', styles["Normal"])

            data.append([
                name,
                f"{h}°, {s}%, {v}%",    
                "",               
                "",               
                result_para                
            ])

        table = Table(data, colWidths=[120, 120, 60, 60, 60], rowHeights=25)

        style = TableStyle([
            ('GRID', (0,0), (-1,-1), 1, colors.black),
        ])

        # Apply sample + reference colors for visualization
        for row_index, ((name, ref_rgb), meas_bgr) in enumerate(zip(color_dict.items(), measured_bgr), start=1):
            
            # Sample 
            b, g, r = meas_bgr
            r = r / 255
            g = g / 255
            b = b / 255
            style.add('BACKGROUND', (2, row_index), (2, row_index), colors.Color(r, g, b))

            # Reference 
            r, g, b = [v/255 for v in ref_rgb]
            style.add('BACKGROUND', (3, row_index), (3, row_index), colors.Color(r, g, b))
        
        summary_table._cellvalues[summary_row_index][1] = "Pass" if summary_result else "Fail"
        if summary_result:     summary_table.setStyle(TableStyle([('TEXTCOLOR', (1,summary_row_index), (1,summary_row_index), colors.green)]))
        else:               summary_table.setStyle(TableStyle([('TEXTCOLOR', (1,summary_row_index), (1,summary_row_index), colors.red)]))
        summary_row_index+=1

        table.setStyle(style)
        elements.append(table)
        elements.append(Spacer(1, 20))

        # Build PDF
        try:
            doc.build(elements)
        except Exception as e:
            print("Error building PDF:", e)
        else:
            print("PDF report generated successfully.")


if __name__ == "__main__":
    Pdf.report()
