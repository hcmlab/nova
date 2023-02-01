import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']


from pathlib import Path
from hcai_datasets.hcai_nova_dynamic.hcai_nova_dynamic_iterable import HcaiNovaDynamicIterable
from hcai_dataset_utils.bridge_pytorch import BridgePyTorch
from torchaudio import transforms as audio_transforms
from torchvision import transforms as vision_transforms
import torch
import time
import whisper
import numpy as np
import logging

DEPENDENCIES = []
OPTIONS = {'model': 'large'}


def preprocess(data_iterator, logger=None, request_form=None):

    # Get all audio tracks
    audio_tracks = list(filter(lambda x: 'audio' in x, data_iterator.data_info.keys()))

    # To pytorch dataset
    torch_ds = BridgePyTorch(data_iterator)

    # Resampling
    for at in audio_tracks:
        torch_ds.apply_transform(
            at,
            vision_transforms.Compose([
                vision_transforms.ToTensor(),
                audio_transforms.Resample(data_iterator.data_info[at].sr, 16000),
                vision_transforms.Lambda(lambda x: torch.squeeze(x))
            ])
        )

    return torch_ds


def train (X, Y, logger=None, request_form=None):
    print('Training not supported')
    return None


def predict(model, X, logger=None, request_form=None):

    transcript_dict = {}

    for sample in X:
        
        frame = sample['frame']
        for at in list(filter(lambda x: 'audio' in x, sample.keys())):
            
            # needs to be np array, float 32 mono
            #audio = np.squeeze(sample[at])

            # load audio and pad/trim it to fit 30 seconds
            audio = whisper.pad_or_trim(sample[at])

            # make log-Mel spectrogram and move to the same device as the model
            mel = whisper.log_mel_spectrogram(audio).to(model.device)

            # detect the spoken language
            _, probs = model.detect_language(mel)
            #print(f"Detected language: {max(probs, key=probs.get)}")

            # decode the audio
            options = whisper.DecodingOptions()
            result = whisper.decode(model, mel, options)

            if logger:
                logger.info(f"{frame} - {result.text}")

            if frame not in transcript_dict.keys():
                transcript_dict[frame] = {}
            
            transcript_dict[frame][at] = {
                'conf': 1 - result.no_speech_prob,
                'name': result.text
            }

    # print the recognized text
    return transcript_dict


def save (model, path, logger=None, request_form=None):
   print('Save not supported')

def load(path, classes=None, logger=None, request_form=None):
    model = whisper.load_model(OPTIONS['model'])
    return model