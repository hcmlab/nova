import torch
import evaluator
import numpy as np
import rasterio.features

from imantics import Mask
from scipy.special import softmax
from matplotlib import pyplot as plt
from shapely.geometry import Polygon


device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")


def prediction_to_binary_mask(predictions):
    predictions_label = np.argmax(predictions, axis=1)
    binary_masks = []
    for id, mask in enumerate(predictions):
        binary_masks_for_class = []
        # We don't want the background polygons, so we start not with 0 but 1
        for label in range(1, len(predictions[id])):
            binary_masks_for_class.append(np.where(label == predictions_label[id], True, False))

        binary_masks.append(binary_masks_for_class)

    return binary_masks


def mask_to_polygons(masks, min_pixel_amount=30):
    def PolyArea(x, y):
        return 0.5 * np.abs(np.dot(x, np.roll(y, 1)) - np.dot(y, np.roll(x, 1)))

    polygons_per_mask = []

    for mask in masks:
        polygons = []

        for layer in mask:
            polygons_in_layer = []
            polygons_for_layer = Mask(layer).polygons()
            for polygon in polygons_for_layer:
                if len(polygon) >= 6 * 3 and PolyArea(polygon[0::2], polygon[1::2]) >= min_pixel_amount:
                    # The polygons have too many points, so remove some (the user can add some afterwards, if necessary)
                    x = polygon[0::2][0::3]
                    y = polygon[1::2][0::3]
                    polygons_in_layer.append(np.array([item for items in zip(x, y) for item in items]))
            polygons.append(polygons_in_layer)
        polygons_per_mask.append(polygons)

    return polygons_per_mask


def get_confidences_from_predictions(confidences_layer, all_polygons):
    predictions_in_percentage = softmax(confidences_layer, axis=1)
    confidences = []
    height = confidences_layer.shape[2]
    width = confidences_layer.shape[3]

    for frame_id, polygons_per_frame in enumerate(all_polygons):
        label_type_polygons_confidences = []
        for label_id, polygons_per_label in enumerate(polygons_per_frame):
            label_confidences = []
            for polygon in polygons_per_label:
                polygon_bitmap = rasterio.features.rasterize([Polygon(np.reshape(polygon, newshape=[int(polygon.shape[0]/2), 2]))], out_shape=(height, width))
                confidence = np.zeros(shape=[height, width], dtype=float)
                confidence = np.where(polygon_bitmap != confidence, predictions_in_percentage[frame_id][label_id + 1], 0)
                confidence = round(np.sum(confidence) / np.sum(polygon_bitmap), 2)

                label_confidences.append(confidence)
            label_type_polygons_confidences.append(label_confidences)
        confidences.append(label_type_polygons_confidences)

    return confidences


def execute_evaluation(model, dataloader, labels_count):
    model.eval()
    all_results = {'intersection': torch.zeros(labels_count - 1).to(device).long(),
                   'union': torch.zeros(labels_count - 1).to(device).long()}

    with torch.no_grad():
        for images, segmentations in dataloader:
            images = images.to(device)
            segmentations = segmentations.to(device)
            predictions = model(images)

            predictions = torch.argmax(predictions, dim=1)
            all_results = evaluator.get_intersection_and_union(predictions, segmentations, all_results,
                                                               labels_count)

    return evaluator.calculate_intersection_over_union(all_results)


def print_images(img1, img2):
    if type(img1).__module__ != np.__name__:
        img1 = img1.cpu().detach().numpy()
    if type(img2).__module__ != np.__name__:
        img2 = img2.cpu().detach().numpy()

    if img1.shape[0] == 3:
        img1 = np.moveaxis(img1, 0, 2)
    if img2.shape[0] == 3:
        img2 = np.moveaxis(img2, 0, 2)

    fig = plt.figure(figsize=(10, 7))
    fig.add_subplot(1, 2, 1)
    plt.imshow(img1)
    plt.axis("off")
    plt.title("Mask")

    fig.add_subplot(1, 2, 2)
    plt.imshow(img2)
    plt.axis("off")
    plt.title("Image")
    plt.show()


def print_polygons(image_shape, polygons, colors: []):
    plt.rcParams["figure.figsize"] = [image_shape[0], image_shape[1]]
    plt.rcParams["figure.autolayout"] = True
    for polygons_per_label in polygons:
        for id, polygon in enumerate(polygons_per_label):
            x = polygon[0::2]
            y = polygon[1::2]
            plt.plot(x, y, c=colors[id])

    plt.show()
