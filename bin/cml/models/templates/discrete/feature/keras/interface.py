import sys
import importlib
if not hasattr(sys, 'argv'):
	sys.argv  = ['']
import numpy as np
import random
from xml.dom import minidom
import os
import shutil
import pprint
import pickle
import site as s
s.getusersitepackages()

import tensorflow as tf
gpus = tf.config.experimental.list_physical_devices('GPU')
#tf.config.experimental.set_memory_growth(gpus[0], True)

from tensorflow.keras.models import load_model
from tensorflow.keras.callbacks import ModelCheckpoint, TensorBoard
from tensorflow.keras import backend, optimizers
tf.compat.v1.disable_v2_behavior()

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

        '''Setting the default options. All options can be overwritten by adding them to the conf-dictionary in the same file as the network'''
        opts['network'] = ''
        opts['experiment_id'] = ''
        opts['is_regression'] = False
        opts['n_timesteps'] = 1
        opts['perma_drop'] = False
        opts['n_fp'] = 1
        opts['loss_function'] = 'mean_squared_error'
        opts['optimizier'] = 'adam'
        opts['metrics'] = ['accuracy']
        opts['lr'] = 0.0001
        opts['n_epoch'] = 1
        opts['batch_size'] = 32

    except Exception as e:
        print_exception(e, 'getOptions')
        sys.exit()


def train(data,label_score, opts, vars):
    try:
         module = __import__(opts['network'])
         set_opts_from_config(opts, module.conf)

         n_input = data[0].dim
        
         if not opts['is_regression']:
             #adding one output for the restclass
             n_output = int(max(label_score)+1)
             vars['monitor'] = "acc"
         else:
             vars['monitor'] = "loss"
             n_output = 1
            

         print ('load network architecture from ' + opts['network'] + '.py')
         print('#input: {} \n#output: {}'.format(n_input, n_output))
         model = module.getModel(n_input, n_output)
        

         nts = opts['n_timesteps']
         if nts < 1:
             raise ValueError('n_timesteps must be >= 1 but is {}'.format(nts))     
         elif nts == 1:
             sample_shape = (n_input, )   
         else: 
             sample_shape = (int(n_input / nts), nts)

         #(number of samples, number of features, number of timesteps)
         sample_list_shape = ((len(data),) + sample_shape)

         x = np.empty(sample_list_shape)
         y = np.empty((len(label_score), n_output))
       
         print('Input data array should be of shape: {} \nLabel array should be of shape {} \nStart reshaping input accordingly...\n'.format(x.shape, y.shape))

         sanity_check(x)
        
         #reshaping
         for sample in range(len(data)):
             #input
             temp = np.reshape(data[sample], sample_shape) 
             x[sample] = temp
             #output
             if not opts['is_regression']:
                 y[sample] = convert_to_one_hot(label_score[sample], n_output)
             else:
                 y[sample] = label_score[sample] 

         log_path, ckp_path = get_paths()

         experiment_id = opts['experiment_id'] if opts['experiment_id'] else opts['network']
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
         callbacklist = [checkpoint]

         model.summary()
         print('Loss: {}\nOptimizer: {}\n'.format(opts['loss_function'], opts['optimizier']))
         model.compile(loss=opts['loss_function'],
                 optimizer=opts['optimizier'],
                 metrics=opts['metrics'])

         print('train_x shape: {} | train_x[0] shape: {}'.format(x.shape, x[0].shape))

         model.fit(x,
             y,
             epochs=opts['n_epoch'],
             batch_size=opts['batch_size'],
             callbacks=callbacklist)        

         vars['n_input'] = n_input
         vars['n_output'] = n_output
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

            x = np.asarray(data)
            x = x.astype('float32')
            x.reshape(sample_shape) 

            sanity_check(data)

            results = np.zeros((n_fp, n_output), dtype=np.float32)

            with sess.as_default():
                with graph.as_default():
                    for fp in range(n_fp):
                        pred = model.predict(x, batch_size=1, verbose=0)
                        if opts['is_regression']:
                            results[fp][0] = pred[0][0]
                        else:     
                            for i in range(len(pred[0])):
                                results[fp][i] = pred[0][i]
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
        exit()

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
        server =  tf.compat.v1.train.Server.create_local_server()
        sess = tf.compat.v1.Session(server.target) 
        graph = tf.compat.v1.get_default_graph()
        tf.compat.v1.keras.backend.set_session(sess)

        
        model_path = path + '.' + opts['network'] + '.h5'
        print('Loading model from {}'.format(model_path))
        model = load_model(model_path);     

        
        print('Create prediction function')

        model._make_predict_function()
        with graph.as_default():
            with sess.as_default():
                input_shape = model.layers[0].input_shape
                #input_shape = list(model.layers[0].input_shape)
                #input_shape[0] = 1
                #print('SHAPE:')
                #print(input_shape)
                #model.predict(np.zeros(tuple(input_shape)))
                #model.predict(np.zeros(input_shape))

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

def main():
    print("Hello World!")

if __name__ == "__main__":
    opts = []
    vars = []
    getOptions(opts, vars)
    train([0,1,2,2], [0,0,1,1], opts, vars)