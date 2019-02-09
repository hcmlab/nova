import sys
import numpy as np
import keras
import subprocess
import json
import os
import imageio
import multiprocessing as mp
from keras.models import Sequential
from keras.layers import Dense, Dropout, Flatten
from keras.layers import Conv2D, MaxPooling2D
#from ffmpy import FFprobe, FFRuntimeError
import db_handler as db
from xml.dom import minidom
from skimage.transform import resize




class DataGenerator(keras.utils.Sequence):

    def __init__(self, batch_size=32, dim=(300, 300), n_channels=3, n_classes=3, shuffle=True):
        'Initialization'
        self.dim = dim
        self.batch_size = batch_size
        self.n_channels = n_channels
        self.n_classes = n_classes
        self.shuffle = shuffle
        self.current_file_reader = mp.Value('i', 0)
        self.current_batch_step = mp.Value('i', 0)
        self.__step_lock = mp.Lock()
        self.on_epoch_end()
        self.sessions_file = os.path.dirname(os.path.realpath(__file__)) + "\\nova_sessions"
        self.db_info_file = os.path.dirname(os.path.realpath(__file__)) + "\\nova_db_info"
        self.file_readers = []
        self.annos = []
        self.annos_cont = []
        self.get_training_data()
        self.annos_to_continuous()
        self.cmlbegintime

    def annos_to_continuous(self):
        anno_cont = []

        for i in range(0, self.annos.__len__()):
            header, data = self.annos[i]
            xmlreader = minidom.parseString(header)
            numclasses = len(xmlreader.getElementsByTagName("item"))
            self.n_classes = numclasses
            sample_rate = self.file_readers[i]._meta["fps"]
            anno_cont = np.full( min(self.file_readers[i]._meta['nframes'], int(self.cmlbegintime * sample_rate)), numclasses-1)

            for label in data[0]['labels']:
                start = int(label["from"]*sample_rate)
                if (label["to"]*sample_rate) < min(self.file_readers[i]._meta['nframes'], int(self.cmlbegintime * sample_rate)):
                    end = int((label["to"] * sample_rate))
                else:
                    end = min(self.file_readers[i]._meta['nframes'], int(self.cmlbegintime * sample_rate))
                for k in range(start, end):
                    anno_cont[k] = label["id"]

            self.annos_cont.append(anno_cont)

    def get_training_data(self):

        db_info = {}

        with open(self.db_info_file, 'r') as f:
            entries = f.readline().split(";")
            for e in entries:
                entry = e.split("=")
                db_info[str(entry[0])] = str(entry[1])
            if(db_info['cooperative'] != None and db_info['cooperative'] == "True" and db_info['cmlbegintime'] != None and float(db_info['cmlbegintime']) > 0 ):
                print("CML Training performed until: " + str(db_info['cmlbegintime']))
                self.cmlbegintime = float(db_info['cmlbegintime']) 
            else:
                self.cmlbegintime = sys.maxsize
                print("Training performed on full dataset")
               

        with open(self.sessions_file, 'r') as f:
            for line in f:
                multi_corpus = line.split(":")

                corpus = multi_corpus[0]
                annotator = multi_corpus[1]
                roles = multi_corpus[2].split(";")
                stream = multi_corpus[3]
                sessions = multi_corpus[4].strip().split(";")

                for s in sessions:
                    for r in roles:
                        annoToAdd = db.get_anno_by_session(db_info, corpus, s, annotator, r)
                        if annoToAdd != None:
                            self.annos.append(annoToAdd)
                            filename = r + "." + stream
                            filepath = os.path.join(db_info['root'].strip(), corpus, s, filename)
                            self.file_readers.append(imageio.get_reader(filepath, 'ffmpeg'))
                            print("Loading: " + corpus + ":" + s + ' '+ filename)
                        else: print("Skipping: " + corpus + ":" + s + ' '+ r + "." + filename)   

        #uncomment for debugging.
        os.remove(self.sessions_file)
        os.remove(self.db_info_file)

    def on_epoch_end(self):
        #TODO Updates indexes after each epoch
        # self.indexes = np.arange(len(self.list_IDs))
        # if self.shuffle == True:
        #     np.random.shuffle(self.indexes)
        # resetting the current files
        self.current_file_reader.value = 0
        self.current_batch_step.value = 0


    def validate_batch(self):
        # check if we have enough data left in the current file for this batch
        #print(self.file_readers[self.current_file_reader.value]._meta['nframes'])
        batch_is_valid = min(self.file_readers[self.current_file_reader.value]._meta['nframes'], self.cmlbegintime * self.file_readers[self.current_file_reader.value]._meta["fps"]) >= self.batch_size * (self.current_batch_step.value + 1)
        return batch_is_valid

    def get_next_batch_pair(self):
        # copying the current batch of data before the preprocessing starts
        self.__step_lock.acquire()

        # load new file if necessary
        while not self.validate_batch():
            self.current_file_reader.value += 1
            self.current_batch_step.value = 0

        # copying the current batch of data
        next_batch = [self.file_readers[self.current_file_reader.value].get_data(x)
                      for x
                      in range(self.current_batch_step.value * self.batch_size, (self.current_batch_step.value + 1) * self.batch_size)]
        next_anno_batch = [self.annos_cont[self.current_file_reader.value][x]
                      for x
                      in range(self.current_batch_step.value * self.batch_size, (self.current_batch_step.value + 1) * self.batch_size)]
        self.current_batch_step.value += 1

        self.__step_lock.release()

        return np.asarray(next_batch), np.asarray(next_anno_batch)

    def __len__(self):
        n_batches = 0
        for i in range(0, self.file_readers.__len__()):
            n_frames =  min(self.file_readers[i]._meta['nframes'], self.cmlbegintime *self.file_readers[i]._meta["fps"])
            n_batches += int(n_frames) // self.batch_size
        return n_batches

    def __getitem__(self, index):
        xtemp = []
        x, y = self.get_next_batch_pair()

        for i in range(0, len(x)):
            xtemp.append(resize(x[i], (self.dim[1], self.dim[0])))

        y = keras.utils.to_categorical(y, num_classes=self.n_classes)
        X = np.asarray(xtemp)

        return X, y

if __name__ == '__main__':
    # Parameters

    width = 300
    height = 300
    n_channels = 3
    n_classes = 3
    batch_size = 32

    params = {'dim': (width, height),
              'batch_size': batch_size,
              'n_classes': n_classes,
              'n_channels': n_channels,
              'shuffle': False
              }

    # Generators
    training_generator = DataGenerator(**params)

    # Design model
    model = Sequential()
    model.add(Conv2D(32, kernel_size=(3, 3),
                     activation='relu',
                     input_shape=(height,width, n_channels)))
    model.add(MaxPooling2D(pool_size=(2, 2)))

    model.add(Flatten())
    model.add(Dense(3, activation='softmax'))
    model.compile(optimizer='sgd', loss='categorical_crossentropy')

    # Train model on dataset
    model.fit_generator(generator=training_generator,
                        #validation_data=validation_generator,
                        verbose=1,
                        shuffle=False,
                        use_multiprocessing=False,
                        workers=20,
                        max_queue_size=20,
                        epochs=3)
