# import cv2
# import glob
# import os
# import matplotlib.pyplot as plt
# from skimage import feature,color, io
# import numpy as np
# class Colorspace:
    
#     #Should take all color images, collect them into 1 image, identify each color and then compare it to the limits of the absolute (ny name???)

#     #sRGB Measurement Data for the 24ColorCard + 1 neutral color for the GreyBalanceCard
#     drkTone = (116, 81, 67)
#     ltTone = (199, 147, 129)
#     skyBlue = (91, 122, 156)
#     treeGrn = (90, 108, 64)
#     ltBlue = (130, 128, 176)
#     bluGrn = (92, 190, 172)
#     orange = (224, 124, 47)
#     medBlu = (68, 91, 170)
#     ltRed = (198, 82, 97)
#     purple = (94, 58, 106)
#     yelGrn = (159, 189, 63)
#     orgYel = (230, 162, 39)
#     blue = (34, 63, 147)
#     green = (67, 149, 74)
#     yellow = (238, 198, 32)
#     red = (180, 49, 47)
#     magenta = (193, 84, 151)
#     cyan = (12, 136, 170)
#     white = (243, 238, 243)
#     ltGrey = (200, 202, 202)
#     grey = (161, 162, 161)
#     drkGrey = (120, 121, 120)
#     charcoal = (82, 83, 83)
#     black = (49, 48, 51)

#     neutral = (220, 218, 220)
#     colorDict={
#     "Dark Tone":drkTone,
#     "Light Tone":ltTone,
#     "Sky Blue":skyBlue,
#     "Tree Green":treeGrn,
#     "Light Blue":ltBlue,
#     "Blue Green":bluGrn,
#     "Orange":orange,
#     "Medium Blue":medBlu,
#     "Light Red":ltRed,
#     "Purple":purple,
#     "Yellow Green":yelGrn,
#     "Orange Yellow":orgYel,
#     "Blue":blue,
#     "Green":green,
#     "Yellow":yellow,
#     "Red":red,
#     "Magenta":magenta,
#     "Cyan":cyan,
#     "White":white,
#     "Light Grey":ltGrey,
#     "Grey":grey,
#     "Dark Grey":drkGrey,
#     "Charcoal":charcoal,
#     "Black":black,
#     "Neutral":neutral
#     }
#     colorList = [drkTone,
#     ltTone,
#     skyBlue,
#     treeGrn,
#     ltBlue,
#     bluGrn,
#     orange,
#     medBlu,
#     ltRed,
#     purple,
#     yelGrn,
#     orgYel,
#     blue,
#     green,
#     yellow,
#     red,
#     magenta,
#     cyan,
#     white,
#     ltGrey,
#     grey,
#     drkGrey,
#     charcoal,
#     black,
#     neutral]

#     notAcceptedColorsList = []


#     #Function to Convert BGR image to HSV Color Space
#     def bgrToHsv(img):
#         hsvImg = cv2.cvtColor(img, cv2.COLOR_BGR2HSV_FULL)
#         return hsvImg

#     #Sets upper and lower limits (threshold can be changed) to set a range to be in for the color
#     def defineBounds(hsvColor, value):
#         lowerbound = np.array([hsvColor[0]-value, hsvColor[1]-value, hsvColor[2]-value])
#         upperbound = np.array([hsvColor[0]+value, hsvColor[1]+value, hsvColor[2]+value])
#         if lowerbound[0]< 0:
#             lowerbound[0] = 0
#         if lowerbound[1]< 0:
#             lowerbound[1] = 0
#         if lowerbound[2]< 0:
#             lowerbound[2] = 0
        
#         if upperbound[0]> 179:
#             upperbound[0] = 179
#         if upperbound[1]> 255:
#             upperbound[1] = 255
#         if upperbound[2]> 255:
#             upperbound[2] = 255
        
#         return lowerbound, upperbound

#     #Function to display image on screen
#     def displayImg(img):
#         cv2.imshow('Image', img)
#         cv2.waitKey(0)
        

#     def readNotAcceptedColorList():
#         print(f"The following colors are not within the acceptable color range: {Colorspace.notAcceptedColorsList}")
                
#     def compare(sampleColor, color):
#         for sampleColor in range(3):
#             lower, upper = Colorspace.defineBounds(color, 50)
#             if sampleColor[range] >= lower[range] & color[range] <= upper[range]:
#                 continue
#             else:
#                 return False
#         return True
    
#     #Calculates the average RGB value of the blobs detected
#     def calculateAverageRGB():
        
