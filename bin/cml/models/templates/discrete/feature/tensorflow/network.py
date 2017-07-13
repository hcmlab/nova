import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']
import tensorflow as tf


def variable_summaries(var):

    with tf.name_scope('summaries'):
        mean = tf.reduce_mean(var)
        tf.summary.scalar('mean', mean)
        stddev = tf.sqrt(tf.reduce_mean(tf.square(var - mean)))
        tf.summary.scalar('stddev', stddev)
        tf.summary.scalar('max', tf.reduce_max(var))
        tf.summary.scalar('min', tf.reduce_min(var))
        tf.summary.histogram('histogram', var)


def getModel (n_input, n_output):

    with tf.name_scope('input/'):
        x = tf.placeholder(tf.float32, [None, n_input], name='x')
    with tf.name_scope('W'):
        W = tf.Variable(tf.zeros([n_input, n_output]))
        variable_summaries(W)
    with tf.name_scope('b'):
        b = tf.Variable(tf.zeros([n_output]))
        variable_summaries(b)
    with tf.name_scope('Wx_plus_b'):
        y = tf.matmul(x, W) + b
        tf.summary.histogram('activations', y)

    p_drop = tf.placeholder(tf.float32)

    return (x,y,p_drop)


def getTrainer (n_output, y):

    with tf.name_scope('input/'):
        y_ = tf.placeholder(tf.float32, [None, n_output], name='y')
    
    with tf.name_scope('entropy'):
        cross_entropy = tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(labels=y_, logits=y))
        tf.summary.scalar('entropy', cross_entropy)
                    
    with tf.name_scope('train'):
        train_step = tf.train.GradientDescentOptimizer(0.5).minimize(cross_entropy)  

    return (train_step, y_)