import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

from tensorflow.python.keras.layers import Input, Dense, Dropout
from tensorflow.python.keras.models import Sequential
from customlayer import CustomDropout

conf = {
        #general
        'n_timesteps' : 1, #should be equal to the number of samples in the left context + 1    
        'dropout_rate' : 0.5,

        #confidence calculation
        'perma_drop' : False, #uses dropout also during testing
        'n_fp' : 3, #number of forward passes for each prediction in order to calculate the confidence

        #compile
        'loss_function' : 'binary_crossentropy',    
        'optimizier' : 'adam',  
        'metrics' : ['accuracy'],
        'lr' : 0.0001,

        #fit
        'n_epoch' : 10,
        'batch_size' : 32, 
       
    }

customObjects = {
    'CustomDropout' : CustomDropout
}

def getModel (n_input, n_output):
    model = Sequential()
    model.add(Dense(round(n_input * 2), activation='relu' , input_dim=n_input))
    model.add(Dense(n_input, activation="relu"))
    model.add(Dense(units=n_output, activation='softmax'))  

    return model
    

