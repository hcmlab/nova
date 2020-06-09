import sys
import importlib
if not hasattr(sys, 'argv'):
	sys.argv  = ['']
import tensorflow as tf
import numpy as np
import random
import datetime
from xml.dom import minidom
import os
import shutil
import site
from tensorflow.python.keras.models import load_model
from tensorflow.python.keras.callbacks import ModelCheckpoint, EarlyStopping
from tensorflow.python.keras.optimizers import Adam
from tensorflow.python.keras import backend as K

def correlation_coefficient_loss(y_true, y_pred):
    x = y_true
    y = y_pred
    mx = K.mean(x)
    my = K.mean(y)
    xm, ym = x-mx, y-my
    r_num = K.sum(tf.multiply(xm,ym))
    r_den = K.sqrt(tf.multiply(K.sum(K.square(xm)), K.sum(K.square(ym))))
    r = r_num / r_den

    r = K.maximum(K.minimum(r, 1.0), -1.0)
    return 1 - K.square(r)


def getOptions(opts, vars):
    vars['x'] = None
    vars['y'] = None
    vars['sess'] = None
    vars['model'] = None

    opts['network'] = ''
    opts['p_drop'] = 0.5
    opts['batch_size'] = 100
    opts['n_epoch'] = 200
    opts['n_log'] = 10
    opts['n_log_full'] = 100    

def convert_to_one_hot(label, n_classes):

    float_label = int(label)
    one_hot = np.zeros(n_classes)
    one_hot[label] = 1.0

    return one_hot


def train(data, label, score, opts, vars):
    n_input = data[0].dim
    n_output = 1

    print ('load network architecture from ' + opts['network'] + '.py')
    print('#input: {} \n#output: {}'.format(n_input, n_output))
    module = __import__(opts['network'])

    model = module.getModel(n_input, n_output)

    train_x = np.empty((len(data), n_input))
    train_y = np.empty((len(data), n_output))
  
    print('Data length {}'.format(len(data)))

    for sample in range(len(data)):
        train_x[sample] = np.reshape(data[sample],(n_input))
        train_y[sample] = score[sample] 

    train_x = train_x.astype('float32')  


    model.compile(loss=correlation_coefficient_loss, optimizer=Adam(lr=0.00001))

    #checkpoint
    #filepath="weights-improvement-{epoch:02d}-{val_acc:.2f}.hdf5"
    #checkpoint = ModelCheckpoint(filepath, monitor='val_acc', verbose=1, save_best_only=False, mode='max')
    
    early_stoppping = EarlyStopping(monitor='loss', min_delta=0.0001, patience=3)
 
    callbacks_list = [early_stoppping]

    model.fit(train_x,
          train_y,
          epochs=opts['n_epoch'],
          batch_size=opts['batch_size'],
          shuffle=True,
          callbacks=callbacks_list)        

    vars['n_input'] = n_input
    vars['n_output'] = n_output
    vars['model'] = model

def forward(data, probs, opts, vars):
        n_input = data.dim

        np_array_x = np.asarray(data)
        np_array_x = np_array_x.astype('float32')
        np_array_x = np.reshape(np_array_x, (1, n_input))

        model = vars['model']
        pred = model.predict(np_array_x, batch_size=opts['batch_size'], verbose=0)

        for i in range(len(pred[0])):
            probs[i] = float(pred[0][i])

def save(path, opts, vars):

    # save model
    model_path = path + '_keras.h5'
    print('save model to ' + model_path)
    model = vars['model']
    model.save(model_path)

    # copy scripts
    srcDir = os.path.dirname(os.path.realpath(__file__)) + '\\'
    dstDir = os.path.dirname(path) + '\\'

    print('copy scripts from \'' + srcDir + '\' to \'' + dstDir + '\'')

    if not os.path.exists(dstDir):
        os.makedirs(dstDir)

    srcFiles = os.listdir(srcDir)
    for fileName in srcFiles:
        fullFileName = os.path.join(srcDir, fileName)
        if os.path.isfile(fullFileName) and fileName.endswith('.py'):
            shutil.copy(fullFileName, dstDir)
    

    network_new =  os.path.basename(path) + '.' + opts['network']

    
    ############# FAILS WHEN EXECUTED A SECOND TIME #########################
    try:
        shutil.move(dstDir + opts['network'] + '.py', dstDir + network_new + '.py')
    except Exception as e:
        print('Exception: {}'.format(e))


def load(path, opts, vars):
    print('load model from ' + path)
 
    # parse input/output dimensions
    trainerPath = path
    trainerPath = trainerPath.replace("trainer.PythonModel.model", "trainer")

    print('tp: {}'.format(trainerPath))

    xmldoc = minidom.parse(trainerPath)
    streams = xmldoc.getElementsByTagName('streams')
    streamItem = streams[0].getElementsByTagName('item')
    n_input = streamItem[0].attributes['dim'].value
    print("#input: " + str(n_input))

    classes = xmldoc.getElementsByTagName('classes')
    n_output = len(classes[0].getElementsByTagName('item'))
    print("#output: " + str(n_output))

    # copy unique network file

    network_tmp = os.path.dirname(path) + '\\' + opts['network'] + '.py'
    shutil.copy(path + '.' + opts['network'] + '.py', network_tmp)

    # reload sys path and import network file
    importlib.reload(site)

    print('Modelpath {}'.format(path + '_keras.h5'))

    # load model
    vars['model'] = load_model(path + '_keras.h5', custom_objects={'correlation_coefficient_loss': correlation_coefficient_loss})

