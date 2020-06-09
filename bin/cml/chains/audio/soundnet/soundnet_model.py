# TensorFlow version of NIPS2016 soundnet

import sys
import numpy as np
import tensorflow as tf
from soundnet_model_ops import batch_norm, conv2d, relu, maxpool
tf.compat.v1.disable_eager_execution()
# Make xrange compatible in both Python 2, 3
try:
    xrange
except NameError:
    xrange = range

local_config = {  
            'batch_size': 1, 
            'eps': 1e-5,
            'name_scope': 'SoundNet',
            }

class Model():
    def __init__(self, session, config=local_config, param_G=None):
        # Print config
        for key in config: print("{}:{}".format(key, config[key]))

        self.sess           = session
        self.config         = config
        self.param_G        = param_G
        
        # Placeholder
        self.add_placeholders()
        
        # Generator
        self.add_generator(name_scope=self.config['name_scope'])        


    def add_placeholders(self):
        self.sound_input_placeholder = tf.compat.v1.placeholder(tf.float32,
                shape=[self.config['batch_size'], None, 1, 1]) # batch x h x w x channel


    def add_generator(self, name_scope='SoundNet'):
        with tf.compat.v1.variable_scope(name_scope) as scope:
            self.layers = {}
            
            # Stream one: conv1 ~ conv7
            self.layers[1] = conv2d(self.sound_input_placeholder, 1, 16, F_h=64, S_h=2, P_h=32, name_scope='conv1')
            self.layers[2] = batch_norm(self.layers[1], 16, self.config['eps'], name_scope='conv1')
            self.layers[3] = relu(self.layers[2], name_scope='conv1')
            self.layers[4] = maxpool(self.layers[3], F_h=8, S_h=8, name_scope='conv1')

            self.layers[5] = conv2d(self.layers[4], 16, 32, F_h=32, S_h=2, P_h=16, name_scope='conv2')
            self.layers[6] = batch_norm(self.layers[5], 32, self.config['eps'], name_scope='conv2')
            self.layers[7] = relu(self.layers[6], name_scope='conv2')
            self.layers[8] = maxpool(self.layers[7], F_h=8, S_h=8, name_scope='conv2')

            self.layers[9] = conv2d(self.layers[8], 32, 64, F_h=16, S_h=2, P_h=8, name_scope='conv3')
            self.layers[10] = batch_norm(self.layers[9], 64, self.config['eps'], name_scope='conv3')
            self.layers[11] = relu(self.layers[10], name_scope='conv3')

            self.layers[12] = conv2d(self.layers[11], 64, 128, F_h=8, S_h=2, P_h=4, name_scope='conv4')
            self.layers[13] = batch_norm(self.layers[12], 128, self.config['eps'], name_scope='conv4')
            self.layers[14] = relu(self.layers[13], name_scope='conv4')

            self.layers[15] = conv2d(self.layers[14], 128, 256, F_h=4, S_h=2, P_h=2, name_scope='conv5')
            self.layers[16] = batch_norm(self.layers[15], 256, self.config['eps'], name_scope='conv5')
            self.layers[17] = relu(self.layers[16], name_scope='conv5')
            self.layers[18] = maxpool(self.layers[17], F_h=4, S_h=4, name_scope='conv5')

            self.layers[19] = conv2d(self.layers[18], 256, 512, F_h=4, S_h=2, P_h=2, name_scope='conv6')
            self.layers[20] = batch_norm(self.layers[19], 512, self.config['eps'], name_scope='conv6')
            self.layers[21] = relu(self.layers[20], name_scope='conv6')

            self.layers[22] = conv2d(self.layers[21], 512, 1024, F_h=4, S_h=2, P_h=2, name_scope='conv7')
            self.layers[23] = batch_norm(self.layers[22], 1024, self.config['eps'], name_scope='conv7')
            self.layers[24] = relu(self.layers[23], name_scope='conv7')

            # Split one: conv8, conv8_2
            self.layers[25] = conv2d(self.layers[24], 1024, 1000, F_h=8, S_h=2, name_scope='conv8')
            self.layers[26] = conv2d(self.layers[24], 1024, 401, F_h=8, S_h=2, name_scope='conv8_2')


    def load(self):
        if self.param_G is None: return False
        data_dict = self.param_G
        for key in data_dict:
            with  tf.compat.v1.variable_scope(self.config['name_scope'] + '/' + key, reuse=True):
                for subkey in data_dict[key]:
                    try:
                        var =  tf.compat.v1.get_variable(subkey)
                        self.sess.run(var.assign(data_dict[key][subkey]))
                        #print('Assign pretrain model {} to {}'.format(subkey, key))
                    except:
                        print('Ignore {}'.format(key))
        self.param_G.clear()
        return True


if __name__ == '__main__':
    
    layer_min = int(sys.argv[1])
    layer_max = int(sys.argv[2]) if len(sys.argv) > 2 else layer_min + 1
    
    # Load pre-trained model
    G_name = './models/sound8.npy'
    param_G = np.load(G_name, encoding='latin1').item()
    dump_path = './output/'

    with tf.Session() as session:
        # Build model
        model = Model(session, config=local_config, param_G=param_G)
        init =  tf.compat.v1.global_variables_initializer()
        session.run(init)
        
        model.load()
        
        # Demo
        sound_input = np.reshape(np.load('data/demo.npy', encoding='latin1'), [local_config['batch_size'], -1, 1, 1])
        feed_dict = {model.sound_input_placeholder: sound_input}
        
        # Forward
        for idx in xrange(layer_min, layer_max):
            feature = session.run(model.layers[idx], feed_dict=feed_dict)
            np.save(dump_path + 'tf_fea{}.npy'.format(str(idx).zfill(2)), np.squeeze(feature))
            print("Save layer {} with shape {} as {}tf_fea{}.npy".format(idx, np.squeeze(feature).shape, dump_path, str(idx).zfill(2)))

