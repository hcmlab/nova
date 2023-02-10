import os
import sys
import math
import pickle
import numpy as np
from pathlib import Path

if not hasattr(sys, 'argv'):
    sys.argv = ['']

from sklearn.svm import SVR
from sklearn.pipeline import make_pipeline
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import GridSearchCV
from sklearn.preprocessing import normalize


class TrainerClass:
    """Includes all the necessary files to run this script"""

    def __init__(self, ds_iter, logger, request_form):
        self.model = None
        self.ds_iter = ds_iter
        self.logger = logger
        self.data = None
        self.predictions = None
        self.DEPENDENCIES = []
        self.OPTIONS = {'C': '0.1', 'number_folds': '3', 'dual': 'True', 'method': 'sigmoid'}
        self.request_form = request_form
        self.MODEL_SUFFIX = '.model'

    def preprocess(self):
        """Possible pre-processing of the data. Returns a list with the pre-processed data."""
        data_list = list(self.ds_iter)
        data_list.sort(key=lambda x: int(x["frame"].split("_")[0]))

        # Get keys for all roles

        x = []
        y = []
        for r in self.request_form["roles"].split(';'):
            x_tmp = [val[r + '.' + self.request_form["streamName"].split(" ")[0]] for val in data_list]
            y_tmp = [val[r + '.' + self.request_form["scheme"].split(";")[0]] for val in data_list]
            x.extend(x_tmp)
            y.extend(y_tmp)

        self.data = np.ma.concatenate(x, axis=0), np.array(y)

    def train(self):
        """Trains a model with the given data. Returns this model."""
        x, y = self.data

        indices = []
        for id, value in enumerate(y):
            if math.isnan(value):
                indices.append(id)

        y = np.delete(y, indices)
        x = np.delete(x, indices, axis=0)

        self.model = SVR(kernel="linear", C=1, gamma="auto")
        self.model.fit(x, y)
        print('train_x shape: {} | train_x[0] shape: {}'.format(x.shape, x[0].shape))
        print('train_y shape: {} | train_y[0] shape: {}'.format(y.shape, y[0].shape))

    def predict(self):
        """Predicts the given data with the given model. Returns a list with the predicted values."""
        x, _ = self.data
        self.predictions = self.model.predict(x)

    def postprocess(self) -> list:
        """Possible pro-processing of the data. Returns a list with the pro-processed data."""
        result = {"values": [], "confidences": []}
        for prediction in self.predictions:
            result["values"].append(prediction)
            result["confidences"].append(1)

        return result

    def save(self, path) -> str:
        """Stores the weights of the given model at the given path. Returns the path of the weights."""
        out_path = str(path) + self.MODEL_SUFFIX
        with open(out_path, 'wb') as f:
            pickle.dump(self.model, f)
        return out_path

    def load(self, path):
        """Loads a model with the given path. Returns this model."""
        file = open(path, 'rb')
        self.model = pickle.load(file)
