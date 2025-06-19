import json
import cv2
import numpy as np
import math

WHITE = (255, 255, 255)
BLACK = (0, 0, 0)
BASE = (50, 50, 50)
RED = (255, 0, 0)


def load_data(data_path = '/Users/mehulmathur/Desktop/Main Folder 0/Unity Projects/Review_Project_01/Assets/graph_data.dat'):
    with open(data_path, 'r') as file:
        data = json.load(file)

    nodes = data['nodes']
    return nodes


COLOR = (200, 200, 200)
    # for node in nodes[0:10]:
    #     position = node['position']
    #     x, y, z = position['x'], position['z'], position['z']
    #     outgoing_indices = node['outgoingIndices']
        
    #     print(f"Position: x={x}, y={y}, z={z}")
    #     print(f"Outgoing Indices: {outgoing_indices}")

def normalize_data(nodes, width, height):
    x_min = 1e9
    x_max = -1e9
    y_min = 1e9
    y_max = -1e9
    for node in nodes:
        position = node['position']
        x_min = min(x_min, position['x'])
        y_min = min(y_min, position['z'])
        x_max = max(x_max, position['x'])
        y_max = max(y_max, position['z'])

    for node in nodes:
        position = node['position']
        position['x'] -= x_min
        position['x'] *= height / (x_max - x_min)
        position['z'] -= y_min
        position['z'] *= width / (y_max - y_min)

        position['x'] = int(position['x'])
        position['z'] = int(position['z'])

    return nodes


def get_coord(node):
    return (int(node['position']['x']), int(node['position']['z']))

def direction_to_color(direction):
    # direction = direction/np.linalg.norm(direction)
    theta = math.atan2(direction[0], direction[1])
    theta_deg = math.degrees(theta)
    theta = theta_deg % 360
    k = abs(180-theta)*255/180
    # r = abs(180-theta)*255/180
    # g = 255 - abs(180-(60+theta))*255/180
    # b = 255 - abs(180-(120+theta))*255/180
    r = int(255*math.cos(math.radians(theta)))
    g = int(255*math.cos(math.radians(90-theta)))
    b = int(255*math.cos(math.radians(270-theta)))
    # if (theta == 0): print(r, g, b)
    color = (255-r, 255-g, 255-b) #(k, 255-k, 255-k)

    return color


def nodes_to_image(nodes, width, height):
    image = np.full((height, width, 3), BASE, dtype=np.uint8)
    
    for node in nodes:
            position = node['position']
            outgoing_indices = node['outgoingIndices']
            x, y = int(position['x']), int(position['z'])

            for outgoing_idx in outgoing_indices:
                x_, y_ = get_coord(nodes[outgoing_idx])
                image = cv2.line(image, (x, y), (x_, y_), direction_to_color((x_-x, y_-y)), 2)
            # image = cv2.circle(image, (x, y), 4, BLACK, -1)

    return image

if __name__ == '__main__':
    width = 8000
    height = 8000
    nodes = normalize_data(load_data(), width, height)

    image = nodes_to_image(nodes, width, height)
    image = image[::-1]
    cv2.imwrite('img.png', image)
    # cv2.imshow("converted", image)
    # cv2.waitKey(0)
    # cv2.destroyAllWindows()