#         return averageRGB

#     #Identifies the blobs 
#     def blobDetection(mask, sampleImg):
#         params = cv2.SimpleBlobDetector_Params()

#         params.filterByArea = True
#         params.minArea = 10
#         params.filterByCircularity = False
#         params.filterByConvexity = False
#         params.filterByInertia = False

#         detector = cv2.SimpleBlobDetector_create(params)
#         sampleImg_GRAY = cv2.cvtColor(sampleImg, cv2.COLOR_BGR2GRAY)

#         keypoints = detector.detect(mask)
#         img_with_keypoints = cv2.drawKeypoints(sampleImg, keypoints, 
#         np.array([]), (0, 0, 255), 
#         cv2.DRAW_MATCHES_FLAGS_DRAW_RICH_KEYPOINTS)
#         cv2.imshow('Blob Detection', img_with_keypoints)
#         cv2.waitKey(0)
#         averageRGB = calc()
#         return averageRGB
            
#     #Goes through each color and creates a mask to see if the values are in the image
#     #Returns True if all colors match absolute, false even if 1 fails
#     def compareColors(color):
#         match = True
        
#             #STILL NEED PICS (maybe add it in the dictionary too!!!)
#         sampleImg = cv2.imread()
#         sampleImg_hsv = cv2.cvtColor(sampleImg, cv2.COLOR_BGR2HSV)

#         color = cv2.cvtColor(color, cv2.COLOR_RGB2HSV)
#         lb, ub = Colorspace.defineBounds(color, 40)
#         lb = np.array(lb, dtype="uint8")
#         ub = np.array(ub, dtype="uint8")

#         mask = cv2.inRange(sampleImg_hsv,lb,ub)

#         final = cv2.bitwise_and(sampleImg, sampleImg, mask = mask)
#         avg = blobDetection(mask, sampleImg)
#         hue = avg[0]
#         saturation = avg[1]
#         value = avg[2]
#             #calculate average
#             #Hue
#         if hue < lb and hue >ub:
#             match = False
#             Colorspace.notAcceptedColorsList.append(color)
#             #Saturation
#         if saturation < lb and saturation>ub:
#             match = False
#             Colorspace.notAcceptedColorsList.append(color)
#             #Value
#         if value < lb and value>ub:
#             match = False
#             Colorspace.notAcceptedColorsList.append(color)
                
#         return match, hue, saturation, value



# def main():
#         inbounds = False

#         sampleImg = cv2.imread('/Users/josephsteriti/Downloads/Platypus Vision/cameracalibration/assets/imgs/Pic_2025_08_13_144717_6.bmp')
#         sampleImg_hsv = cv2.cvtColor(sampleImg, cv2.COLOR_BGR2HSV)
#         grey = (Colorspace.grey[2],Colorspace.grey[1],Colorspace.grey[0])
#         print(f"Grey RGB{grey}")
        
#         print(f"Grey Value{grey}")
#         lb, ub = Colorspace.defineBounds(grey, 40)
#         lb = np.array(lb, dtype="uint8")
#         ub = np.array(ub, dtype="uint8")
#         print(f"Lower bound: {lb}, Upper bound: {ub}")

#         mask = cv2.inRange(sampleImg_hsv,lb,ub)

#         final = cv2.bitwise_and(sampleImg, sampleImg, mask = mask)

        

#         #Blob detection
#         params = cv2.SimpleBlobDetector_Params()

#         params.filterByArea = True
#         params.minArea = 10
#         params.filterByCircularity = False
#         params.filterByConvexity = False
#         params.filterByInertia = False

#         detector = cv2.SimpleBlobDetector_create(params)
#         sampleImg_GRAY = cv2.cvtColor(sampleImg, cv2.COLOR_BGR2GRAY)

#         keypoints = detector.detect(mask)
#         img_with_keypoints = cv2.drawKeypoints(sampleImg, keypoints, 
#         np.array([]), (0, 0, 255), 
#         cv2.DRAW_MATCHES_FLAGS_DRAW_RICH_KEYPOINTS)
#         cv2.imshow('Image', sampleImg)
#         cv2.waitKey(0)
#         cv2.imshow('Blob Detection', img_with_keypoints)
#         cv2.waitKey(0)
#         cv2.imshow('Mask', mask)
#         cv2.waitKey(0)
#         cv2.imshow('Final (only Grey)', final)
#         cv2.waitKey(0)
#         cv2.destroyAllWindows()
#         #color()

# if __name__ == '__main__':
#     main()