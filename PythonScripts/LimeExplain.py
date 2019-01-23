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
    imgR = img.resize((224, 224))
    x = image.img_to_array(imgR)
    x = np.expand_dims(x, axis=0)
    #x = preprocess_input(x)
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

def getType():
    return ScriptType.EXPLAINER.name

def rescale(img, width, height):
    img = resize(img, (width, height))

def test():
    modelPath = "C:/Users/Alex Heimerl/Desktop/test/vgg16_pokemon_test__2_mse_sgd_10-0.23.h5"
    img_path = "C:/Users/Alex Heimerl/Desktop/test/Capture1.jpg"
    img = Image.open(img_path)

    imgByteArr = io.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()

    newImg, oldImg = transform_img_fn(imgByteArr)

    img = newImg

    model = load_model(modelPath)

    img = img*(1./255)

    prediction = model.predict(img)

    explainer = lime_image.LimeImageExplainer()
    
    img = np.squeeze(img)
#    img = (img + 1)*0.5
#    img = np.asarray(img, np.float32)
    explanation = explainer.explain_instance(img, model.predict, top_labels=2, hide_color=0, num_samples=1000)

    temp, mask = explanation.get_image_and_mask(getTopPrediction(prediction[0]), positive_only=True, num_features=100, hide_rest=False)
    imgExplained = mark_boundaries(temp, mask)

    tempMask = mask * 255
    newMask = resize(tempMask, (oldImg.height, oldImg.width))
    newMask = newMask.astype(np.int64)
    oldImgArr = image.img_to_array(oldImg)
    oldImgArr = oldImgArr * (1./255)
    oldImgArr = oldImgArr.astype(np.float64)
    imgOrgExplained = mark_boundaries(oldImgArr, newMask)
    
    testImg = Image.fromarray(np.uint8(tempMask))
    testImg = testImg.resize((oldImg.width, oldImg.height))

    testImg = image.img_to_array(testImg)
    testImg = testImg * 1./255
    testImg = testImg.astype(np.int64)
    testImg = np.squeeze(testImg)

    testImgExpl = mark_boundaries(oldImgArr, testImg)

    # test = np.array(img)
    # test = test*255
    # img = test

    #img = imresize(img,(224,224))

    # plt.imshow(img)
    # plt.show()

    # # return img

    # suc, imgenc = cv2.imencode('.jpg', img)

    # plt.imshow(imgenc)
    # plt.show()

    # return imgenc.tobytes()
    imgExplained = testImgExpl
    test = np.uint8(imgExplained*255)
    img = Image.fromarray(test)
    img.save(r'C:/Users/Alex Heimerl/Desktop/test/testpython.jpg')

    f, axarr = plt.subplots(1,3)
    axarr[0].imshow(testImg)
    axarr[1].imshow(testImgExpl)
    axarr[2].imshow(img)
    plt.show()

    imgByteArr = io.BytesIO()
    img.save(imgByteArr, format='JPEG')
    imgByteArr = imgByteArr.getvalue()

    return imgByteArr

def transform_img_fn_inception(path_list):
    out = []
    for img_path in path_list:
        img = image.load_img(img_path, target_size=(224, 224))
        x = image.img_to_array(img)
        x = np.expand_dims(x, axis=0)
        #x = preprocess_input(x)
        out.append(x)
    return np.vstack(out)

def inception():

    images = transform_img_fn_inception([r'C:/Users/Alex Heimerl/Desktop/nova/Scripts/Capture1.jpg'])
   
    inet_model = VGG16(weights='imagenet', include_top=False)
    x = inet_model.output
    x = GlobalAveragePooling2D()(x)
    # let's add a fully-connected layer
    x = Dense(1024, activation='relu')(x)
    # and a logistic layer -- let's say we have 200 classes
    predictions = Dense(2, activation='softmax')(x)

    # this is the model we will train
    model = Model(inputs=inet_model.input, outputs=predictions)
    model.compile(optimizer='rmsprop', loss='categorical_crossentropy')
    preds = model.predict(images)
    print(preds)

    explainer = lime_image.LimeImageExplainer()
   
    # Hide color is the color for a superpixel turned OFF. Alternatively, if it is NONE, the superpixel will be replaced by the average of its pixels
    explanation = explainer.explain_instance(images[0]*1./255, model.predict, top_labels=5, hide_color=0, num_samples=100)
    temp, mask = explanation.get_image_and_mask(1, positive_only=True, num_features=10, hide_rest=True)
    img_exp = mark_boundaries(temp, mask)
    plt.imshow(img_exp)
    plt.show()
    # img = Image.fromarray(np.uint8(img_exp*255))
    # img.save(r"C:\Users\Alex Heimerl\Desktop\nova\bin\Debug\inception.jpg")
    # imgByteArr = io.BytesIO()
    # img.save(imgByteArr, format='JPEG's)
    # imgByteArr = imgByteArr.getvalue()
    # return imgByteArr

if __name__ == '__main__':
    test()
    test()
    #inception()