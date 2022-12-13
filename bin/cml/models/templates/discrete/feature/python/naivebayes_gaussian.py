import os
import sys
import numpy as np
from pathlib import Path

if not hasattr(sys, 'argv'):
    sys.argv  = ['']


from sklearn.naive_bayes import GaussianNB
import pickle
DEPENDENCIES = []
OPTIONS = {}
MODEL_SUFFIX = '.model'


def train (data, logger=None):
    X,Y = data
    model = GaussianNB()
    print('train_x shape: {} | train_x[0] shape: {}'.format(X.shape, X[0].shape))
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
    Y = model.predict(data[0])
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
        x_tmp = [v[r + '.' + request_form["streamName"].split(" ")[0]] for v in data_list]
        y_tmp = [v[r + '.' + request_form["scheme"].split(";")[0]] for v in data_list]
        x.extend(x_tmp)
        y.extend(y_tmp)

    return np.ma.concatenate(x, axis=0), np.array(y)