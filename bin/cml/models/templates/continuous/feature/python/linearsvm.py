import os
import sys
import numpy as np
from pathlib import Path

if not hasattr(sys, 'argv'):
    sys.argv = ['']

from sklearn.svm import LinearSVR
from sklearn.calibration import CalibratedClassifierCV
import pickle


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
        X, Y = self.data

        linear_SVR = LinearSVR(C=float(self.OPTIONS['C']), dual=bool(self.OPTIONS['dual']), epsilon=0.0,
                               fit_intercept=True, intercept_scaling=1.0, loss='epsilon_insensitive', max_iter=1000,
                               random_state=None, tol=0.0001, verbose=0)
        self.model = CalibratedClassifierCV(linear_SVR, method=self.OPTIONS['method'], cv=int(self.OPTIONS['number_folds']))
        self.model.fit(X, Y)

        print('train_x shape: {} | train_x[0] shape: {}'.format(X.shape, X[0].shape))
        print('train_y shape: {} | train_y[0] shape: {}'.format(Y.shape, Y[0].shape))

    def predict(self):
        """Predicts the given data with the given model. Returns a list with the predicted values."""
        X, Y = self.data
        self.predictions = self.model.predict(X)

    def postprocess(self) -> list:
        """Possible pro-processing of the data. Returns a list with the pro-processed data."""
        return self.predictions

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
