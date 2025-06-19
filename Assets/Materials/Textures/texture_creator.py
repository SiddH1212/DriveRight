import cv2
import numpy as np

width = 8000
height = 1000
side_padding = 60

line_width = 40
lane_width = 20
line_length = 250

bidirectional = False
n_lanes = 1

lane_color = (200, 200, 0)
k = 80
swatch_color = cv2.cvtColor(cv2.imread('swatch.png'), cv2.COLOR_BGR2RGB)[0][0]
road_color = swatch_color # (k, k, k)
l = 210
line_color = (l, l, l)


height_new = int(height*(n_lanes)/2)
road_image = np.zeros((height_new, width, 3), dtype=np.uint8)

for i in range(height_new):
    for j in range(width):
        color = road_color
        if (i < side_padding or height_new - i < side_padding):
            color = road_color
        elif ((i >= side_padding and i <= side_padding + lane_width) or (height_new - i >= side_padding and height_new - i <= side_padding + lane_width)):
            color = lane_color
        elif (i%(height/2) >= (height - line_width)/2 and 
              i%(height/2) <= (height + line_width)/2 and 
              (j+3*line_length/2) % (2*line_length) < line_length):
            color = line_color

        road_image[i][j] = color

# breakpoint()

if bidirectional:
    road_image_new = np.zeros((len(road_image)*2, len(road_image[0]), 3), np.uint8)
    h = road_image.shape[0]
    road_image_new[0:h] = road_image
    road_image_new[h:2*h] = road_image

    road_image = road_image_new

road_image_bgr = cv2.cvtColor(road_image, cv2.COLOR_RGB2BGR)
cv2.imshow(f"road_{n_lanes}lanes_{bidirectional}", road_image_bgr)
if bidirectional: cv2.imwrite(f'road_{n_lanes}_bi.png', road_image_bgr)
else: cv2.imwrite(f'road_{n_lanes}.png', road_image_bgr)
cv2.waitKey(0)
cv2.destroyAllWindows()
