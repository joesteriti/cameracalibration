# cameracalibration
Camera Calibration Program for Platypus Vision

Image Quality Analyzer & PDF Report Generator

This project processes images to evaluate several quality metrics—focus sharpness, light intensity, reprojection error, and color balance comparison —and automatically generates a PDF report summarizing the results.
It includes visual annotations, colored pass/fail indicators, and configurable thresholds.

Features:
Image Quality Analysis

Focus Value Detection
Measures sharpness using the Sobel Variance method. Other methods are implemented so that changing the method of measurement would be easy.

Light Intensity Calculation
Computes average brightness intensity values from a gray image.

Reprojection Error Evaluation
Assesses calibration output accuracy.

Color Balance Evaluation
Assesses the sample colors from the color card and compares them to true, standardized values.

PDF Report Generation
Uses ReportLab to build a structured report:

Tables with colored Pass (green) / Fail (red) statuses

Threshold values for transparency and calibration traceability


Configurable

You can modify:

Thresholds for pass/fail (this should be modified to suit the program better)

Included images and annotations
