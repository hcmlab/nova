from keras.applications.vgg16 import VGG16
from keras.applications.vgg16 import preprocess_input
from keras.preprocessing import image
from keras.models import load_model
from keras.preprocessing.image import ImageDataGenerator
import os
import numpy as np
import sys
import io
import cv2


import lime
from lime import lime_image
from skimage.segmentation import mark_boundaries
import matplotlib.pyplot as plt
from PIL import Image

from skimage.transform import resize

dir_path = os.path.dirname(os.path.realpath(__file__))
print(dir_path)
sys.path.append(dir_path)
from EnumScriptType import ScriptType

from keras.applications import inception_v3 as inc_net
from keras.applications.imagenet_utils import decode_predictions
from keras.models import Model
from keras.layers import Dense, GlobalAveragePooling2D


def transform_img_fn(img):
    img = Image.open(io.BytesIO(bytes(img)))
    img = img.resize((224, 224))
    x = image.img_to_array(img)
    x = np.expand_dims(x, axis=0)
    #x = preprocess_input(x)
    return x

def getTopPrediction(prediction):
    maxP = 0
    for i in range(1, len(prediction)):
        if prediction[maxP] < prediction[i]:
            maxP = i
    return maxP

def explain(modelPath, img):

    modelPath = "C:/Users/Alex Heimerl/Desktop/test/vgg16_pokemon_100.h5"

    img = transform_img_fn(img)

    model = load_model(modelPath)

    img = img/255

    prediction = model.predict(img)

    explainer = lime_image.LimeImageExplainer()

    explanation = explainer.explain_instance(np.squeeze(img), model.predict, top_labels=2, hide_color=0, num_samples=1000)
    temp, mask = explanation.get_image_and_mask(getTopPrediction(prediction[0]), positive_only=True, num_features=50, hide_rest=True)
    imgExplained = mark_boundaries(temp, mask)

    img = Image.fromarray(np.uint8(imgExplained*255))
    
    imgByteArr = io.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()

    return imgByteArr

def getType():
    return ScriptType.EXPLAINER.name
