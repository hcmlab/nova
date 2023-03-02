import sys
import pathlib

path = pathlib.Path(pathlib.Path(__file__).parent)
sys.path.append(str(path))

import cv2
import time
import torch
import random
import datetime
import numpy as np
import polygon_utils
import model_interface

from PIL import Image
from pathlib import Path
from torchvision import transforms
from dataset import PolygonDataset
from deeplabv3 import DeepLabV3Plus
from matplotlib import pyplot as plt
from torch.utils.data import DataLoader

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")


class TrainerClass:

    def __init__(self, ds_iter, logger):
        self.model = None
        self.ds_iter = ds_iter
        self.logger = logger
        self.predictions = None
        self.data = None
        self.labels = None
        self.start_frame = None
        self.DEPENDENCIES = ["deeplabv3.py", "dataset.py", "evaluator.py", "polygon_utils.py"]
        self.OPTIONS = {}

    def preprocess(self):
        data_list = list(self.ds_iter)
        last_id = 0
        for id, data_point in enumerate(data_list):
            if len(data_point[self.ds_iter.roles[0] + "." + self.ds_iter.schemes[0]]['polygons']) > 0:
                last_id = id

        if last_id == 0:
            labeled_data_list = []
            unlabeled_data_list = data_list
        else:
            labeled_data_list = data_list[:last_id + 1]
            unlabeled_data_list = data_list[last_id + 1:]

        labels = self.ds_iter.annos[self.ds_iter.roles[0] + "." + self.ds_iter.schemes[0]].labels
        if len(data_list) < 1:
            self.logger.error("The number of available training data is too low!")

        start_frame = data_list[0][self.ds_iter.roles[0] + "." + self.ds_iter.schemes[0]]['name']
        self.logger.info("Amount of labeled data: " + str(len(labeled_data_list)))
        self.logger.info("Amount of unlabeled data: " + str(len(unlabeled_data_list)))
        self.logger.info("Start-frame for training/prediction: " + str(start_frame))

        self.data = [labeled_data_list, unlabeled_data_list]
        self.labels = labels
        self.start_frame = start_frame

    def train(self):
        steps = 800

        labeled_data_list = self.data[0]
        labeled_data_list = random.sample(labeled_data_list, len(labeled_data_list))
        dataset_train = PolygonDataset(labeled_data_list[0:int(len(labeled_data_list) * 0.9)], train=True)
        dataset_val = PolygonDataset(labeled_data_list[int(len(labeled_data_list) * 0.9):])
        batch_size = 8
        train_dataloader = DataLoader(dataset_train, batch_size=batch_size, shuffle=True, num_workers=0, drop_last=True)
        val_dataloader = DataLoader(dataset_val, batch_size=batch_size, shuffle=True, num_workers=0, drop_last=True)

        self.model = DeepLabV3Plus(len(self.labels) + 1).to(device)
        optimizer = torch.optim.Adam(self.model.parameters(), lr=0.00025)
        criterion = torch.nn.CrossEntropyLoss(ignore_index=255)

        validation_data = []
        loss_data = []

        val_range = 50
        loss_sum = 0
        data_iter = iter(train_dataloader)

        start_time = time.time()
        self.logger.info("Training started...")
        self.logger.info("Used device: " + str(device))
        for step in range(steps):

            self.model.train()

            data = next(data_iter, None)
            if data is None:
                data_iter = iter(train_dataloader)
                data = next(data_iter)

            images, segmentations = data
            images = images.to(device)
            segmentations = segmentations.to(device)
            optimizer.zero_grad()
            predictions = self.model(images)

            loss = criterion(predictions, segmentations)
            loss_sum += loss.item()
            loss.backward()
            optimizer.step()

            if (step + 1) % 25 == 0:
                self.logger.info((str(step + 1) + "/" + str(steps) + " trainings steps done..."))

            if (step + 1) % val_range == 0:
                loss_data.append(loss_sum / (step * batch_size))
                logger_msg = "Loss after " + str(step + 1) + " steps (batch size = " + str(batch_size) + "): " \
                             + str(round(loss_data[-1], 5))
                self.logger.info(logger_msg)
                validation_data.append(
                    polygon_utils.execute_evaluation(self.model, val_dataloader, len(self.labels) + 1))
                logger_msg = "IoU after " + str(step + 1) + " steps: " + str(round(validation_data[-1], 5))
                self.logger.info(logger_msg)
                logger_msg = "The best IoU so far: " + str(round(max(validation_data), 5))
                self.logger.info(logger_msg)

                time_dif = round(time.time() - start_time)
                self.logger.info(
                    "Duration since the training started: " + str(datetime.timedelta(seconds=round(time_dif))))

    def predict(self):
        unlabeled_data_list = self.data[1]
        shape = list(self.ds_iter.data_info.values())[0].sample_data_shape
        batch_size = 4
        self.model.eval()
        self.predictions = np.zeros(shape=(len(unlabeled_data_list), len(self.labels) + 1, shape[0], shape[1]),
                                    dtype=np.float64)

        dataset = PolygonDataset(unlabeled_data_list)
        dataloader = DataLoader(dataset, batch_size=batch_size, shuffle=False, num_workers=0, drop_last=False)
        self.logger.info("Prediction started.")

        counter = 0
        for images, _ in dataloader:
            predictions = self.model(images.to(device)).to("cpu").detach().numpy()
            resized_predictions = np.zeros(shape=(predictions.shape[0], predictions.shape[1], shape[0], shape[1]))
            for frame_id, prediction in enumerate(predictions):
                for layer_id, layer in enumerate(prediction):
                    resized_predictions[frame_id][layer_id] = cv2.resize(predictions[frame_id][layer_id],
                                                                         dsize=(shape[1], shape[0]),
                                                                         interpolation=cv2.INTER_NEAREST)

            self.predictions[counter * batch_size:counter * batch_size + batch_size, :, :, :] = resized_predictions

            counter += 1
            if counter % 25 == 0:
                self.logger.info(str(counter * batch_size) + " predictions done.")

        self.logger.info("All predictions done.")

    def postprocess(self):
        self.logger.info("Starting postprocessing.")
        # 1. Create True/False Bitmaps
        binary_masks = polygon_utils.prediction_to_binary_mask(self.predictions)
        self.logger.info("Bitmaps created!")
        # 2. Get Polygons
        all_polygons = polygon_utils.mask_to_polygons(binary_masks)
        self.logger.info("Polygons extracted!")
        # 3. Get Confidences
        confidences = polygon_utils.get_confidences_from_predictions(self.predictions, all_polygons)
        self.logger.info("Starting postprocessing done.")

        return [all_polygons, confidences]

    def save(self, path):
        self.logger.info("Saving model...")
        path = str(path) + ".pth"
        self.model.save_weights(path)
        self.logger.info("...done.")
        return path

    def load(self, path):
        self.logger.info("Loading model...")
        path = str(path)
        self.model = DeepLabV3Plus(len(self.labels) + 1).to(device)
        self.model.load_weights(path)
        self.logger.info("...done.")
        return self.model
