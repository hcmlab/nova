import glob
import os
import shutil
import sys

import cv2
import random
import json
import numpy as np
from decimal import Decimal
from xml.dom import minidom
from termcolor import colored
from skimage.draw import polygon


def get_random_int(start, end):
    return random.randint(start, end)


def add_background_to_items(items):
    color = ""

    while color == "":
        color = '#FF%02X%02X%02X' % (get_random_int(0, 255), get_random_int(0, 255), get_random_int(0, 255))
        for item in items:
            if color == items[item]['color']:
                color = ""

    # Zero is background
    items[0] = {'name': 'Background', 'color': color}
    return items


def hex_to_rgb(color):
    color = color.replace('#', '')
    color = list(color)
    a, r, g, b = '', '', '', ''
    if len(color) == 6:
        a = "FF"
        r = str(color[0]) + str(color[1])
        g = str(color[2]) + str(color[3])
        b = str(color[4]) + str(color[5])
    elif len(color) == 8:
        a = str(color[0]) + str(color[1])
        r = str(color[2]) + str(color[3])
        g = str(color[4]) + str(color[5])
        b = str(color[7]) + str(color[7])

    return a, r, g, b


def set_background_color(ann_img, color):
    a, r, g, b = hex_to_rgb(color)
    for y in range(ann_img.shape[1]):
        for x in range(ann_img.shape[0]):
            ann_img[x, y] = (int(r, 16), int(g, 16), int(b, 16))

    return ann_img


def create_dir_if_not_exist(directory):
    if not os.path.exists(directory):
        os.makedirs(directory)


