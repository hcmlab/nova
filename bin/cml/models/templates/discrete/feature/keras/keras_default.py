import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

from tensorflow.keras.layers import Input, Flatten, Dense, Input, Dropout
from tensorflow.keras.models import Model, Sequential
from customlayer import CustomDropout

#from keras.models import Model, Sequential
#from keras.layers import Flatten, Dense, Input, Dropout

conf = {
        #general
        'n_timesteps' : 1, #should be equal to the number of samples in the left context + 1    
        'dropout_rate' : 0.5,

        #confidence calculation
        'perma_drop' : False, #uses dropout also during testing
        'n_fp' : 1, #number of forward passes for each prediction in order to calculate the confidence

        #compile
        'loss_function' : 'categorical_crossentropy',    
        'optimizier' : 'adam',  
        'metrics' : ['accuracy'],
        'lr' : 0.0001,

        #fit
        'n_epoch' : 10,
        'batch_size' : 64, 
       
    }

customObjects = {
    'CustomDropout' : CustomDropout
}

def getModel (n_input, n_output):
    # model = Sequential()
    # model.add(Dense(round(n_input * 2), activation='relu' , input_dim=n_input))
    # model.add(Dense(n_input, activation="relu"))
    # model.add(Dense(units=n_output, activation='softmax'))  


    visible = Input(shape=(n_input,))
    x = Dense(round(n_input * 2), activation='relu', name="dense_a")(visible)
    x = Dense(round(n_input), activation='relu', name="dense_c")(x)
    output = Dense(n_output, activation='softmax', name="dense_d")(x)
    model = Model(inputs=visible, outputs=output)


    return model
    

