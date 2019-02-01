import sys
import importlib
if not hasattr(sys, 'argv'):
	sys.argv  = ['']
import tensorflow as tf
import numpy as np
import random
import datetime
from xml.dom import minidom
import os
import shutil
import time
import site


def getModelType(types, opts, vars):
    return types.CLASSIFICATION


def getOptions(opts, vars):

    vars['x'] = None
    vars['y'] = None
    vars['sess'] = None
    opts['network'] = ''
    opts['p_drop'] = 0.5
    opts['batch_size'] = 1
    opts['n_epoch'] = 1000
    opts['n_log'] = 10
    opts['n_log_full'] = 100    


def convert_to_one_hot(label, n_classes):

    float_label = int(label)
    one_hot = np.zeros(n_classes)
    one_hot[label] = 1.0

    return one_hot


def getBatch(n, x, y):

    indices = np.random.choice(len(x),n,False)

    x_ = x[indices,]
    y_ = y[indices]

    return (x_, y_)


def train(data, label, opts, vars):

    n_input = data[0].dim
    n_output = max(label)+1

    print ('load network architecture from ' + opts['network'] + '.py')
    module = __import__(opts['network'])
    x, y, p_drop = module.getModel(n_input,n_output) 
    train_step, y_ = module.getTrainer(n_output,y)

    sess = tf.InteractiveSession()

    merged = tf.summary.merge_all()
    st = datetime.datetime.now().strftime('%Y-%m-%d_%H-%M-%S')
    train_writer = tf.summary.FileWriter('./logs/' + opts['network'] + '/' + st + '/train', sess.graph)
    test_writer = tf.summary.FileWriter('./logs/' + opts['network'] + '/' + st + '/test')
    tf.global_variables_initializer().run()

    with tf.name_scope('prediction'):
        correct_prediction = tf.equal(tf.argmax(y, 1), tf.argmax(y_, 1))        
        accuracy = tf.reduce_mean(tf.cast(correct_prediction, tf.float32))
        summary_accuracy = tf.summary.scalar('accuracy', accuracy)

    train_x = np.empty((len(data), n_input))
    train_y = np.empty((len(label), n_output))

    for sample in range(len(data)):
        train_x[sample] = np.reshape(data[sample],(n_input))
        train_y[sample] = convert_to_one_hot(label[sample], n_output)

    train_x = train_x.astype('float32')  
    for i in range(opts['n_epoch']):
        batch_x, batch_y = getBatch(opts['batch_size'], train_x, train_y)
        feed_dict = {x: batch_x, y_: batch_y, p_drop: opts['p_drop']}
        _ = sess.run(train_step, feed_dict=feed_dict)

    vars['x'] = x
    vars['y'] = y
    vars['p_drop'] = p_drop
    vars['sess'] = sess
    vars['n_input'] = n_input
    vars['n_output'] = n_output


def forward(data, probs, opts, vars):

    n_input = data.dim    

    sess = vars['sess']
    x = vars['x']
    y = vars['y']
    p_drop = vars['p_drop']

    np_array_x = np.asarray(data)
    np_array_x = np_array_x.astype('float32')
    np_array_x = np.reshape(np_array_x, (n_input))

    pred = sess.run(y, {x: [np_array_x], p_drop: 0})
    for i in range (len(pred[0])):
        probs[i] = float(pred[0][i])

    return max(probs)


def save(path, opts, vars):

    sess = vars['sess']

    print('save model to ' + path)

    # save model weights

    saver = tf.train.Saver()
    saver.save(sess, path)

    # copy scripts

    srcDir = os.path.dirname(os.path.realpath(__file__)) + '\\'
    dstDir = os.path.dirname(path) + '\\'

    print('copy scripts from \'' + srcDir + '\' to \'' + dstDir + '\'')

    if not os.path.exists(dstDir):
        os.makedirs(dstDir)

    srcFiles = os.listdir(srcDir)
    for fileName in srcFiles:
        fullFileName = os.path.join(srcDir, fileName)
        if os.path.isfile(fullFileName) and fileName.endswith('.py'):
            shutil.copy(fullFileName, dstDir)
    
    network_new =  os.path.basename(path) + '.' + opts['network']
    shutil.copy(dstDir + opts['network'] + '.py', dstDir + network_new + '.py')
    #os.rename(dstDir + opts['network'] + '.py', dstDir + network_new + '.py')    
    #shutil.move(dstDir + opts['network'] + '.py', dstDir + network_new + '.py') 


def load(path, opts, vars):
	
    print ('load model from ' + path)

    # parse input/output dimensions

    trainerPath = path
    trainerPath = trainerPath.replace("trainer.PythonModel.model", "trainer")	
    xmldoc = minidom.parse(trainerPath)
    streams = xmldoc.getElementsByTagName('streams')
    streamItem = streams[0].getElementsByTagName('item')
    n_input = streamItem[0].attributes['dim'].value
    print("#input: " + str(n_input))
    	
    classes = xmldoc.getElementsByTagName('classes')
    n_output = len(classes[0].getElementsByTagName('item'))
    print("#output: " + str(n_output))
   
    # copy unique network file
    
    network_tmp = os.path.dirname(path) + '\\' + opts['network'] + '.py'
    shutil.copy(path + '.' + opts['network'] + '.py', network_tmp)    

    # reload sys path and import network file

    importlib.reload(site)
    module = __import__(opts['network'])
    os.remove(network_tmp)

    # load model weights

    [x,y,p_drop] = module.getModel(n_input,n_output)
    sess = tf.InteractiveSession()
    saver = tf.train.Saver()    
    saver.restore(sess, path)

    # store variables

    vars['x'] = x
    vars['y'] = y
    vars['sess'] = sess
    vars['p_drop'] = p_drop

