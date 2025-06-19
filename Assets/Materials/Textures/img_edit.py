import cv2
import numpy as np

# Load swatch image
swatch_img = cv2.imread('road_texture_6.png')

buff = np.zeros_like(swatch_img)

for i in range(len(buff)):
    for j in range(len(buff[0])):
        buff[i][j] = swatch_img[10][10]

# Get swatch color (top-left pixel)
swatch_color = swatch_img[0, 0].astype(np.int16)

# Threshold for color similarity
thresh = 50

# Compute difference between every pixel and swatch color
diff = np.linalg.norm(swatch_img.astype(np.int16) - swatch_color, axis=2)

# Create mask of similar pixels
mask = diff < thresh

# Apply swatch color to matching pixels
swatch_img[mask] = swatch_color.astype(np.uint8)

# Show modified swatch image
cv2.imshow('Cleaned Swatch Image', swatch_img)
cv2.imshow("orig", cv2.imread('road_texture_6.png'))
cv2.imwrite('swatch.png', buff)
# cv2.imwrite('road_texture_6.png', swatch_img)
# cv2.waitKey(0)
# cv2.destroyAllWindows()
