'''
soundnet_ssi.py
author: Johannes Wagner <wagner@hcm-lab.de>
created: 2017/11/14
Copyright (C) University of Augsburg, Lab for Human Centered Multimedia

Feature extraction using SoundNet

https://github.com/eborboihuc/SoundNet-tensorflow
https://github.com/cvondrick/soundnet

'''

import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

import os
import numpy as np
import site as s
s.getusersitepackages()
import tensorflow as tf
import resampy

from soundnet_model import Model

SAMPLE_RATE = 22050


def error(text):
    print('ERROR: ' + text)


def warning(text):
    print('WARNING: ' + text)


def getOptions(opts,vars):

    opts['path'] = './sound8.npy'
    opts['layer'] = 18
    opts['n_feature'] = 256
    opts['n_samples'] = 12765 # minimum length
    opts['trim'] = True    

    vars['model'] = None
    vars['warn_enlarge'] = False
    vars['warn_trim'] = False
    vars['warn_reduce'] = False
    vars['warn_resample'] = False


def loadModel(opts,vars):

    path = opts['path']    

    print ('load model "{}"'.format(path))
   
    if not os.path.exists(path):
        error('model not found "{}"'.format(path))
        return  

    param_G = np.load(path, encoding = 'latin1', allow_pickle="true").item()
    
    # Init Session
    sess_config = tf.compat.v1.ConfigProto()
    sess_config.allow_soft_placement=True
    sess_config.gpu_options.allow_growth = True
    sess = tf.compat.v1.InteractiveSession(config=sess_config)

    # Config
    local_config = {  
        'batch_size': 1, 
        'eps': 1e-5,
        'name_scope': 'SoundNet',        
    }
 
    # Build model
    model = Model(sess, config=local_config, param_G=param_G)          
  
    # Run session
    init = tf.compat.v1.global_variables_initializer()
    sess.run(init) 
       
    if not model.load():
        error('could not load model'.format(path))
        return

    # Check layer
    if opts['layer'] > len(model.layers)+1:
        opts['layer'] = len(model.layers)+1
        warning('set layer to {}'.format(len(model.layers)+1))

    vars['model'] = model


def preprocess(raw_audio, sr, opts, vars):
    
    length = opts['n_samples']

    # Select first channel (mono)
    if raw_audio.shape[1] > 1:
        raw_audio = raw_audio[:,0]

    # Convert sample rate
    if sr != SAMPLE_RATE:
        if not vars['warn_resample']:
            warning('resample audio from {} to {}'.format(sr, SAMPLE_RATE))
            vars['warn_resample'] = True
        raw_audio = resampy.resample(raw_audio, sr, SAMPLE_RATE, axis=0)

    # Make range [-256, 256]
    raw_audio *= 256.0

    # Enlarge
    if length > raw_audio.shape[0]:
        if not vars['warn_enlarge']:
            warning('enlarge audio from {} to {}'.format(raw_audio.shape[0], length))
            vars['warn_enlarge'] = True
        reps = int(length/raw_audio.shape[0]) + 1
        raw_audio = np.tile(raw_audio, [reps,1])        
        raw_audio = raw_audio[0:length]

    # Trim
    if length < raw_audio.shape[0] and opts['trim']:
        if not vars['warn_trim']:
            warning('trim audio from {} to {}'.format(raw_audio.shape[0], length))
            vars['warn_trim'] = True             
        raw_audio = raw_audio[0:length]                   

    # Shape to 1 x DIM x 1 x 1
    raw_audio = np.reshape(raw_audio, [1, -1, 1, 1])

    return raw_audio.copy()


def getSampleDimensionOut(dim, opts, vars):

    return opts['n_feature']


def getSampleTypeOut(type, types, opts, vars): 

    if type != types.FLOAT and type != types.DOUBLE:  
        print('types other than float and double are not supported') 
        return types.UNDEF

    return type


def transform_enter(sin, sout, sxtra, board, opts, vars): 

    vars['warn_enlarge'] = False
    vars['warn_reduce'] = False
    vars['warn_resample'] = False

    loadModel(opts, vars)


def transform(info, sin, sout, sxtra, board, opts, vars): 

    input = np.asarray(sin)
    output = np.asarray(sout)

    model = vars['model']
    layer = opts['layer']    

    if not model:
        output.fill(0)
        return

    # Extract feature    

    sound = preprocess(input.copy(), sin.sr, opts, vars)
    feed_dict = { model.sound_input_placeholder: sound }       
    features = model.sess.run(model.layers[layer], feed_dict=feed_dict)
    features = np.squeeze(features)

    # Reduce feature dimension if necessary

    if len(features.shape) > 1:    
        if not vars['warn_reduce']:
            warning('reduce feature dimension from {} to 1'.format(features.shape[0]))
            vars['warn_reduce'] = True
        features = np.mean(features, axis=0)
    
    np.copyto(output, features)


def transform_flush(sin, sout, sxtra, board, opts, vars): 

    model = vars['model']
    model.sess.close()
