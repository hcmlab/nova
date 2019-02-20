import sys
class CatchOutErr:
    def __init__(self):
        self.value = ''
    def write(self, txt):
       self.value += txt

catchOutErr = CatchOutErr()
sys.stderr = catchOutErr

from keras.preprocessing import image as kerasimg
from keras.models import load_model
import os
import numpy as np
import io as inputoutput

from PIL import Image as pilimage
import numpy as np

import innvestigate
import innvestigate.utils as iutils
import innvestigate.utils.visualizations as ivis

import matplotlib.pyplot as plot


def transform_img_fn(img):
    img = pilimage.open(inputoutput.BytesIO(bytes(img)))
    imgR = img.resize((224, 224))
    x = kerasimg.img_to_array(imgR)
    x = np.expand_dims(x, axis=0)
    return (x, img)

def loadModel(modelPath):
    try:
        model = load_model(modelPath)
    except Exception as ex:
        return None
    return model

def getTopXpredictions(prediction, topLabels):

    prediction_class = []

    for i in range(0, len(prediction[0])):
        prediction_class.append((i, prediction[0][i]))

    prediction_class.sort(key=lambda x: x[1], reverse=True)

    return prediction_class[:topLabels]

def explain(model, img, postprocess, explainer):
    
    explanation = []

    img, oldImg = transform_img_fn(img)
    img = img*(1./255)

    prediction = model.predict(img)
    topClass = getTopXpredictions(prediction, 1)

    model_wo_sm = iutils.keras.graph.model_wo_softmax(model)

    analyzer = []

    if explainer == "GUIDEDBACKPROP":
        analyzer = innvestigate.analyzer.GuidedBackprop(model_wo_sm)
    elif explainer == "GRADIENT":
        analyzer = innvestigate.analyzer.Gradient(model_wo_sm)
    elif explainer == "DECONVNET":
        analyzer = innvestigate.analyzer.Deconvnet(model_wo_sm)
    elif explainer == "LRPEPSILON":
        analyzer = innvestigate.analyzer.LRPEpsilon(model_wo_sm)
    elif explainer == "LRPZ":
        analyzer = innvestigate.analyzer.LRPZ(model_wo_sm)
    elif explainer == "LRPALPHABETA":
        analyzer = innvestigate.analyzer.LRPAlphaBeta(model_wo_sm, beta=1)
    elif explainer == "PATTERNNET":
        analyzer = innvestigate.analyzer.PatternNet(model_wo_sm, pattern_type="relu")

    # Applying the analyzer
    analysis = analyzer.analyze(img)

    imgFinal = []

    if postprocess == "GRAYMAP":
        imgFinal = graymap(analysis)[0]
    elif postprocess =="HEATMAP":
        imgFinal = heatmap(analysis)[0]
    elif postprocess == "BK_PROJ":
        imgFinal = bk_proj(analysis)[0]
    elif postprocess == "GNUPLOT2":
        imgFinal = heatmapgnuplot2(analysis)[0]
    elif postprocess == "CMRMAP":
        imgFinal = heatmapCMRmap(analysis)[0]
    elif postprocess == "NIPY_SPECTRAL":
        imgFinal = heatmapnipy_spectral(analysis)[0]
    elif postprocess == "RAINBOW":
        imgFinal = heatmapnipy_rainbow(analysis)[0]

    imgFinal = np.uint8(imgFinal*255)

    img = pilimage.fromarray(imgFinal)
    imgByteArr = inputoutput.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()

    explanation = (topClass[0][0], topClass[0][1], imgByteArr)
    
    return explanation

def test():
    modelPath = "C:/Users/Alex Heimerl/Desktop/test/pokemon.trainer.PythonModel.model.keras_vgg_face.h5"
    img_path = "C:/Users/Alex Heimerl/Desktop/test/pikachu.jpeg"
    img = pilimage.open(img_path)

    imgByteArr = inputoutput.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()

    img, oldImg = transform_img_fn(imgByteArr)
    img = img*(1./255)

    model = load_model(modelPath)

    model_wo_sm = iutils.keras.graph.model_wo_softmax(model)

    # Creating an analyzer
    gradient_analyzer = innvestigate.analyzer.GuidedBackprop(model_wo_sm)

    # Applying the analyzer
    analysis = gradient_analyzer.analyze(img)
    
    analysis = innvestigate.analyzer.DeepTaylor(model_wo_sm).analyze(img)

    testfilter = heatmapnipy_rainbow(analysis)[0]
    plot.imshow(testfilter)
    plot.show()

    imgFinal = graymap(analysis)[0]
    imgFinal = np.uint8(imgFinal*255)

    img = pilimage.fromarray(imgFinal)
    imgByteArr = inputoutput.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()

    plot.imshow(graymap(analysis)[0])
    plot.show()

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

def heatmapgnuplot2(X):
    X =  np.abs(X)
    return ivis.heatmap(X, cmap_type="gnuplot2", input_is_postive_only=True)

def heatmapCMRmap(X):
    X =  np.abs(X)
    return ivis.heatmap(X, cmap_type="CMRmap", input_is_postive_only=True)

def heatmapnipy_spectral(X):
    X =  np.abs(X)
    return ivis.heatmap(X, cmap_type="nipy_spectral", input_is_postive_only=True)

def heatmapnipy_rainbow(X):
    X =  np.abs(X)
    return ivis.heatmap(X, cmap_type="rainbow", input_is_postive_only=True)


def graymap(X):
    return ivis.graymap(np.abs(X), input_is_postive_only=True)



if __name__ == '__main__':
    test()
