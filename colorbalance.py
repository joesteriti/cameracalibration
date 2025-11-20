import cv2
import math
import numpy as np

class Color:

    def get_average_light_intensity(image):

        if image is None:
            print("Error: Image not found or invalid path.")
            return -1

        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

        avg_light_intensity = float(np.mean(gray))

        return avg_light_intensity

    # Extend a line segment by a specified length in both directions, this was to account for Hough line detection not fully covering the grid lines
    def extend_line(x1, y1, x2, y2, length=20):
        
        dx = x2 - x1
        dy = y2 - y1
        line_len = math.hypot(dx, dy)
        if line_len == 0:
            return x1, y1, x2, y2
        scale = length / line_len
        x1_new = int(x1 - dx * scale)
        y1_new = int(y1 - dy * scale)
        x2_new = int(x2 + dx * scale)
        y2_new = int(y2 + dy * scale)
        return x1_new, y1_new, x2_new, y2_new

    def colorbalance(img, rows=6, cols=4):
        
        blurred = cv2.GaussianBlur(img, (5,5), 0)
        img_hsv = cv2.cvtColor(blurred, cv2.COLOR_BGR2HSV)
        split = cv2.split(img_hsv)
        hue = split[0]
        sat = split[1]
        val = split[2]

        ret,thresh1 = cv2.threshold(val,60,255, cv2.THRESH_BINARY)
        ret,thresh2 = cv2.threshold(sat,60,255, cv2.THRESH_BINARY)

        mask = cv2.bitwise_and(thresh1, thresh2)

        # cv2.imshow("HSV",img_hsv)
        # cv2.imshow("Val", thresh1)
        # cv2.imshow("Sat", thresh2)

        # cv2.imshow("Both Sat and Val", mask)

        edges = cv2.Canny(mask, 30, 180)

        # Clean small noise (optional)
        kernel = np.ones((3,3), np.uint8)
        edges = cv2.morphologyEx(edges, cv2.MORPH_CLOSE, kernel)
        contours, _ = cv2.findContours(edges, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        thin_lines = img.copy()
        cv2.drawContours(thin_lines, contours, -1, (0,255,0), 4)

        # cv2.imshow("Thin Lines", thin_lines)
        # cv2.imshow("Edges", edges)

        contours, ret = cv2.findContours(edges, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

        largest_contour= max(contours, key = cv2.contourArea)
        x, y, w, h = cv2.boundingRect(largest_contour)

        cropped_image = mask[y:y+h, x:x+w]
        cropped_color = img[y:y+h, x:x+w].copy()  # original color crop

        cropped_edges = edges[y:y+h, x:x+w]  # crop edges too


        # cv2.imshow("Cropped", cropped_image)
        # cv2.imshow("Cropped Edges", cropped_edges)


        #Using Hough Transform to detect lines of the black grid
        lines = cv2.HoughLinesP(
            cropped_edges,
            rho=1,
            theta=np.pi/180,
            threshold=80,        
            minLineLength=30,   
            maxLineGap=40
        )

        grid_vis = img[y:y+h, x:x+w].copy()  # 3-channel BGR

        if lines is not None:
            for line in lines:
                x1, y1, x2, y2 = line[0]  
                x1, y1, x2, y2 = Color.extend_line(x1, y1, x2, y2, length=350)
                cv2.line(grid_vis, (x1,y1), (x2,y2), (0,0,255), 4)

        # cv2.imshow("Grid Lines", grid_vis)

        cell_h = h // rows
        cell_w = w // cols

        # Store average colors
        avg_colors = np.zeros((rows, cols, 3), dtype=np.uint8)

        median_vis = np.zeros_like(grid_vis)

        median_colors = []
        row_colors = []


        for r in range(rows):
            for c in range(cols):
                # Compute cell coordinates
                x_start = c * cell_w
                y_start = r * cell_h
                x_end = x_start + cell_w
                y_end = y_start + cell_h
                
                # Crop the cell
                cell = grid_vis[y_start:y_end, x_start:x_end]
                
                # Compute average color
                avg = cell.mean(axis=(0,1))  # mean over height and width
                avg_color = avg.astype(np.uint8)
                avg_colors[r, c] = avg_color

                median_color = np.median(cell.reshape(-1, 3), axis=0)
                row_colors.append(median_color)

                
                # Fill the cell in the visualization image
                grid_vis[y_start:y_end, x_start:x_end] = avg_color
                median_vis[y_start:y_end, x_start:x_end] = median_color

            
            median_colors.append(row_colors)

            # Show the averaged image
        # cv2.imshow("Average Colors per Cell", grid_vis)
        cv2.imshow("Median Colors per Cell", median_vis)


        cv2.waitKey(0)
        cv2.destroyAllWindows()
        flat_median_colors = [color for row in median_colors for color in row] #Using median colors for better accuracy (to avoid large outliers)

        #print(flat_median_colors)

        # print("Top-left cell median RGB:", flat_median_colors[0])

        # Access bottom-right cell (last one)
        # print("Bottom-right cell median RGB:", flat_median_colors[-1])
        return flat_median_colors #IN BGR FORMAT
    
    #Helper functions for color conversions 
    def rgb_to_hsv_tuple(rgb):
        # rgb: tuple (R, G, B) with 0-255
        color_np = np.uint8([[rgb]])
        hsv_np = cv2.cvtColor(color_np, cv2.COLOR_RGB2HSV)
        h, s, v = hsv_np[0][0]

        h = int(h) * 2        # OpenCV H: 0-179 → 0-358
        s = int((s / 255) * 100)
        v = int((v / 255) * 100)

        return (h, s, v)

    #Helper function to compute color distance (Euclidean) between two RGB colors. This is used to compare measured colors against reference colors. 
    #I have the CIEDE2000 function implemented in C# but for simplicity and speed Euclidean distance is sufficient for now. I'll include my C# implementation in the repo as well.
    def color_distance(sample_rgb, reference_rgb):
        sample = np.array(sample_rgb, dtype=float)
        reference = np.array(reference_rgb, dtype=float)
        distance = np.linalg.norm(sample - reference)
        return distance

