import cv2
import numpy as np
import random


from PIL import Image
from skimage.draw import polygon
from torchvision import transforms
from torch.utils.data import Dataset


class PolygonDataset(Dataset):
    def __init__(self, list_dataset, train=False):
        self.list_dataset = list_dataset
        self.num_images = len(self.list_dataset)
        self.train = train

    def __len__(self):
        return self.num_images

    def __getitem__(self, idx):
        data = self.list_dataset[idx]
        image = np.uint8(data[list(data)[1]])[0]
        label = np.uint8(polygon_to_mask(image.shape, data[list(data)[0]]))

        image, label = resize(image, label)

        image = transforms.ToTensor()(Image.fromarray(image))
        label = Image.fromarray(label).convert('L')
        label = transforms.ToTensor()(label)
        label = (label * 255).long()
        label.squeeze_(dim=0)

        if self.train:
            image, label = execute_augmentation(image, label)

        return [image, label]


def execute_augmentation(image, label):
    if random.uniform(0, 1) > 0.80:
        image = transforms.GaussianBlur(kernel_size=(5, 9), sigma=(0.1, 5))(image)

    if random.uniform(0, 1) > 0.9:
        image = transforms.RandomHorizontalFlip(p=1)(image)
        label = transforms.RandomHorizontalFlip(p=1)(label)

    if random.uniform(0, 1) > 0.9:
        image = transforms.RandomVerticalFlip(p=1)(image)
        label = transforms.RandomVerticalFlip(p=1)(label)

    if random.uniform(0, 1) > 0.9:
        image = transforms.Grayscale(num_output_channels=3)(image)

    if random.uniform(0, 1) > 0.9:
        image = transforms.ColorJitter(brightness=.5, hue=.3)(image)

    image = transforms.RandomInvert(p=0.10)(image)

    return image, label


def polygon_to_mask(size, polygons):
    mask = np.zeros(shape=size, dtype=np.int)

    polygons = polygons['polygons']
    for polygon in polygons:
        label = polygon['label']
        polygon_points = polygon['points']
        polygon_points = points_as_list(polygon_points)
        mask = fill_mask(mask, polygon_points, [label, label, label])

    return mask


def points_as_list(polygon_points):
    poly = np.zeros(shape=(len(polygon_points), 2))
    for counter, point in enumerate(polygon_points):
        poly[counter] = [point['x'], point['y']]
    return poly


def fill_mask(mask, polygon_points, color):
    rr, cc = polygon(polygon_points[:, 1], polygon_points[:, 0], [mask.shape[0], mask.shape[1]])
    mask[rr, cc, :] = color

    return mask


def resize(input_image, input_mask):
    output_image = cv2.resize(input_image, dsize=(512, 256), interpolation=cv2.INTER_NEAREST)
    output_mask = cv2.resize(input_mask, dsize=(512, 256), interpolation=cv2.INTER_NEAREST)

    return output_image, output_mask
