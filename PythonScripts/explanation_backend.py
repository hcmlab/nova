import tensorflow as tf
from keras.models import load_model
from keras.preprocessing import image as keras_image
from keras.applications import imagenet_utils
from PIL import Image
import numpy as np
import flask
from flask import request
import io

from PIL import Image as pilimage
import numpy as np

import lime
from lime import lime_image
import lime.lime_tabular
from skimage.segmentation import mark_boundaries

import innvestigate
import innvestigate.utils as iutils
import innvestigate.utils.visualizations as ivis

import matplotlib.pyplot as plot

import io as inputoutput

import base64

import json
import pickle
import ast


# initialize our Flask application and the Keras model
app = flask.Flask(__name__)
model = None
graph = tf.get_default_graph()

# def loadmodel(model_path):
#     # load the pre-trained Keras model (here we are using a model
#     # pre-trained on ImageNet and provided by Keras, but you can
#     # substitute in your own networks just as easily)
#     global model
#     if model is None:
#         model = load_model(model_path)
#     global graph
#     graph = 

def prepare_image(image, target):
    # if the image mode is not RGB, convert it
    if image.mode != "RGB":
        image = image.convert("RGB")
    # resize the input image and preprocess it
    image = image.resize(target)
    image = keras_image.img_to_array(image)
    image = np.expand_dims(image, axis=0)

    # return the processed image
    return image

def getTopXpredictions(prediction, topLabels):

    prediction_class = []

    for i in range(0, len(prediction[0])):
        prediction_class.append((i, prediction[0][i]))

    prediction_class.sort(key=lambda x: x[1], reverse=True)

    return prediction_class[:topLabels]

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

def heatmap_rainbow(X):
    X =  np.abs(X)
    return ivis.heatmap(X, cmap_type="rainbow", input_is_postive_only=True)

def heatmap_inferno(X):
    X =  np.abs(X)
    return ivis.heatmap(X, cmap_type="inferno", input_is_postive_only=True)

def heatmap_viridis(X):
    X =  np.abs(X)
    return ivis.heatmap(X, cmap_type="viridis", input_is_postive_only=True)

def heatmap_gist_heat(X):
    X =  np.abs(X)
    return ivis.heatmap(X, cmap_type="gist_heat", input_is_postive_only=True)

def graymap(X):
    return ivis.graymap(np.abs(X), input_is_postive_only=True)

@app.route("/model", methods=["POST"])
def set_model_path():

    data = {"success":"failed"}
    if flask.request.method == "POST":
        print("Post")
        test = flask.request
        if flask.request.form.get("model_path"):
            print("loadmodel")
            model_path = flask.request.form.get("model_path")
            load_model(model_path)
            print("loaded")
            data = {"success": "success"}

    return flask.jsonify(data)

@app.route("/predict", methods=["POST"])
def predict():
    # initialize the data dictionary that will be returned from the
    # view
    global graph
    data = {"success": "failed"}

    # ensure an image was properly uploaded to our endpoint
    if flask.request.method == "POST":
        if flask.request.form.get("image"):
            # read the image in PIL format
            image = flask.request.form.get("image")
            image = Image.open(io.BytesIO(image))

            # preprocess the image and prepare it for classification
            image = prepare_image(image, target=(224, 224))

            # classify the input image and then initialize the list
            # of predictions to return to the client
            with graph.as_default():
                model_path = flask.request.form.get("model_path")
                model = load_model(model_path)

                preds = model.predict(image)
                results = imagenet_utils.decode_predictions(preds)
                data["predictions"] = []

                # loop over the results and add them to the list of
                # returned predictions
                for (imagenetID, label, prob) in results[0]:
                    r = {"label": label, "probability": float(prob)}
                    data["predictions"].append(r)

                # indicate that the request was a success
                data["success"] = True

    # return the data dictionary as a JSON response
    return flask.jsonify(data)

