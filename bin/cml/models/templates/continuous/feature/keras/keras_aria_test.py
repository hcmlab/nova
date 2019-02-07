import sys
import random
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

from tensorflow.python.keras.layers import Input, Dense, Dropout, LSTM
from tensorflow.python.keras.models import Sequential
from tensorflow.contrib.keras.api.keras.initializers import glorot_uniform
from customlayer import CustomDropout

conf = {
        #general
        'n_timesteps' : 3, #should be equal to the number of samples in the left context + 1    
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
    
    # model.add(Dense(round(n_input * 2), activation='relu' , input_dim=n_input))
    # model.add(Dropout(0.5))
    # model.add(CustomDropout(conf['dropout_rate'], conf['perma_drop']))
    # model.add(Dense(round(n_input), activation='relu'))
    # model.add(Dense(round(n_input / 2), activation='relu'))
    # model.add(CustomDropout(conf['dropout_rate'], conf['perma_drop']))
    # model.add(Dense(round(n_input / 3), activation='relu'))
    # model.add(Dense(units=n_output, activation='sigmoid'))  
    
    # model = Sequential()
    # model.add(Dense(round(n_input), activation='relu', input_dim=n_input)) 
    # model.add(Dropout(conf['dropout_rate']))
    # model.add(Dense(units=round(n_input ), activation='relu'))    
    # model.add(Dense(units=round(n_input / 2 ), activation='relu'))         
    # model.add(Dense(units=n_output, activation='sigmoid'))  
    
    asdf = random.seed()
    
    # model.add(Dense(round(n_input), activation='relu' ,kernel_initializer=glorot_uniform(seed=asdf), input_dim=n_input))
    # model.add(Dropout(0.5))
    # model.add(Dense(round(n_input / 2), activation='relu',kernel_initializer=glorot_uniform(seed=asdf)))
    # model.add(Dropout(0.5))
    # model.add(Dense(round(n_input / 3), activation='relu', kernel_initializer=glorot_uniform(seed=asdf)))
    # model.add(Dense(units=n_output, activation='sigmoid', kernel_initializer=glorot_uniform(seed=asdf)))
    
    #model.add(Dense(round(n_input * 2), activation='relu' , input_dim=n_input,kernel_initializer=glorot_uniform(seed=asdf)))
    model.add(LSTM(32, input_shape=(int(n_input / conf['n_timesteps']), conf['n_timesteps'])))
    model.add(Dropout(0.25))
    model.add(Dense(round(n_input), activation='relu',kernel_initializer=glorot_uniform(seed=asdf)))
    model.add(Dense(round(n_input / 2), activation='relu',kernel_initializer=glorot_uniform(seed=asdf)))
    model.add(Dense(units=n_output, activation='sigmoid',kernel_initializer=glorot_uniform(seed=asdf)))  

    return model
    

