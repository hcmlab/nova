import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']
import os
from tensorflow.python.keras.layers import Input, Dense, Dropout, Flatten
from tensorflow.python.keras.models import Sequential
#from tensorflow.keras.api.keras.initializers import glorot_uniform
from tensorflow.keras.models import Model
from vggface import get_vggface_model



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
        'n_epoch' : 50,
        'batch_size' : 16,

        'image_width' : 224,
        'image_height' : 224,
        'n_channels' : 3
       
    }


def getModel (shape, n_classes):
    hidden_dim = 512   

    base_model = get_vggface_model(shape)
    for layer in base_model.layers:
        layer.trainable = False

    
    x = base_model.layers[-2].output
    x = Flatten()(x)
    x = Dense(hidden_dim, activation='relu', name='fc6')(x)
    # # #x = Dropout(0.25)(x)
    x = Dense((int)(hidden_dim/2), activation='relu', name='fc7')(x)
    predictions = Dense(n_classes, activation='softmax')(x)

    model = Model(inputs=base_model.input, outputs=predictions)

    return model

