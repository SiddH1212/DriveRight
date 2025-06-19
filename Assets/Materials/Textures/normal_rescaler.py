import cv2
import numpy as np

# Load the original normal map
normal_original = cv2.imread("../pebbled-asphalt1-unity/pebbled_asphalt_Normal-ogl.png")
normal_original = cv2.resize(normal_original, (10000, 10000))
# Target output size
width, height = 500, 500

# tiled_cropped = cv2.resize(normal_original, (width, height))

# Get original image size
tile_h, tile_w = normal_original.shape[:2]

# Compute number of tiles needed
tiles_x = (width + tile_w - 1) // tile_w  # Ceiling division
tiles_y = (height + tile_h - 1) // tile_h

# Tile the image using np.tile
tiled = np.tile(normal_original, (tiles_y, tiles_x, 1))

# Crop to exact target size
tiled_cropped = tiled[:height, :width]

# Save result
cv2.imwrite('intersection_normal.png', tiled_cropped)