@app.route("/innvestigate", methods=["POST"])
def explain_innvestigate():

    global graph
    data = {"success": "failed"}
    # ensure an image was properly uploaded to our endpoint
    if flask.request.method == "POST":
        if flask.request.form.get("image"):

            postprocess = request.args.get("postprocess")
            explainer = request.args.get("explainer")
            lrpalpha = float(request.args.get("lrpalpha"))
            lrpbeta = float(request.args.get("lrpbeta"))

            with graph.as_default():
                model_path = flask.request.form.get("model_path")
                model = load_model(model_path)

                # read the image in PIL format
                image64 = flask.request.form.get("image")
                image = base64.b64decode(image64)
                image = Image.open(io.BytesIO(image))

                # preprocess the image and prepare it for classification
                image = prepare_image(image, target=(224, 224))
                image = image*(1./255)

                #print(model.summary())
                model_wo_sm = iutils.keras.graph.model_wo_softmax(model)
      
                prediction = model.predict(image)
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
                    analyzer = innvestigate.analyzer.LRPAlphaBeta(model_wo_sm, alpha=lrpalpha, beta=lrpbeta)
                elif explainer == "DEEPTAYLOR":
                    analyzer = innvestigate.analyzer.DeepTaylor(model_wo_sm)

                # Applying the analyzer
                analysis = analyzer.analyze(image)

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
                    imgFinal = heatmap_rainbow(analysis)[0]
                elif postprocess == "INFERNO":
                    imgFinal = heatmap_inferno(analysis)[0]
                elif postprocess == "GIST_HEAT":
                    imgFinal = heatmap_gist_heat(analysis)[0]
                elif postprocess == "VIRIDIS":
                    imgFinal = heatmap_viridis(analysis)[0]

                imgFinal = np.uint8(imgFinal*255)

                img = pilimage.fromarray(imgFinal)
                imgByteArr = inputoutput.BytesIO()
                img.save(imgByteArr, format='JPEG')
                imgByteArr = imgByteArr.getvalue()

                img64 = base64.b64encode(imgByteArr)
                img64_string = img64.decode("utf-8")
                data["explanation"] = img64_string
                data["prediction"] = str(topClass[0][0])
                data["prediction_score"] = str(topClass[0][1])
                data["success"] = "success"
                
    return flask.Response(json.dumps(data), mimetype="text/plain")


@app.route("/lime", methods=["POST"])
def explain_lime():

    global graph
    data = {"success": "failed"}

    if flask.request.method == "POST":
        if flask.request.form.get("image"):
            
            top_labels = int(request.args.get("toplabels"))
            hide_color = str(request.args.get("hidecolor"))
            num_samples =  int(request.args.get("numsamples"))
            positive_only = str(request.args.get("positiveonly"))
            num_features = int(request.args.get("numfeatures"))
            hide_rest = str(request.args.get("hiderest"))

            # read the image in PIL format
            image64 = flask.request.form.get("image")
            image = base64.b64decode(image64)
            image = Image.open(io.BytesIO(image))

            with graph.as_default():
                
                model_path = flask.request.form.get("model_path")
                model = load_model(model_path)

                img = prepare_image(image, (224, 224))
                img = img*(1./255)
                prediction = model.predict(img)
                explainer = lime_image.LimeImageExplainer()
                img = np.squeeze(img)
                explanation = explainer.explain_instance(img, model.predict, top_labels=top_labels, hide_color=hide_color=="True", num_samples=num_samples)

                top_classes = getTopXpredictions(prediction, top_labels)

                explanations =  []

                for cl in top_classes:

                    temp, mask = explanation.get_image_and_mask(cl[0], positive_only=positive_only=="True", num_features=num_features, hide_rest=hide_rest=="True")
                    img_explained = mark_boundaries(temp, mask)
                    img = Image.fromarray(np.uint8(img_explained*255))
                    img_byteArr = io.BytesIO()
                    img.save(img_byteArr, format='JPEG')
                    img_byteArr = img_byteArr.getvalue()
                    img64 = base64.b64encode(img_byteArr)
                    img64_string = img64.decode("utf-8")

                    explanations.append((str(cl[0]), str(cl[1]), img64_string))

                data["explanations"] = explanations
                data["success"] = "success"

    return flask.Response(json.dumps(data), mimetype="text/plain")

@app.route("/tabular", methods=["POST"])
def explain_tabular():

    data = {"success": "failed"}

    #TODO send sample to be explained

    if flask.request.method == "POST":

        if flask.request.form:

            #data_dict = ast.literal_eval(json.loads(flask.request.data))

            print("try open model")
            with open(flask.request.form.get("model_path"), 'rb') as f:
                model = pickle.load(f)

            train_data = json.loads(flask.request.form.get("data"))
            dim = json.loads(flask.request.form.get("dim"))
            train_data = np.asarray(train_data)
            train_data = train_data.reshape(((int)(train_data.size/dim), dim))
            sample = json.loads(flask.request.form.get("sample"))
            
            num_features = int(request.args.get("numfeatures"))

            explainer = lime.lime_tabular.LimeTabularExplainer(train_data, mode="classification", discretize_continuous=True)
            exp = explainer.explain_instance(np.asarray(sample), model.predict_proba, num_features=num_features, top_labels=1)

            explanation_dictionary = {}

            for entry in exp.as_list():
                explanation_dictionary.update({entry[0]: entry[1]})

            data["explanation"] = explanation_dictionary
            data["success"] = "success"

    return flask.Response(json.dumps(data), mimetype="text/plain")

if __name__ == "__main__":
    print(("* Loading Keras model and Flask starting server..."
        "please wait until server has fully started"))
    app.run(debug=True)