import sys
import importlib
if not hasattr(sys, 'argv'):
	sys.argv  = ['']
import numpy as np
import random
from xml.dom import minidom
import os
import shutil
import site
import pprint
import pickle

from sklearn import tree



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
        opts['metrics'] = []
        opts['lr'] = 0.0001
        opts['n_epoch'] = 1
        opts['batch_size'] = 32

    except Exception as e:
        print_exception(e, 'getOptions')
        exit()


def train(data,label_score, opts, vars):
 
    try:
        n_input = data[0].dim
        
        if not opts['is_regression']:
            #adding one output for the restclass
            n_output = int(max(label_score)+1)
        else:
            n_output = 1
            

        print ('load network architecture from ' + opts['network'] + '.py')
        print('#input: {} \n#output: {}'.format(n_input, n_output))

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
        y = np.empty((len(label_score)))
 
        print('Input data array should be of shape: {} \nLabel array should be of shape {} \nStart reshaping input accordingly...\n'.format(x.shape, y.shape))

       
        
        #reshaping
        for sample in range(len(data)):
            #input
            temp = np.reshape(data[sample], sample_shape) 
            x[sample] = temp
            y[sample] = label_score[sample] 


        sanity_check(x)
        #model = LinearSVC(random_state=1234, tol=1e-4)

         
        model = tree.DecisionTreeClassifier()
        print('train_x shape: {} | train_x[0] shape: {}'.format(x.shape, x[0].shape))


        model.fit(x, y)  
      
        vars['n_input'] = n_input
        vars['n_output'] = n_output
        vars['model'] = model
       
        
    
    except Exception as e:
        print_exception(e, 'train')
        exit()

def forward(data, probs_or_score, opts, vars):
    try:
        model = vars['model']
     

         #reshaping the input
        

        if model:
            n_input = data.dim
            n_output = len(probs_or_score)
            n_fp = int(opts['n_fp'])
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
            n_output = len(probs_or_score)
            results = np.zeros((n_fp, n_output), dtype=np.float32)

            #TODO: Add logic for multiple time frames
            for fp in range(n_fp):
                pred = model.predict_proba(data)  
                # for i in range(len(pred[fp])):
                #     results[fp][i] = pred[fp][i]
                # print(pred)    

                # if opts['is_regression']:
                #             results[0] = pred[0]
                # else:     
                #     results[fp] = pred[0]
      
            #for i in range(len(mean)):
                # probs_or_score = pred
                # conf = 1
            #mean = np.mean(results, axis=0)
            #std = np.std(results, axis=0)
            #conf = max(0,1-np.mean(std))
            conf = max(pred[0])

            for i in range(pred.size):
                probs_or_score[i] = pred[0][i]

            return conf
       
        else:
            print('Train model first') 
            return 1

    except Exception as e: 
        print_exception(e, 'forward')
        exit()

def save(path, opts, vars):
    try:
 
        with open(path, 'wb') as f:
            pickle.dump(vars['model'], f)
        # # copy scripts
        src_dir = os.path.dirname(os.path.realpath(__file__))
        dst_dir = os.path.dirname(path)

        # print('copy scripts from \'' + src_dir + '\' to \'' + dst_dir + '\'')

        srcFiles = os.listdir(src_dir)
        for fileName in srcFiles:
            full_file_name = os.path.join(src_dir, fileName)
            if os.path.isfile(full_file_name) and (fileName.endswith('sklearn_decisiontree.py')):
                shutil.copy(full_file_name, dst_dir)


    except Exception as e: 
        print_exception(e, 'save')
        sys.exit()

def load(path, opts, vars):
  
    try:
        print('\nLoading model\n')   
        with open(path, 'rb') as f:
            model = pickle.load(f)
        vars['model'] = model

    except Exception as e:
        print_exception(e, 'load')
        sys.exit()
        

# helper functions

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

