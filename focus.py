import numpy as np
import cv2
class Focus:
    def __init__(self):
        return None
    #https://opencv.org/blog/autofocus-using-opencv-a-comparative-study-of-focus-measures-for-sharpness-assessment/

    

    #Implement all of the options to see which one produces the best results, Sobel Variance seems to be the most reliable
    #All have pros and cons considering environment
    def computeLaplacian(img):
        imgGray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        laplacian = cv2.Laplacian(imgGray, cv2.CV_64F)
        focusValue= np.var(laplacian)
        focusThreshold= 10000
        return focusValue, focusThreshold

    def computeLocalVariance(img):
        imgGray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        ksize=5
        mean = cv2.blur(imgGray, (ksize, ksize))
        squared_mean = cv2.blur(imgGray**2, (ksize, ksize))
        variance = squared_mean - (mean**2)
        focusValue= np.mean(variance)
        focusThreshold= 200
        return focusValue, focusThreshold


    def computeTenengrad(img):
        imgGray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        sobel_x=cv2.Sobel(imgGray, cv2.CV_64F, 1, 0, ksize=3)#Sobel x gradient
        sobel_y=cv2.Sobel(imgGray, cv2.CV_64F, 0, 1, ksize=3)#Sobel y gradient
        tenengrad = np.sqrt(sobel_x**2+sobel_y**2)
        focusValue= np.mean(tenengrad)
        focusThreshold=100
        return focusValue, focusThreshold

    def computeBrenner(img):
        imgGray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        shifted = np.roll(imgGray, -2, axis=1)  # Shift by 2 pixels horizontally
        diff = (imgGray - shifted) ** 2
        focusValue= np.sum(diff)
        focusThreshold=5000
        return focusValue, focusThreshold

    def computeSobelVarianceCombo(img):
        imgGray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        sobel_x=cv2.Sobel(imgGray, cv2.CV_64F, 1, 0, ksize=3)#Sobel x gradient
        sobel_y=cv2.Sobel(imgGray, cv2.CV_64F, 0, 1, ksize=3)#Sobel y gradient
        sobel_magnitude = np.sqrt(sobel_x**2+sobel_y**2)
        variance = np.var(imgGray)

        focusValue= round(np.mean(sobel_magnitude) + variance, 2)
        print("Sobel Variance Focus Value:", focusValue)
        return focusValue
