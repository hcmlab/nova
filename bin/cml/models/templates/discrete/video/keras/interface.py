from numpy.random import seed
seed(1234)
from tensorflow import set_random_seed
set_random_seed(1234)

import sys
import importlib
if not hasattr(sys, 'argv'):
	sys.argv  = ['']
import tensorflow as tf
import numpy as np
import random
from datetime import datetime
from xml.dom import minidom
import os
import shutil
import site
import pprint
import h5py
import imageio
from skimage.transform import resize
#from tensorflow.python.keras.models import load_model
#from tensorflow.python.keras.callbacks import ModelCheckpoint
#from tensorflow.python.keras import backend
import keras
from keras import backend
from keras.models import load_model
from keras.callbacks import TensorBoard
from nova_data_generator import DataGenerator
from PIL import Image

#interface
def getModelType(types, opts, vars):
    return types.REGRESSION if opts["is_regression"] else types.CLASSIFICATION

def getOptions(opts, vars):
    try:
        vars['x'] = None
        vars['y'] = None
        vars['session'] = None
        vars['model'] = None

        '''Setting the default options. All options can be overwritten by adding them to the conf-dictionary in the same file as the network'''
        opts['network'] = ''
        opts['is_regression'] = False
        opts['n_fp'] = 1
        opts['loss_function'] = 'categorical_crossentropy'
        opts['optimizier'] = 'adam'
        opts['metrics'] = ['accuracy']
        opts['lr'] = 0.0001
        opts['n_epoch'] = 10
        opts['image_width'] = 300
        opts['image_height'] = 300
        opts['batch_size_train'] = 32
        opts['batch_size_val'] = 32
        opts['data_path_train'] = ''
        opts['data_path_val'] = ''
        opts['datagen_rescale'] = 1./255
        opts['datagen_rotation_range'] = 20
        opts['datagen_width_shift_range'] = 0.2
        opts['datagen_height_shift_range'] = 0.2


    except Exception as e:
        print_exception(e, 'getOptions')
        sys.exit()


def train(data, label_score, opts, vars):
    
    if not opts['is_regression']:
        n_classes = int(max(label_score)+1)
    else:
        n_classes = 1
            
    try:

        width = opts['image_width']
        height = opts['image_height']
        n_channels = 3
        batch_size = 8

        params = {'dim': (width, height),
                'batch_size': batch_size,
                'n_classes': n_classes,
                'n_channels': n_channels,
                'shuffle': False
                }

        #make folder for callbacks
        if not os.path.exists(os.path.dirname(os.path.realpath(__file__))  + '/backups/'):
             os.makedirs(os.path.dirname(os.path.realpath(__file__))  + '/backups/')
        if not os.path.exists(os.path.dirname(os.path.realpath(__file__))  + '/logs/'):
             os.makedirs(os.path.dirname(os.path.realpath(__file__))  + '/logs/')



        # tensorboard = keras.callbacks.TensorBoard(
        #     log_dir=os.path.dirname(os.path.realpath(__file__)) + '/logs/{}'.format(opts['network'] + str(datetime.now()) ),
        #     write_graph=True,
        #     write_images=True)

        # saver = keras.callbacks.ModelCheckpoint(
        #     filepath =  os.path.dirname(os.path.realpath(__file__))  + '/backups/' + opts['network'] + '{epoch:02d}-{acc:.2f}.h5'  , 
        #     monitor='acc', 
        #     verbose=0, 
        #     save_best_only=True, 
        #     save_weights_only=False, 
        #     mode='auto', 
        #     period=1)

        # add ,saver to save after each epoch
        callbacks = [
            # tensorboard
        ]
        # Generators
        training_generator = DataGenerator(**params)

        print("Target Classes: " + str(params['n_classes']))
        n_input = width * height * n_channels
        module = __import__(opts['network'])
        print(opts['network']) 
        model = module.getModel((width,height,n_channels), params['n_classes'])
        print("getmodel")
        model.compile(optimizer=opts['optimizier'], loss=opts['loss_function'], metrics=opts['metrics'])
        print("compile")
        model.fit_generator(generator=training_generator,
                            shuffle=False,
                            workers=40,
                            max_queue_size=40,
                            verbose=1,
                            epochs=opts['n_epoch'],
                            callbacks=callbacks)
     
        vars['n_input'] = n_input
        vars['n_output'] = params['n_classes']
        vars['model'] = model

    except Exception as e:
        print_exception(e, 'train')
        sys.exit()


