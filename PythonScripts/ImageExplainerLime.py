import sys
import importlib
import site

if not hasattr(sys, 'argv'):
	sys.argv  = ['']
from keras.preprocessing import image
from keras.models import load_model
import os
import numpy as np
import io
import lime
from lime import lime_image
from skimage.segmentation import mark_boundaries
from PIL import Image
from keras import backend as K
import tensorflow as tf



dir_path = os.path.dirname(os.path.realpath(__file__))
print(dir_path)
sys.path.append(dir_path)
from EnumScriptType import ScriptType


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


def loadModel(modelPath):
    try:
        model = load_model(modelPath)
    except Exception as e:
        with open(r'exception.txt', "w") as f:
            f.write(str(e))    
    return model

def explain(model, img, topLabels, numSamples, numFeatures, hideRest, hideColor, positiveOnly):
            
    try:
        # server = tf.train.Server.create_local_server()
        # sess = tf.Session(server.target) 
        # graph = tf.get_default_graph()
        # K.set_session(sess)
        img, oldImg = transform_img_fn(img)
        # model = load_model(modelPath)
        img = img*(1./255)
        # with graph.as_default():
        #     with sess.as_default():
                # model.predict(np.zeros((1,224,224,3), dtype=np.float32))
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
    except Exception as e:
        with open(r'exception.txt', "w") as f:
            f.write(str(e))        

    return imgByteArr

def explain_deprecated(model, img, topLabels, numSamples, numFeatures, hideRest, hideColor, positiveOnly):
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

def getType():
    return ScriptType.EXPLAINER.name

def test():
    return "test"