class PolygonPreprocessor:

    def __init__(self, main_dir):

        self.working_dir = os.getcwd()
        self.main_dir = main_dir
        self.video_path = ""
        self.captured_video = None
        self.annotation_path = ""
        self.annotation_tilde_path = ""

        self.dataset_dir = os.path.join(main_dir, "dataset")

        self.train_images_dir = os.path.join(self.dataset_dir, "train_images")
        self.train_segmentation_dir = os.path.join(self.dataset_dir, "train_segmentation")
        self.val_images_dir = os.path.join(self.dataset_dir, "val_images")
        self.val_segmentation_dir = os.path.join(self.dataset_dir, "val_segmentation")
        self.segmentation_images_train_dir = os.path.join(self.dataset_dir, "segmented_images_train")

        self.items = []
        self.sr = 0.0

    def env_is_ready(self):
        file_list = os.listdir(self.main_dir)

        annotation_counter = 0
        annotation_counter_tilde = 0
        else_counter = 0
        for filename in file_list:
            if not os.path.isfile(os.path.join(self.main_dir, filename)):
                continue

            if filename.endswith(".annotation"):
                annotation_counter += 1
                self.annotation_path = os.path.join(self.main_dir, filename)

            elif filename.endswith(".annotation~"):
                annotation_counter_tilde += 1
                self.annotation_tilde_path = os.path.join(self.main_dir, filename)

            else:
                else_counter += 1
                self.video_path = os.path.join(self.main_dir, filename)

        if annotation_counter + annotation_counter_tilde + else_counter > 3:
            print(colored("There are too many files in the directory! Expected 3 but " + str(len(file_list)) +
                          " were given!", 'red'))
            return False

        elif annotation_counter + annotation_counter_tilde + else_counter < 3:
            print(colored("There are too less files in the directory! Expected 3 but " + str(len(file_list)) +
                          " were given!", 'red'))
            return False

        if annotation_counter == 1 and annotation_counter_tilde == 1 and else_counter == 1:

            self.read_sample_rate()
            self.read_item_infos()
            self.items = add_background_to_items(self.items)
            return True
        else:
            print(colored("An error occurred! Make sure that only the '.annotation~', the '.annotation' and "
                          "the video file are in the mentioned folder (other folders are allowed)!", 'red'))
            return False

    def expand_environment(self):
        files = glob.glob(self.dataset_dir)
        for file in files:
            shutil.rmtree(file)

        create_dir_if_not_exist(self.dataset_dir)
        create_dir_if_not_exist(self.train_images_dir)
        create_dir_if_not_exist(self.train_segmentation_dir)
        create_dir_if_not_exist(self.val_images_dir)
        create_dir_if_not_exist(self.val_segmentation_dir)
        create_dir_if_not_exist(self.segmentation_images_train_dir)

    def read_sample_rate(self):
        xml_doc = minidom.parse(self.annotation_path)
        scheme_infos = xml_doc.getElementsByTagName('scheme')
        self.sr = float(scheme_infos[0].attributes['sr'].value)

    def create_images(self):
        self.captured_video = cv2.VideoCapture(self.video_path)
        frame_count = self.captured_video.get(cv2.CAP_PROP_FRAME_COUNT)
        fps = self.captured_video.get(cv2.CAP_PROP_FPS)
        duration = frame_count / fps
        if duration == 0:
            raise Exception("Duration of the given video file is zero!")

        point_of_time = 0
        e = abs(Decimal(str(self.sr)).as_tuple().exponent) + 2
        frame_rate = round(1 / self.sr, e)

        count = 1
        success = self.get_frame_from_time(point_of_time, count)
        while success:
            steps = int(100 * frame_rate * count / duration)
            sys.stdout.write("\r[%s%s] " % ('=' * steps, ' ' * (100 - steps)))
            done = round((100.0/duration)*(frame_rate * count), 2)
            if done > 100.00:
                done = 100.00
            sys.stdout.write(" %6s%%  " % (str(done)))
            sys.stdout.flush()

            count += 1
            point_of_time = round(point_of_time + frame_rate, e)
            success = self.get_frame_from_time(point_of_time, count)

    def get_frame_from_time(self, point_of_time, count):
        self.captured_video.set(cv2.CAP_PROP_POS_MSEC, point_of_time * 1000)
        frame_available, image = self.captured_video.read()
        if frame_available:
            cv2.imwrite(os.path.join(self.train_images_dir, "frame" + str(count) + ".jpg"), image)
            self.create_annotation_file(image, count)
        return frame_available

    def read_item_infos(self):
        xml_doc = minidom.parse(self.annotation_path)
        item_list = xml_doc.getElementsByTagName('item')
        items = {}

        for item in item_list:
            name_and_color = {'name': item.attributes['name'].value, 'color': item.attributes['color'].value}
            items[int(item.attributes['id'].value)] = name_and_color

        self.items = items

    def create_annotation_file(self, img, counter):
        dimensions = img.shape
        self.create_segmentation_images(dimensions, counter)

    def create_segmentation_images(self, dimensions, count):
        seg_img = np.zeros(dimensions).astype('uint8')
        seg_img = set_background_color(seg_img, self.items[0]['color'])

        ground_truth = np.zeros(dimensions).astype('uint8')

        with open(self.annotation_tilde_path) as file:
            data = json.load(file)

        frame = data['frame'][count-1]
        if 'polygons' in frame:
            for poly in frame['polygons']:
                color = self.items[int(poly['label'])]['color']
                a, r, g, b = hex_to_rgb(color)
                polygon_points = []

                if 'points' in poly:
                    for counter, point in enumerate(poly['points']):
                        polygon_points.append((point[1], point[0]))

                    polygon_points = np.array(polygon_points)
                    rr, cc = polygon(polygon_points[:, 0], polygon_points[:, 1], dimensions)
                    seg_img[rr, cc, 0] = int(r, 16)
                    seg_img[rr, cc, 1] = int(g, 16)
                    seg_img[rr, cc, 2] = int(b, 16)

                    ground_truth[rr, cc] = poly['label']

        cv2.imwrite(os.path.join(self.segmentation_images_train_dir, "frame" + str(count) + ".png"), seg_img)
        cv2.imwrite(os.path.join(self.train_segmentation_dir, "frame" + str(count) + ".png"), ground_truth)

    def move_items_to_validation_set(self):
        file_list = os.listdir(self.train_images_dir)
        ten_percent = int((len(file_list)/100)*10)
        mini_batch = random.sample(file_list, ten_percent)
        for item in mini_batch:
            shutil.move(os.path.join(self.train_images_dir, item), os.path.join(self.val_images_dir, item))
            shutil.move(os.path.join(self.train_segmentation_dir, item), os.path.join(self.val_segmentation_dir, item))
