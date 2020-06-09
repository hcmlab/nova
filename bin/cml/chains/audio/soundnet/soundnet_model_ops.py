# TensorFlow version of NIPS2016 soundnet
import tensorflow as tf
# input         input layer
# depth_in      depth of input layer
# depth_out     depth of output layer (=number of filter)
# F_h           receptive field height (=filter height)
# F_w           receptive field width (=filter width)
# S_h           vertical stride with which we slide the filter
# S_w           horizontal stride with which we slide the filter
# P_h           vertical zero padding
# P_w           horizonal zero padding
# pad           set 'VALID' apply padding
# name_scope    scope name
#
# returns output layer
def conv2d(input, depth_in, depth_out, F_h=1, F_w=1, S_h=1, S_w=1, P_h=0, P_w=0, pad='VALID', name_scope='conv'):
    
    with tf.compat.v1.variable_scope(name_scope) as scope:

        try:
            w_conv = tf.compat.v1.get_variable('weights', [F_h, F_w, depth_in, depth_out], 
                    initializer=tf.compat.v1.truncated_normal_initializer(0.0, stddev=0.01))
        except ValueError:
            scope.reuse_variables()
            w_conv = tf.compat.v1.get_variable('weights')

        b_conv = tf.compat.v1.get_variable('biases', [depth_out], 
                initializer=tf.compat.v1.constant_initializer(0.0))
        
        padded_input = tf.pad(input, [[0, 0], [P_h, P_h], [P_w, P_w], [0, 0]], "CONSTANT") if pad == 'VALID' \
                else input

        output = tf.nn.conv2d(padded_input, w_conv, 
                [1, S_h, S_w, 1], padding=pad, name='z') + b_conv
    
        return output

# input         input layer
# depth_out     depth of output layer
# eps           epsilon
# name_scope    scope name
#
# returns output layer
def batch_norm(input, depth_out, eps, name_scope='conv'):

    with  tf.compat.v1.variable_scope(name_scope) as scope:

        try:
            mu_conv = tf.compat.v1.get_variable('mean', [depth_out], 
                initializer=tf.compat.v1.constant_initializer(0))
        except ValueError:
            scope.reuse_variables()
            mu_conv = tf.compat.v1.get_variable('mean')

        var_conv = tf.compat.v1.get_variable('var', [depth_out], 
            initializer=tf.compat.v1.constant_initializer(1))
        gamma_conv = tf.compat.v1.get_variable('gamma', [depth_out], 
            initializer=tf.compat.v1.constant_initializer(1))
        beta_conv = tf.compat.v1.get_variable('beta', [depth_out], 
            initializer=tf.compat.v1.constant_initializer(0))
        output = tf.compat.v1.nn.batch_normalization(input, mu_conv, 
            var_conv, beta_conv, gamma_conv, eps, name='batch_norm')
        
        return output

# input         input layer
# name_scope    scope name
#
# returns output layer
def relu(input, name_scope='conv'):
    with tf.compat.v1.variable_scope(name_scope) as scope:
        return tf.compat.v1.nn.relu(input, name='a')

# input         input layer
# F_h           receptive field height (=filter height)
# F_w           receptive field width (=filter width)
# S_h           vertical stride with which we slide the filter
# S_w           horizontal stride with which we slide the filter
# name_scope    scope name
#
# returns output layer
def maxpool(input, F_h=1, F_w=1, S_h=1, S_w=1, name_scope='conv'):
    with tf.compat.v1.variable_scope(name_scope) as scope:
        return tf.compat.v1.nn.max_pool(input, 
                [1, F_h, F_w, 1], [1, S_h, S_w, 1], padding='VALID', name='maxpool')
