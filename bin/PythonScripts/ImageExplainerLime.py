import sys
import multiprocessing


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
import lime
from lime import lime_image
from skimage.segmentation import mark_boundaries
from PIL import Image

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

def explain(model, img, topLabels, numSamples, numFeatures, hideRest, hideColor, positiveOnly):
            
    img, oldImg = transform_img_fn(img)
    img = img*(1./255)
    prediction = model.predict(img)
    explainer = lime_image.LimeImageExplainer()
    img = np.squeeze(img)
    explanation = explainer.explain_instance(img, model.predict, top_labels=topLabels, hide_color=hideColor, num_samples=numSamples)
    temp, mask = explanation.get_image_and_mask(getTopPrediction(prediction[0]), positive_only=positiveOnly, num_features=numFeatures, hide_rest=hideRest)
    tempMask = mask * 255
    temp = Image.fromarray(np.uint8(tempMask))
    temp = temp.resize((oldImg.width, oldImg.height))
    temp = image.img_to_array(temp)
    temp = temp * 1./255
    temp = temp.astype(np.int64)
    temp = np.squeeze(temp)
    oldImgArr = image.img_to_array(oldImg)
    oldImgArr = oldImgArr * (1./255)
    oldImgArr = oldImgArr.astype(np.float64)
    imgExplained = mark_boundaries(oldImgArr, temp)
    imgFinal = np.uint8(imgExplained*255)
    img = Image.fromarray(imgFinal)
    imgByteArr = io.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()


    return imgByteArr

def explain_raw(model, img, topLabels, numSamples, numFeatures, hideRest, hideColor, positiveOnly):
    img, oldImg = transform_img_fn(img)
    img = img*(1./255)
    prediction = model.predict(img)
    explainer = lime_image.LimeImageExplainer()
    img = np.squeeze(img)
    explanation = explainer.explain_instance(img, model.predict, top_labels=topLabels, hide_color=hideColor, num_samples=numSamples)
    temp, mask = explanation.get_image_and_mask(getTopPrediction(prediction[0]), positive_only=positiveOnly, num_features=numFeatures, hide_rest=hideRest)
    imgExplained = mark_boundaries(temp, mask)
    img = Image.fromarray(np.uint8(imgExplained*255))
    imgByteArr = io.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()

    return imgByteArr

def explain_multiple(model, img, topLabels, numSamples, numFeatures, hideRest, hideColor, positiveOnly):
    img, oldImg = transform_img_fn(img)
    img = img*(1./255)
    prediction = model.predict(img)
    explainer = lime_image.LimeImageExplainer()
    img = np.squeeze(img)
    explanation = explainer.explain_instance(img, model.predict, top_labels=topLabels, hide_color=hideColor, num_samples=numSamples)

    topClasses = getTopXpredictions(prediction, topLabels)

    explanations =  []

    for cl in topClasses:

        temp, mask = explanation.get_image_and_mask(cl[0], positive_only=positiveOnly, num_features=numFeatures, hide_rest=hideRest)
        imgExplained = mark_boundaries(temp, mask)
        img = Image.fromarray(np.uint8(imgExplained*255))
        imgByteArr = io.BytesIO()
        img.save(imgByteArr, format='JPEG')
        imgByteArr = imgByteArr.getvalue()

        explanations.append((cl[0], cl[1], imgByteArr))

    return (explanations, len(explanations))

def test_explain(model, img, topLabels, numSamples, numFeatures, hideRest, hideColor, positiveOnly):
    result = []
    p = multiprocessing.Process(target=explain_multiple, args=(model, img, topLabels, numSamples, numFeatures, hideRest, hideColor, positiveOnly))
    #result.append(p)
    p.start()
    p.join()
    print(result)
    return (None, None)
