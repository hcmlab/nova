import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

from tensorflow.python.keras.layers import Input, Dense, Dropout, LSTM
from tensorflow.python.keras.models import Sequential

conf = {
        #general
        'n_timesteps' : 1, #should be equal to the number of samples in the left context + 1    
        'is_regression' : True,
        'dropout_rate' : 0.1,

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
    #model.add(LSTM(round(n_input), input_shape=(conf['n_timesteps'], int(n_input / conf['n_timesteps']))))
    model.add(Dense(round(n_input), activation='relu' , input_dim=n_input))
    model.add(Dense(round(n_input/2), activation='relu'))
    model.add(Dense(units=n_output, activation='tanh'))

    return model
    

