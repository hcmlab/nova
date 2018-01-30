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
import pprint
from tensorflow.python.keras.models import load_model
from tensorflow.python.keras.callbacks import ModelCheckpoint
from tensorflow.python.keras import backend


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
        opts['n_timesteps'] = 1
        opts['is_regression'] = True
        opts['perma_drop'] = False
        opts['n_fp'] = 1
        opts['loss_function'] = 'mean_squared_error'
        opts['optimizier'] = 'rmsprop'
        opts['metrics'] = []
        opts['lr'] = 0.001
        opts['n_epoch'] = 10
        opts['batch_size'] = 32

    except Exception as e:
        print_exception(e, 'getOptions')
        exit()


def train(data,label_score, opts, vars):
    try:
        if backend.backend() == 'tensorflow':
            setSessionTF(vars)

        n_input = data[0].dim
        
        if opts['is_regression']:
            n_output = 1
        else:
            n_output = max(label_score)+1

        print ('load network architecture from ' + opts['network'] + '.py')
        print('#input: {} \n#output: {}'.format(n_input, n_output))
        module = __import__(opts['network'])
        model = module.getModel(n_input, n_output)
        set_opts_from_config(opts, module.conf)

        nts = opts['n_timesteps']
        if nts < 1:
            raise ValueError('n_timesteps must be >= 1 but is {}'.format(nts))     
        elif nts == 1:
            sample_shape = (n_input, )   
        else: 
            sample_shape = (int(n_input / nts), nts)

        #(number of samples, number of features, number of timesteps)
        sample_list_shape = ((len(data),) + sample_shape)
        train_x = np.empty(sample_list_shape)

        train_y = np.empty((len(label_score), n_output))

        print('Input data array should be of shape: {} \nLabel array should be of shape {} \nStart reshaping input accordingly...\n'.format(train_x.shape, train_y.shape))
    
        #reshaping
        for sample in range(len(data)):
            #input
            x = np.reshape(data[sample], sample_shape) 
            train_x[sample] = x
            #output
            if not opts['is_regression']:
                train_y[sample] = convert_to_one_hot(label_score[sample], n_output)
            else:
                train_y[sample] = label_score[sample] 

        train_x = train_x.astype('float32') 
        model.compile(loss=opts['loss_function'],
                optimizer=opts['optimizier'],
                metrics=opts['metrics'])

        model.fit(train_x,
            train_y,
            epochs=opts['n_epoch'],
            batch_size=opts['batch_size'])        

        vars['n_input'] = n_input
        vars['n_output'] = n_output
        vars['model'] = model
    
    except Exception as e:
        print_exception(e, 'train')
        exit()

def forward(data, probs_or_score, opts, vars):
    try:
        if backend.backend() == 'tensorflow':
            setSessionTF(vars)
        
        model = vars['model']

        if model is None:
            load_forward(vars['network'], opts, vars)
            model = vars['model']
           
        n_input = data.dim
        n_output = len(probs_or_score)
        n_fp = int(opts['n_fp'])

        #reshaping the input
        nts = opts['n_timesteps']
        if nts < 1:
            raise ValueError('n_timesteps must be >= 1 but is {}'.format(nts))     
        elif nts == 1:
            sample_shape = (n_input,)   
        else:  
            sample_shape = (int(n_input / nts), nts)

        np_array_x = np.asarray(data)
        np_array_x = np_array_x.astype('float32')
        # np_array_x = np.reshape(np_array_x, (1,) + sample_shape)
        np_array_x.reshape((1,) + sample_shape)
        results = np.zeros((n_fp, n_output), dtype=np.float32)
        for fp in range(n_fp):
            pred = model.predict(np_array_x, batch_size=1, verbose=0)
            if opts['is_regression']:
                 results[fp][0] = pred[0][0]
            else:     
                for i in range(len(pred[0])):
                    results[fp][i] = pred[0][i]

        mean = np.mean(results, axis=1)
        std = np.std(results, axis=1)
        conf = max(0,1-np.mean(std))

        #print(results)
        print('---')
        print(mean)
        print(std)
        print(conf)
        print('---')
        probs_or_score = mean
        return conf

    except Exception as e: 
        print_exception(e, 'forward')
        exit()

def save(path, opts, vars):
    try:
        # save model
        model_path = path + '.' + opts['network'] + '_weights.h5'
        print('save model to ' + model_path)
        model = vars['model']
        #model.save(model_path)
        model.save_weights(model_path)

        # copy scripts
        srcDir = os.path.dirname(os.path.realpath(__file__)) + '\\' 
        dstDir = os.path.dirname(path) + '\\'

        print('copy scripts from \'' + srcDir + '\' to \'' + dstDir + '\'')

        if not os.path.exists(dstDir):
            os.makedirs(dstDir)

        srcFiles = os.listdir(srcDir)
        for fileName in srcFiles:
            fullFileName = os.path.join(srcDir, fileName)
            if os.path.isfile(fullFileName) and (fileName.endswith(opts['network']+'.py') or 'interface' in fileName or 'customlayer' in fileName) :
                shutil.copy(fullFileName, dstDir)

        
        network_new =  os.path.basename(path) + '.' + opts['network']
        shutil.move(dstDir + opts['network'] + '.py', dstDir + network_new + '.py')
    except Exception as e: 
        print_exception(e, 'save')
        exit()

def load(path, opts, vars):
    vars['network'] = path
    vars['model'] = None


def load_forward(path, opts, vars):
    try:
        print('\nload model from ' + path)
    
        trainerPath = path
        trainerPath = trainerPath.replace("trainer.PythonModel.model", "trainer")

        print('tp: {}'.format(trainerPath))

        # parsing trainer to retreive input output
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
        print ('\nload training configuration from ' + network_tmp)

        # reload sys path and import network file
        importlib.reload(site)

        # loading network specific options 
        module = __import__(opts['network'])
        set_opts_from_config(opts, module.conf)
    
        # load weights
        weigth_path = path + '.' + opts['network'] + '_weights.h5'

        print('\nModelpath {}'.format(weigth_path))
        model = module.getModel(int(n_input), int(n_output))
        model.load_weights(weigth_path)
        vars['model'] = model

    except Exception as e:
        print_exception(e, 'load')
        exit()

# helper functions
def setSessionTF(vars):
    if not vars['session']:
        print('create tf session')
        import tensorflow as tf
        server = tf.train.Server.create_local_server()
        sess = tf.Session(server.target)
        vars['session'] = sess

    backend.set_session(vars['session'])


def convert_to_one_hot(label, n_classes):
    float_label = int(label)
    one_hot = np.zeros(n_classes)
    one_hot[label] = 1.0
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
