import sys
class CatchOutErr:
    def __init__(self):
        self.value = ''
    def write(self, txt):
       self.value += txt

catchOutErr = CatchOutErr()
sys.stderr = catchOutErr

from keras.preprocessing import image
from keras.models import load_model
import os
import numpy as np
import io

from PIL import Image
import numpy as np

import innvestigate
import innvestigate.utils as iutils
import innvestigate.utils.visualizations as ivis

def transform_img_fn(img):
    img = Image.open(io.BytesIO(bytes(img)))
    imgR = img.resize((224, 224))
    x = image.img_to_array(imgR)
    x = np.expand_dims(x, axis=0)
    return (x, img)

def getTopPrediction(prediction):
    maxP = 0
    for i in range(1, len(prediction)):
        if prediction[maxP] < prediction[i]:
            maxP = i
    return maxP

def getTopXpredictions(prediction, topLabels):

    prediction_class = []

    for i in range(0, len(prediction[0])):
        prediction_class.append((i, prediction[0][i]))

    prediction_class.sort(key=lambda x: x[1], reverse=True)

    return prediction_class[:topLabels]

def loadModel(modelPath):
    try:
        model = load_model(modelPath)
    except Exception as ex:
        return None
    return model

def explain(model, img, explainer, representation):
            
    img, oldImg = transform_img_fn(img)
    img = img*(1./255)
    
    model_wo_sm = iutils.keras.graph.model_wo_softmax(model)

    # Creating an analyzer
    gradient_analyzer = innvestigate.analyzer.Gradient(model_wo_sm)

    # Applying the analyzer
    analysis = gradient_analyzer.analyze(img)

    imgFinal = graymap(analysis)[0]

    img = Image.fromarray(imgFinal)
    imgByteArr = io.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()


    return imgByteArr


def preprocess(X, net):
    X = X.copy()
    X = net["preprocess_f"](X)
    return X


def postprocess(X, color_conversion, channels_first):
    X = X.copy()
    X = iutils.postprocess_images(
        X, color_coding=color_conversion, channels_first=channels_first)
    return X


def image(X):
    X = X.copy()
    return ivis.project(X, absmax=255.0, input_is_postive_only=True)


def bk_proj(X):
    X = ivis.clip_quantile(X, 1)
    return ivis.project(X)


def heatmap(X):
    X = ivis.gamma(X, minamp=0, gamma=0.95)
    return ivis.heatmap(X)


def graymap(X):
    return ivis.graymap(np.abs(X), input_is_postive_only=True)