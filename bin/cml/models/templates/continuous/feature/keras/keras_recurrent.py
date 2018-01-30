import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

from tensorflow.python.keras.layers import Input, Dense, Dropout, LSTM
from tensorflow.python.keras.models import Sequential

conf = {
        #general
        'n_timesteps' : 5, #should be equal to the number of samples in the left context + 1    
        'is_regression' : True,
        'dropout_rate' : 0.5,

        #confidence calculation
        'perma_drop' : True, #uses dropout also during testing
        'n_fp' : 3, #number of forward passes for each prediction in order to calculate the confidence

        #compile
        'loss_function' : 'mean_squared_error',    
        'optimizier' : 'rmsprop',  
        'metrics' : ['mse'],
        'lr' : 0.0001,

        #fit
        'n_epoch' : 1,
        'batch_size' : 32, 
       
    }


def getModel (n_input, n_output):
    model = Sequential()
    model.add(LSTM(32, input_shape=(int(n_input / conf['n_timesteps']), conf['n_timesteps'])))
    model.add(Dense(1))

    return model
    