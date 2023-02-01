import os
import sys
import numpy as np
from pathlib import Path

if not hasattr(sys, 'argv'):
    sys.argv  = ['']


from sklearn.svm import LinearSVC
from sklearn.calibration import CalibratedClassifierCV
import pickle

DEPENDENCIES = []
OPTIONS = {'C': '0.1','number_folds':'3', 'dual':'True', 'class_weight':'None', 'method':'sigmoid'}
MODEL_SUFFIX = '.model'

def train (data, logger=None):
    X,Y = data

    #Manage Options
    if OPTIONS['class_weight'] == 'None':
        OPTIONS['class_weight'] = None

    linear_svc = LinearSVC(C=float(OPTIONS['C']), dual=bool(OPTIONS['dual']), class_weight=(OPTIONS['class_weight']))
    model = CalibratedClassifierCV(linear_svc, method=OPTIONS['method'], cv=int(OPTIONS['number_folds']))
    print('train_x shape: {} | train_x[0] shape: {}'.format(X.shape, X[0].shape))
    print('train_y shape: {} | train_y[0] shape: {}'.format(Y.shape, Y[0].shape))
    model.fit(X, Y)
    return model

def save (model, path, logger=None):
    if not Path(path.parent).is_dir():
        path.parent.mkdir(parents=True, exist_ok=True)
        # out_path = str(path) + ".pth"
    out_path = str(path) + MODEL_SUFFIX
    with open(out_path, 'wb') as f:
        pickle.dump(model, f)
        #print('Model stored at: {}'.format(path))
    return out_path


def predict (model, data, logger=None):
    X,Y = data
    Y = model.predict_proba(X) 
    return Y


def load (path, classes, logger=None):
    file = open(path,'rb')
    model = pickle.load(file)
    #model = pickle.load(path, "rb" )
    return model

def preprocess(ds_iter, request_form=[], logger=None):
    data_list = list(ds_iter)
    data_list.sort(key=lambda x: int(x["frame"].split("_")[0]))

    # Get keys for all roles

    x = []
    y = []
    for r in request_form["roles"].split(';'):
        x_tmp = [val[r + '.' + request_form["streamName"].split(" ")[0]] for val in data_list]
        y_tmp = [val[r + '.' + request_form["scheme"].split(";")[0]] for val in data_list]
        x.extend(x_tmp)
        y.extend(y_tmp)

    return np.ma.concatenate(x, axis=0), np.array(y)

def postprocess(ds_iter, request_form=[], logger=None):
    pass