
from keras.preprocessing import image
from keras.models import load_model
import os
import numpy as np
import sys
import io
import lime
from lime import lime_image
from skimage.segmentation import mark_boundaries
from PIL import Image

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

def explain(modelPath, img):
    modelPath = "C:/Users/Alex Heimerl/Desktop/test/vgg16_pokemon_100.h5"
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

def explainToOrg(modelPath, img):
    # modelPath = "C:/Users/Alex Heimerl/Desktop/test/vgg16_pokemon_test__2_mse_sgd_10-0.23.h5"
    # img_path = "C:/Users/Alex Heimerl/Desktop/nova/Scripts/Capture1.jpg"
    # img = Image.open(img_path)
    # imgByteArr = io.BytesIO()
    # img.save(imgByteArr, format='JPEG')
    # imgByteArr = imgByteArr.getvalue()
    img, oldImg = transform_img_fn(img)
    model = load_model(modelPath)
    img = img*(1./255)
    prediction = model.predict(img)
    explainer = lime_image.LimeImageExplainer()
    img = np.squeeze(img)
    explanation = explainer.explain_instance(img, model.predict, top_labels=2, hide_color=0, num_samples=1000)
    temp, mask = explanation.get_image_and_mask(getTopPrediction(prediction[0]), positive_only=True, num_features=100, hide_rest=False)
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
    #img.save("C:/Users/Alex Heimerl/Desktop/nova/Scripts/testpython.jpg")
    imgByteArr = io.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()

    return imgByteArr

def getType():
    return ScriptType.EXPLAINER.name