def forward(data, probs_or_score, opts, vars):
    try:
        model = vars['model']
        sess = vars['session']
        graph = vars['graph']   

        if model and sess and graph:   

            n_output = len(probs_or_score)
            npdata = np.asarray(data)
            img = Image.fromarray(npdata)
           
            #img.save('test_org.jpg')
            x = img.resize((opts['image_height'], opts['image_width']))
            #imgR.save('test.jpg')
            x = np.expand_dims(x, axis=0)

            results = np.zeros((1, n_output), dtype=np.float32)
            with sess.as_default():
                with graph.as_default():
                    pred = model.predict(x, batch_size=1, verbose=0)
                    for i in range(len(pred[0])):
                        results[0][i] = pred[0][i]

            mean = np.mean(results, axis=0)
            std = np.std(results, axis=0)
            conf = max(0,1-np.mean(std))

            #sanity_check(probs_or_score)
            
            for i in range(len(mean)):
                probs_or_score[i] = mean[i]

            return conf
        else:
            print('Train model first') 
            return 1

    except Exception as e: 
        print_exception(e, 'forward')
        sys.exit()

def save(path, opts, vars):
    try:
        # save model
        model_path = path + '.' + opts['network'] + '.h5'
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
            if os.path.isfile(fullFileName) and ((fileName.endswith(opts['network']+'.py') or fileName.endswith('interface.py')  or fileName.endswith('customlayer.py') or fileName.endswith('nova_data_generator.py')  or fileName.endswith('db_handler.py'))) :
                shutil.copy(fullFileName, dstDir)

        
        network_new =  os.path.basename(path) + '.' + opts['network']
        shutil.copy(dstDir + opts['network'] + '.py', dstDir + network_new + '.py')

    except Exception as e: 

        print_exception(e, 'save')
        sys.exit()

def load(path, opts, vars):
    try:
        print('create session and graph')   
        server = tf.train.Server.create_local_server()
        sess = tf.Session(server.target) 
        graph = tf.get_default_graph()
        backend.set_session(sess) 
        
        print('\nload model from ' + path)
        trainerPath = path
        trainerPath = trainerPath.replace("trainer.PythonModel.model", "trainer")

        print('tp: {}'.format(trainerPath))

        # # parsing trainer to retrieve input output
        xmldoc = minidom.parse(trainerPath)
        # streams = xmldoc.getElementsByTagName('streams')
        # streamItem = streams[0].getElementsByTagName('item')
        # n_input = streamItem[0].attributes['dim'].value
        # print("#input: " + str(n_input))
        
        classes = xmldoc.getElementsByTagName('classes')
        n_output = len(classes[0].getElementsByTagName('item'))
        print("#output: " + str(n_output))

        # copy unique network file
        network_tmp = os.path.dirname(path) + '\\' + opts['network'] + '.py'
        shutil.copy(path + '.' + opts['network'] + '.py', network_tmp)
        print ('\nload training configuration from ' + network_tmp)

        # reload sys path and import network file
        importlib.reload(site)

        # loading network specific options 
        module = __import__(opts['network'])
        set_opts_from_config(opts, module.conf)

        # load weights
        
        weight_path = path + '.' + opts['network'] + '_weights.h5'
        model_path = path + '.' + opts['network'] + '.h5'

        print('\nModelpath {}'.format(model_path))
        
        #f = h5py.File(model_path, 'r')
        #print(f.attrs.get('keras_version'))  

        model = load_model(model_path);     
        n_input = (opts['image_width'],  opts['image_height'], 3)
        #model = module.getModel(n_input, int(n_output))
        #model.load_weights(weight_path)
        
        n_input = (1, opts['image_width'],  opts['image_height'], 3)

        # creating the predict function in order to avoid graph-scope errors later
        print('create prediction function')

        model._make_predict_function()
        with graph.as_default():
            with sess.as_default():
                model.predict(np.zeros(n_input))

        vars['graph'] = graph
        vars['session'] = sess
        vars['model'] = model

        print('\nload model from ' + path)
    
        trainerPath = path
        trainerPath = trainerPath.replace("trainer.PythonModel.model", "trainer")

        print('tp: {}'.format(trainerPath))

       

    except Exception as e:
        print_exception(e, 'load')
        sys.exit()


# helper functions
def convert_to_one_hot(label, n_classes):
    int_label = int(label)
    one_hot = np.zeros(n_classes)
    one_hot[int_label] = 1.0
    return one_hot


def print_exception(exception, function):
    exc_type, exc_obj, exc_tb = sys.exc_info()
    fname = os.path.split(exc_tb.tb_frame.f_code.co_filename)
    print('Exception in {}: {} \nType: {} Fname: {} LN: {} '.format(function, exception, exc_type, fname, exc_tb.tb_lineno))


def set_opts_from_config(opts, conf):
    print(conf)

    for key, value in conf.items():
        opts[key] = value

    print('Options haven been set to:\n')
    pprint.pprint(opts)
    print('\n')

#checking the input for corrupted values
def sanity_check(x):
    if np.any(np.isnan(x)):
        print('At least one input is not a number!')
    if np.any(np.isinf(x)):
        print('At least one input is inf!')