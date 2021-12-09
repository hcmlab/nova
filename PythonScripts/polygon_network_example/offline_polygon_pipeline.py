import os
import argparse
import sys
import random
from tensorflow import keras

from polygon_preprocessor import PolygonPreprocessor, create_dir_if_not_exist
from dataIteratorHelper import Iterator
from UNetLikeNetwork import UNetLikeNetwork

main_dir = os.path.join(os.getcwd(), "resources")


def process_parameters():
    parser = argparse.ArgumentParser()
    parser.add_argument("-sl", "--seg_location", required=False, help="Specifies whether to work with a local "
                                                                      "segmentation file or with the database. Possible"
                                                                      " parameters are therefore: 'local' and 'db'. "
                                                                      "Type: string - default: 'local'")

    parser.add_argument("-ad", "--annotation_dir", required=False, help="If seg_location is set to 'local', "
                                                                        "annotation_dir can be used to set the "
                                                                        "directory where the annotation files and the "
                                                                        "video are located. Type: string - default: "
                                                                        "' " + main_dir + "'")
    parser.add_argument("-de", "--delete_environment", required=False, help="Specifies whether the existing "
                                                                            "environment should be deleted or not. "
                                                                            "Possible parameters are therefore: 0 (no) "
                                                                            "and 1 (yes). Type: string - default: 0")

    return vars(parser.parse_args())


def print_environment_infos():
    print("Make sure that the following files are in the preprocessing directory ('" +
          main_dir + "'): ")
    print("1. The video")
    print("2. The annotation file")
    print("3. The annotation tilde file")
    given_input = input("Press Enter to continue or type 'exit' to close the program... ")

    if given_input == "exit":
        sys.exit(0)


if __name__ == '__main__':
    segmentation_location = "local"
    delete_existing_env = 0
    args = process_parameters()

    if args["seg_location"] is not None:
        if args["seg_location"] == "local" or args["seg_location"] == "db":
            segmentation_location = args["seg_location"]
        else:
            raise Exception("Received a wrong parameter for the segmentation location! Must be 'local' or 'db'!")

    if segmentation_location == "local":
        if args["annotation_dir"] is not None:
            main_dir = args["annotation_dir"]
            if not os.path.exists(main_dir):
                raise Exception("The given annotation directory (annotation_dir) doesn't exist!")
        else:
            if not os.path.exists(main_dir):
                os.makedirs(main_dir)

        print("The local file system is used for training/prediction.")
        print_environment_infos()

        preprocessor = PolygonPreprocessor(main_dir)

        while not preprocessor.env_is_ready():
            print()
            print_environment_infos()

        if args['delete_environment'] is not None:
            delete_existing_env = int(args['delete_environment'])

        if delete_existing_env == 0:
            preprocessor.expand_environment()
            preprocessor.create_images()
            #preprocessor.move_items_to_validation_set()

        batch_size = 32
        num_classes = len(preprocessor.items)
        img_size = (300, 300)

        val_samples = 20
        input_img_paths = sorted(
            [
                os.path.join(preprocessor.train_images_dir, fname)
                for fname in os.listdir(preprocessor.train_images_dir)
                if fname.endswith(".jpg")
            ]
        )
        target_img_paths = sorted(
            [
                os.path.join(preprocessor.train_segmentation_dir, fname)
                for fname in os.listdir(preprocessor.train_segmentation_dir)
                if fname.endswith(".png") and not fname.startswith(".")
            ]
        )

        random.Random(1337).shuffle(input_img_paths)
        random.Random(1337).shuffle(target_img_paths)
        train_input_img_paths = input_img_paths[:-val_samples]
        train_target_img_paths = target_img_paths[:-val_samples]
        val_input_img_paths = input_img_paths[-val_samples:]
        val_target_img_paths = target_img_paths[-val_samples:]

        # Instantiate data Sequences for each split
        train_gen = Iterator( batch_size, img_size, train_input_img_paths, train_target_img_paths)
        val_gen = Iterator(batch_size, img_size, val_input_img_paths, val_target_img_paths)

        model_weights_dir = os.path.join(preprocessor.working_dir, 'model_weights')
        create_dir_if_not_exist(model_weights_dir)
        model_weights_path = os.path.join(model_weights_dir, 'polygon_example_segmentation.h5')

        network = UNetLikeNetwork(img_size, num_classes)
        model = network.get_model()
        model.compile(optimizer="rmsprop", loss="sparse_categorical_crossentropy")

        callbacks = [
            keras.callbacks.ModelCheckpoint(model_weights_path, save_best_only=True)
        ]

        # Train the model, doing validation at the end of each epoch.
        epochs = 15
        model.fit(train_gen, epochs=epochs, validation_data=val_gen, callbacks=callbacks)
    else:
        # TODO handle preprocessing for database
        pass

    print("\nSCRIPT DONE!")
