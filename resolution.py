import numpy as np
import cv2
import glob
import os

class Resolution:
    
    #Throw all images collected in this list
    paths = [
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-13-41.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-22-23.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-22-48.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-23-03.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-24-07.csv.tif",            
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-24-37.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-25-21.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-25-35.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-25-50.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-26-12.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-26-30.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-26-45.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-27-00.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-27-23.csv.tif",
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-27-39.csv.tif",            
        "/Users/josephsteriti/Downloads/Platypus Vision/OneDrive_1_11-10-2025/Chess board/Characterization_2025-11-06_15-27-58.csv.tif",
        ]
    imgs = [cv2.imread(p) for p in paths]
        

    #IMPORT INFO
    #COUNT corners and rows correctly or else returns false
    #Need at least 10 INPUTTED images for good results
    #Images should be taken at different planes to avoid degenerate cases
    #Entire chessboard needs to be inside of the image in order to function
    def calibrate(showPics=True):

        #Loading image
        root = os.getcwd()
        calibrationDir = os.path.join(root,'')
        imgPathList = glob.glob(os.path.join(calibrationDir,'*.jpg'))

        #Initializing image
        #IMPORTANT COUNT ROWS AND COLUMNS
        nRows = 9
        nCols = 6
        terminationCriteria = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 30, 0.001)
        #Placeholders
        worldPtsCurrent = np.zeros((nRows*nCols,3), np.float32)
        worldPtsCurrent[:,:2] = np.mgrid[0:nRows, 0:nCols].T.reshape(-1,2)
        worldPtsList = []
        imgPtsList = []

        #Feeding each image of the chessboard in
        for curImgPath in Resolution.paths:
            imgBGR = cv2.imread(curImgPath)
            imgGray = cv2.cvtColor(imgBGR, cv2.COLOR_BGR2GRAY)
            cornersFound, cornersOrg = cv2.findChessboardCorners(imgGray, (nRows, nCols), None)

            if cornersFound == True:
                worldPtsList.append(worldPtsCurrent)
                #Finding more accurate location of the corners
                corners = cv2.cornerSubPix(imgGray,cornersOrg, (11,11), (-1,-1), terminationCriteria)
                imgPtsList.append(corners)
        #         if showPics:
        #             #Draws corners in memory
        #             cv2.drawChessboardCorners(imgBGR, (nRows, nCols), corners,cornersFound)
        #             cv2.imshow('Chessboard', imgBGR)
        #             cv2.waitKey(500)
        # cv2.destroyAllWindows

        reproductionError, cameraMatrix, distCoefficients, rvecs, tvecs = cv2.calibrateCamera(worldPtsList, imgPtsList, imgGray.shape[::-1], None, None)
        print('Camera Matrix:\n', cameraMatrix)
        print('Distortion Coefficients:\n', distCoefficients)
        print('Reprojection Error (pixels): {:.4f}'.format(reproductionError))
        return cameraMatrix, reproductionError, distCoefficients


if __name__ == '__main__':
    Resolution.calibrate()