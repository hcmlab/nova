import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

from tensorflow.python.keras.layers import Input, Dense, Dropout, LSTM
from tensorflow.python.keras.models import Sequential
from customlayer import CustomDropout

conf = {
        #general
        'n_timesteps' : 1, #should be equal to the number of samples in the left context + 1    
        'is_regression' : True,
        'dropout_rate' : 0.5,

        #confidence calculation
        'perma_drop' : False, #uses dropout also during testing
        'n_fp' : 1, #number of forward passes for each prediction in order to calculate the confidence

        #compile
        'loss_function' : 'mean_squared_error',    
        'optimizier' : 'rmsprop',  
        'metrics' : ['mse'],
        'lr' : 0.0001,

        #fit
        'n_epoch' : 5,
        'batch_size' : 32, 
       
    }

customObjects = {
    'CustomDropout' : CustomDropout
}

def getModel (n_input, n_output):
    model = Sequential()
    #model.add(Dense(round(n_input * 2), activation='relu' , input_dim=n_input))
    #model.add(LSTM(32, input_shape=(int(n_input / conf['n_timesteps']), conf['n_timesteps'])))
    #model.add(Dropout(0.25))
    #model.add(Dense(round(n_input), activation='relu'))
    #model.add(Dense(round(n_input / 2), activation='relu'))
    #model.add(Dense(units=n_output, activation='sigmoid'))  

    model.add(Dense(round(n_input), activation='relu' , input_dim=n_input))
    model.add(Dropout(0.5))
    model.add(Dense(round(n_input / 2), activation='relu'))
    model.add(Dropout(0.5))
    model.add(Dense(round(n_input / 3), activation='relu'))
    model.add(Dense(units=n_output, activation='sigmoid'))
    
    return model
    

