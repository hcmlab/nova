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
import time
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
from keras import optimizers
from keras.preprocessing import image as kerasimage
from keras.models import load_model
from keras.callbacks import TensorBoard, ModelCheckpoint
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
        vars['model_id'] = "."
        vars['monitor'] = ""


        '''Setting the default options. All options can be overwritten by adding them to the conf-dictionary in the same file as the network'''
        opts['network'] = ''
        opts['experiment_id'] = ''
        opts['is_regression'] = False
        opts['n_fp'] = 1
        opts['loss_function'] = 'categorical_crossentropy'
        opts['optimizer'] = 'adam'
        opts['metrics'] = ['accuracy']
        opts['lr'] = 0.0001
        opts['batch_size'] = 32
        opts['n_epoch'] = 10
        opts['image_width'] = 224
        opts['image_height'] = 224
        opts['n_channels'] = 3
        opts['shuffle'] = True
        opts['max_queue_size'] = 20
        opts['workers'] = 4
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
    
    try:
        module = __import__(opts['network'])
        set_opts_from_config(opts, module.conf)

        n_input = opts['image_width'] * opts['image_height'] * opts['n_channels']

        if not opts['is_regression']:
            #adding one output for the restclass
            n_output = int(max(label_score)+1)
            vars['monitor'] = "acc"
        else:
            n_output = 1
            vars['monitor'] = "loss"

        # callbacks
        log_path, ckp_path = get_paths()

        experiment_id = opts['experiment_id'] if opts['experiment_id'] else opts['network'] + '-' + str(time.strftime("%Y_%m_%d-%H_%M_%S")) 
        print('Checkpoint dir: {}\nLogdir: {}\nExperiment ID: {}'.format(ckp_path, log_path, experiment_id))
        tensorboard = TensorBoard(
            log_dir=os.path.join(log_path, experiment_id),
            write_graph=True,
            write_images=True,
            update_freq='batch')

        checkpoint = ModelCheckpoint(    
            filepath =  os.path.join(ckp_path, experiment_id +  '.trainer.PythonModel.model.h5'),   
            monitor=vars['monitor'], 
            verbose=1, 
            save_best_only=True, 
            save_weights_only=False, 
            mode='auto', 
            period=1)
        callbacklist = [tensorboard, checkpoint]

        # data
        training_generator = DataGenerator(dim=(opts['image_width'], opts['image_height']), n_channels=opts['n_channels'] ,batch_size=opts['batch_size'], n_classes=n_classes)     
        
        # model
        model = module.getModel(shape=(opts['image_width'], opts['image_height'], opts['n_channels']), n_classes=n_classes)
        model.compile(optimizer=opts['optimizer'], loss=opts['loss_function'], metrics=opts['metrics'])  
        print(model.summary())
        model.fit_generator(generator=training_generator,
                            shuffle=opts['shuffle'],
                            workers=opts['workers'],
                            max_queue_size=opts['max_queue_size'],
                            verbose=1,
                            epochs=opts['n_epoch'],
                            callbacks=callbacklist)
     
        # setting variables
        vars['model_id'] = experiment_id
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
            b, g, r = img.split()
            img = Image.merge("RGB", (r, g, b))
            x = img.resize((opts['image_height'], opts['image_width']))
          
            x = kerasimage.img_to_array(x)
            x = np.expand_dims(x, axis=0)
            x = x*(1./255)
  
            with sess.as_default():
                with graph.as_default():
                    pred = model.predict(x, batch_size=1, verbose=0)

            #sanity_check(probs_or_score)
            
            for i in range(len(pred[0])):
                probs_or_score[i] = pred[0][i]  
            return max(probs_or_score)

        else:
            print('Train model first') 
            return 1

    except Exception as e: 
        print_exception(e, 'forward')
        sys.exit()

def save(path, opts, vars):
    try:
        # save model
        _, ckp_path = get_paths()

        model_path = path + '.' + opts['network']
        print('Move best checkpoint to ' + model_path + '.h5')
        shutil.move(os.path.join(ckp_path, vars['model_id'] + '.trainer.PythonModel.model.h5'), model_path + '.h5')

        # copy scripts
        src_dir = os.path.dirname(os.path.realpath(__file__))
        dst_dir = os.path.dirname(path)

        print('copy scripts from \'' + src_dir + '\' to \'' + dst_dir + '\'')

        srcFiles = os.listdir(src_dir)
        for fileName in srcFiles:
            full_file_name = os.path.join(src_dir, fileName)
            if os.path.isfile(full_file_name) and (fileName.endswith('interface.py')  or fileName.endswith('customlayer.py') or fileName.endswith('nova_data_generator.py')  or fileName.endswith('db_handler.py')):
                shutil.copy(full_file_name, dst_dir)
            elif os.path.isfile(full_file_name) and fileName.endswith(opts['network']+'.py'):
                shutil.copy(full_file_name, os.path.join(dst_dir, model_path + '.py' ))
                

    except Exception as e: 
        print_exception(e, 'save')
        sys.exit()

def load(path, opts, vars):
    try:
        print('\nLoading model\nCreating session and graph')   
        server = tf.train.Server.create_local_server()
        sess = tf.Session(server.target) 
        graph = tf.get_default_graph()
        backend.set_session(sess) 
        
        model_path = path + '.' + opts['network'] + '.h5'
        print('Loading model from {}'.format(model_path))
        model = load_model(model_path);     

        
        print('Create prediction function')

        model._make_predict_function()
        with graph.as_default():
            with sess.as_default():
                input_shape = list(model.layers[0].input_shape)
                input_shape[0] = 1
                model.predict(np.zeros(tuple(input_shape)))

        vars['graph'] = graph
        vars['session'] = sess
        vars['model'] = model
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
    for key, value in conf.items():
        opts[key] = value

    print('\nOptions haven been set to:\n')
    pprint.pprint(opts)
    print('\n')

# checking the input for corrupted values
def sanity_check(x):
    if np.any(np.isnan(x)):
        print('At least one input is not a number!')
    if np.any(np.isinf(x)):
        print('At least one input is inf!')

# retreives the paths for log and checkpoint directories. paths are created if they do not exist
def get_paths():
    file_dir = os.path.dirname(os.path.realpath(__file__)) 
    ckp_dir = 'checkpoints'
    log_dir = 'logs'
    ckp_path = os.path.join(file_dir, ckp_dir)
    log_path = os.path.join(file_dir, log_dir)

    if not os.path.exists(ckp_path):
        os.makedirs(ckp_path)
        print('Created checkpoint folder: {}'.format(ckp_path))
    if not os.path.exists(log_path):
        os.makedirs(log_path)
        print('Created log folder: {}'.format(log_path))
    
    return(log_path, ckp_path)

