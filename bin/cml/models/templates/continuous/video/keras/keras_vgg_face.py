import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

from tensorflow.python.keras.layers import Input, Dense, Dropout
from tensorflow.python.keras.models import Sequential
from tensorflow.contrib.keras.api.keras.initializers import glorot_uniform
from keras.models import Model
from keras.layers import Flatten, Dense, Input, Dropout
from keras_vggface.vggface import VGGFace



conf = {
        #general
        'n_timesteps' : 1, #should be equal to the number of samples in the left context + 1    
        'dropout_rate' : 0.5,

        #confidence calculation
        'perma_drop' : False, #uses dropout also during testing
        'n_fp' : 1, #number of forward passes for each prediction in order to calculate the confidence

        #compile
        'loss_function' : 'mse',    
        'optimizier' : 'adam',  
        'metrics' : ['mean_squared_error'],
        'lr' : 0.0001,

        #fit
        'n_epoch' : 10,
        'batch_size' : 64
       
    }


def getModel (shape, n_classes):
    hidden_dim = 512   
    base_model = VGGFace(include_top=False, input_shape=shape)

    for layer in base_model.layers:
        layer.trainable = False

    x = base_model.output
    x = Flatten()(x)
    x = Dense(hidden_dim, activation='relu', name='fc6')(x)
    x = Dropout(0.25)(x)
    x = Dense(hidden_dim, activation='relu', name='fc7')(x)
    predictions = Dense(n_classes, activation='linear')(x)

    model = Model(inputs=base_model.input, outputs=predictions)

    return model

