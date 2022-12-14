import sys
import pathlib

path = pathlib.Path(pathlib.Path(__file__).parent)
sys.path.append(str(path))

import cv2
import time
import torch
import random
import datetime
import evaluator
import numpy as np

from PIL import Image
from deeplabv3 import DeepLabV3Plus
from pathlib import Path
from torchvision import transforms
from dataset import PolygonDataset
from matplotlib import pyplot as plt
from torch.utils.data import DataLoader

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")
DEPENDENCIES = ["deeplabv3.py", "dataset.py", "evaluator.py"]


def train(data_list, logger, steps=400, plot_path=None):
    if plot_path is None:
        plot_path = Path.cwd()

    data_list, labels = data_list

    data_list = random.sample(data_list, len(data_list))
    dataset_train = PolygonDataset(data_list[0:int(len(data_list) * 0.8)], train=True)
    dataset_val = PolygonDataset(data_list[int(len(data_list) * 0.8):], train=True)
    batch_size = 4
    train_dataloader = DataLoader(dataset_train, batch_size=batch_size, shuffle=True, num_workers=0, drop_last=True)
    val_dataloader = DataLoader(dataset_val, batch_size=batch_size, shuffle=True, num_workers=0, drop_last=True)

    model = DeepLabV3Plus(len(labels) + 1).to(device)
    optimizer = torch.optim.Adam(model.parameters(), lr=0.001)
    criterion = torch.nn.CrossEntropyLoss(ignore_index=255)

    validation_data = []
    loss_data = []

    val_range = 100
    loss_sum = 0
    data_iter = iter(train_dataloader)

    start_time = time.time()
    logger.info("Training started...")
    logger.info("Used device: " + str(device))
    for step in range(steps):

        model.train()

        data = next(data_iter, None)
        if data is None:
            data_iter = iter(train_dataloader)
            data = next(data_iter)

        images, segmentations = data
        images = images.to(device)
        segmentations = segmentations.to(device)
        optimizer.zero_grad()
        predictions = model(images)

        loss = criterion(predictions, segmentations)
        loss_sum += loss.item()
        loss.backward()
        optimizer.step()

        if (step + 1) % 25 == 0:
            logger.info((str(step + 1) + "/" + str(steps) + " trainings steps done..."))

        if (step + 1) % val_range == 0:
            loss_data.append(loss_sum / (step * batch_size))
            logger_msg = "Loss after " + str(step + 1) + " steps (batch size = " + str(batch_size) + "): " \
                         + str(round(loss_data[-1], 3))
            logger.info(logger_msg)
            # Validation; labels_count = len(labels) + 1, because we didn't consider the background until now
            validation_data.append(execute_evaluation(model, val_dataloader, len(labels) + 1))
            logger_msg = "IoU after " + str(step + 1) + " steps: " + str(round(validation_data[-1], 3))
            logger.info(logger_msg)
            logger_msg = "The best IoU so far: " + str(round(max(validation_data), 3))
            logger.info(logger_msg)

            time_dif = round(time.time() - start_time)
            logger.info("Duration since the training started: " + str(datetime.timedelta(seconds=round(time_dif))))

    return model


def preprocess(ds_iter, logger, request_form=None):
    data_list = list(ds_iter)
    labels = ds_iter.annos[ds_iter.roles[0] + "." + ds_iter.schemes[0]].labels
    if len(data_list) < 1:
        logger.error("The number of available training data is too low!")

    return data_list, labels


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
            all_results = evaluator.get_intersection_and_union(predictions, segmentations, all_results, labels_count)

    return evaluator.calculate_intersection_over_union(all_results)


def predict(model, data, shape, logger):
    model = model.to(device)
    model.eval()
    probability_results = None

    dataset = PolygonDataset(data, train=True)
    batch_size = 8
    dataloader = DataLoader(dataset, batch_size=batch_size, shuffle=False, num_workers=0, drop_last=False)
    logger.info("Prediction started...")

    counter = 0
    for images, _ in dataloader:
        predictions = model(images.to(device)).to("cpu").detach().numpy()
        resized_predictions = np.zeros(shape=(predictions.shape[0], predictions.shape[1], shape[0], shape[1]))
        for frame_id, prediction in enumerate(predictions):
            for layer_id, layer in enumerate(prediction):
                resized_predictions[frame_id][layer_id] = cv2.resize(predictions[frame_id][layer_id],
                                                                     dsize=(shape[1], shape[0]),
                                                                     interpolation=cv2.INTER_NEAREST)
        probability_results = resized_predictions if probability_results is None else np.concatenate(
            (probability_results, resized_predictions), axis=0)

        counter += 1
        if counter % 25 == 0:
            logger.info(str(counter * batch_size) + " predictions done.")

    logger.info("All predictions successfully done.")

    return probability_results


def save(model, path):
    path = str(path) + ".pth"
    model.save_weights(path)
    return path


def load(path, classes, logger=None):
    path = str(path)
    model = DeepLabV3Plus(len(classes) + 1).to(device)
    model.load_weights(path)
    return model


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
