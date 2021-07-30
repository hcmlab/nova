from tensorflow.python.keras.layers import Dropout
from tensorflow.python.keras import backend as K


class CustomDropout(Dropout):
    def __init__(self, rate, permanent=False, **kwargs):
        print('Perma:{}'.format(permanent))
        super(CustomDropout, self).__init__(rate, **kwargs) 
        self.permanent = permanent
        #self.uses_learning_phase = permanent


    def call(self, x, mask=None):
        if 0. < self.rate < 1.:
            noise_shape = self._get_noise_shape(x)
            if self.permanent:
                x = K.dropout(x, self.rate)
            else:       
                x = K.in_train_phase(K.dropout(x, self.rate), x)
        return x