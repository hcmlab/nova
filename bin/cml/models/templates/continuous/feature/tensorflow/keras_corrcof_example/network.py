import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']

from tensorflow.python.keras.layers import Input, Dense, Dropout
from tensorflow.python.keras.models import Sequential


def getModel (n_input, n_output):
    model = Sequential()
    model.add(Dense(round(n_input), activation='relu' , input_dim=n_input))
    model.add(Dropout(0.8))
    model.add(Dense(round(n_input / 2), activation='relu'))
    model.add(Dropout(0.8))
    model.add(Dense(round(n_input / 3), activation='relu'))
    model.add(Dense(units=n_output, activation='tanh'))
    return model