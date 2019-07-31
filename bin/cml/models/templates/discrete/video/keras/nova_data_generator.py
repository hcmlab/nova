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
import cv2
import asyncio
import time




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
        self.current_batch_step_shuffle = mp.Value('i', -1)
        self.__step_lock = mp.Lock()
        self.sessions_file = os.path.dirname(os.path.realpath(__file__)) + "\\nova_sessions"
        self.db_info_file = os.path.dirname(os.path.realpath(__file__)) + "\\nova_db_info"
        self.file_readers = []
        self.annos = []
        self.annos_cont = []
        self.get_training_data()
        self.annos_to_continuous()
        
        if self.shuffle:
            sample_rate = self.file_readers[0].get(cv2.CAP_PROP_FPS)
            #self.total_frames = sum([int(reader._meta['nframes']) for reader in self.file_readers])
            if (self.cmlbegintime != sys.maxsize):
                self.total_frames = int(self.cmlbegintime * sample_rate)
            else:
                self.total_frames = sum([int(reader.get(cv2.CAP_PROP_FRAME_COUNT)) for reader in self.file_readers])
            self.indices = np.zeros((self.total_frames, 2))
            self.indices_shuffled = np.arange(self.total_frames)
            np.random.shuffle(self.indices_shuffled)
            count = 0
            for i in range(0, len(self.file_readers)):
                #for j in range(0, self.file_readers[i]._meta['nframes']):
                for j in range(0, min(int(self.file_readers[i].get(cv2.CAP_PROP_FRAME_COUNT)), int(self.cmlbegintime * sample_rate))):
                    self.indices[count][0] = i
                    self.indices[count][1] = j
                    count+=1
        self.on_epoch_end()
        self.__step_locks = [mp.Lock() for x in range(len(self.file_readers))]

    def annos_to_continuous(self):
        anno_cont = []

        for i in range(0, self.annos.__len__()):
            header, data = self.annos[i]
            xmlreader = minidom.parseString(header)
            numclasses = len(xmlreader.getElementsByTagName("item"))
            self.n_classes = numclasses
            #sample_rate = self.file_readers[i]._meta["fps"]
            #anno_cont = np.full( min(self.file_readers[i]._meta['nframes'], int(self.cmlbegintime * sample_rate)), numclasses-1)
            sample_rate = self.file_readers[i].get(cv2.CAP_PROP_FPS)
            anno_cont = np.full( min(int(self.file_readers[i].get(cv2.CAP_PROP_FRAME_COUNT)), int(self.cmlbegintime * sample_rate)), numclasses-1)


            for label in data[0]['labels']:
                start = int(label["from"]*sample_rate)
                #if (label["to"]*sample_rate) < min(self.file_readers[i]._meta['nframes'], int(self.cmlbegintime * sample_rate)):
                if (label["to"]*sample_rate) < min(int(self.file_readers[i].get(cv2.CAP_PROP_FRAME_COUNT)), int(self.cmlbegintime * sample_rate)):
                    end = int((label["to"] * sample_rate))
                else:
                    #end = min(self.file_readers[i]._meta['nframes'], int(self.cmlbegintime * sample_rate))
                    end = min(int(self.file_readers[i].get(cv2.CAP_PROP_FRAME_COUNT)), int(self.cmlbegintime * sample_rate))
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
                if len(line) > 1:
                    multi_corpus = line.split(":")

                    corpus = multi_corpus[0]
                    annotator = multi_corpus[1]
                    roles = multi_corpus[2].split(";")
                    stream = multi_corpus[3]
                    sessions = multi_corpus[4].strip().split(";")

                    for s in sessions:
                        for r in roles:
                            filename = r + "." + stream
                            annoToAdd = db.get_anno_by_session(db_info, corpus, s, annotator, r)
                            if annoToAdd != None:
                                self.annos.append(annoToAdd)
                                filepath = os.path.join(db_info['root'].strip(), corpus, s, filename)
                                #self.file_readers.append(imageio.get_reader(filepath, 'ffmpeg'))
                                self.file_readers.append(cv2.VideoCapture(filepath))
                                print("Loading: " + corpus + ":" + s + ' '+ filename)
                            else:
                                print("Skipping: " + corpus + ":" + s + ' '+ r + "." + filename)

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
        self.current_batch_step_shuffle.value = 0
        if self.shuffle:
            np.random.shuffle(self.indices_shuffled)
        


    def validate_batch(self):
        # check if we have enough data left in the current file for this batch
        #print(self.file_readers[self.current_file_reader.value]._meta['nframes'])
        #batch_is_valid = min(self.file_readers[self.current_file_reader.value]._meta['nframes'], self.cmlbegintime * self.file_readers[self.current_file_reader.value]._meta["fps"]) >= self.batch_size * (self.current_batch_step.value + 1)
        batch_is_valid = min(self.file_readers[self.current_file_reader.value].get(cv2.CAP_PROP_FRAME_COUNT), self.cmlbegintime * self.file_readers[self.current_file_reader.value].get(cv2.CAP_PROP_FPS)) >= self.batch_size * (self.current_batch_step.value + 1)
        return batch_is_valid

    def get_next_batch_pair(self):
        # copying the current batch of data before the preprocessing starts
        self.__step_lock.acquire()
        current_batch_step_shuffle_value = self.current_batch_step_shuffle.value
        self.current_batch_step_shuffle.value += 1
        self.__step_lock.release()

        


        start_time = time.time()
        if self.shuffle:
            upperBound = (current_batch_step_shuffle_value + 1) * self.batch_size
            if upperBound > self.total_frames:
                upperBound = self.total_frames - 1

            #next_batch = [self.file_readers[int(self.indices[self.indices_shuffled[x]][0])].get_data(int(self.indices[self.indices_shuffled[x]][1]))
             #           for x
              #          in range(self.current_batch_step_shuffle.value * self.batch_size, upperBound)]

            next_batch = []
            next_anno_batch = []
           
            #for x in range(self.current_batch_step.value * self.batch_size, (self.current_batch_step.value + 1) * self.batch_size):
            for x in range(current_batch_step_shuffle_value * self.batch_size, upperBound):             
                fr = int(self.indices[self.indices_shuffled[x]][0])
                self.__step_locks[fr].acquire()

                self.file_readers[fr].set(cv2.CAP_PROP_POS_FRAMES, int(self.indices[self.indices_shuffled[x]][1]))
                next_batch.append(self.file_readers[fr].read()[1])
                next_anno_batch.append(self.annos_cont[fr][int(self.indices[self.indices_shuffled[x]][1])])

                self.__step_locks[fr].release()

            

        else:
            # load new file if necessary
            while not self.validate_batch():
                self.current_file_reader.value += 1
                self.current_batch_step.value = 0

            # copying the current batch of data
            self.__step_lock.acquire()
            next_batch = []
            for x in range(self.current_batch_step.value * self.batch_size, (self.current_batch_step.value + 1) * self.batch_size):
                self.file_readers[self.current_file_reader.value].set(cv2.CAP_PROP_POS_FRAMES, x)
                next_batch.append(self.file_readers[self.current_file_reader.value].read()[1])

            next_anno_batch = [self.annos_cont[self.current_file_reader.value][x]
                               for x
                               in range(self.current_batch_step.value * self.batch_size,
                                        (self.current_batch_step.value + 1) * self.batch_size)]
            self.__step_lock.release()

        #print('batches be like: {}'.format(time.time() - start_time))
        return np.asarray(next_batch), np.asarray(next_anno_batch)


    def __len__(self):
        n_batches = 0
        for i in range(0, self.file_readers.__len__()):
            #n_frames =  min(self.file_readers[i]._meta['nframes'], self.cmlbegintime *self.file_readers[i]._meta["fps"])
            n_frames =  min(self.file_readers[i].get(cv2.CAP_PROP_FRAME_COUNT), self.cmlbegintime *self.file_readers[i].get(cv2.CAP_PROP_FPS))
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
              'shuffle': True
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
    model.add(Dense(4, activation='softmax'))
    model.compile(optimizer='adam', loss='categorical_crossentropy', metrics=['accuracy'])

    # Train model on dataset
    model.fit_generator(generator=training_generator,
                        #validation_data=validation_generator,
                        verbose=1,
                        shuffle=True, # <- ignored ?
                        use_multiprocessing=False,
                        workers=10,
                        max_queue_size=50,
                        epochs=10)
