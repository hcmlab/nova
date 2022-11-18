import torch
import torchvision
import torchvision.transforms as transforms
from PIL import Image
import numpy as np

import os
from glob import glob
from tqdm import tqdm
import matplotlib.pyplot as plt
import imageio

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")

val_transform = transforms.Compose([transforms.ToTensor(),
                                    transforms.Normalize((0.485, 0.456, 0.406), (0.229, 0.224, 0.225))])


class DeepLabV3Plus(torch.nn.Module):
    def __init__(self, num_classes):
        super().__init__()
        self.backbone = torchvision.models.resnet50(pretrained=True)

        # this changes the down-sampling from 1/32 to 1/8
        self.backbone.layer3[0].conv1.stride = (1, 1)
        self.backbone.layer3[0].conv2.stride = (1, 1)
        self.backbone.layer4[0].conv1.stride = (1, 1)
        self.backbone.layer4[0].conv2.stride = (1, 1)
        self.backbone.layer3[0].downsample[0].stride = (1, 1)
        self.backbone.layer4[0].downsample[0].stride = (1, 1)

        self.atrous_conv1 = torch.nn.Conv2d(in_channels=2048, out_channels=512, kernel_size=(3, 3), dilation=6,
                                            padding=6, bias=False)
        self.atrous_conv2 = torch.nn.Conv2d(in_channels=2048, out_channels=512, kernel_size=(3, 3), dilation=12,
                                            padding=12, bias=False)
        self.atrous_conv3 = torch.nn.Conv2d(in_channels=2048, out_channels=512, kernel_size=(3, 3), dilation=18,
                                            padding=18, bias=False)
        self.atrous_conv4 = torch.nn.Conv2d(in_channels=2048, out_channels=512, kernel_size=(3, 3), dilation=24,
                                            padding=24, bias=False)

        self.up_sample = torch.nn.Upsample(size=(256, 512), mode='bilinear', align_corners=False)

        self.deconv1 = torch.nn.ConvTranspose2d(in_channels=512, out_channels=256, kernel_size=(4, 4), stride=2,
                                                padding=1)
        self.deconv2 = torch.nn.ConvTranspose2d(in_channels=512, out_channels=256, kernel_size=(4, 4), stride=2,
                                                padding=1)
        self.deconv3 = torch.nn.ConvTranspose2d(in_channels=320, out_channels=num_classes, kernel_size=(4, 4), stride=2,
                                                padding=1)
        self.to(device)

    def forward(self, x):

        xc1 = self.backbone.conv1(x)
        xbn1 = self.backbone.relu(self.backbone.bn1(xc1))
        xmp = self.backbone.maxpool(xbn1)
        xl1 = self.backbone.layer1(xmp)
        xl2 = self.backbone.layer2(xl1)
        xl3 = self.backbone.layer3(xl2)
        xl4 = self.backbone.layer4(xl3)
        x1 = self.atrous_conv1(xl4)
        x2 = self.atrous_conv2(xl4)
        x3 = self.atrous_conv3(xl4)
        x4 = self.atrous_conv4(xl4)

        x = x1 + x2 + x3 + x4
        xd1 = self.deconv1(x)
        xd1c = torch.cat(tensors=(xd1, xl1), dim=1)
        xd2 = self.deconv2(xd1c)
        xd2c = torch.cat(tensors=(xd2, xc1), dim=1)
        x = self.deconv3(xd2c)
        return x

    def save_weights(self, path):
        torch.save(self.state_dict(), path)

    def load_weights(self, path):
        if device == torch.device('cpu'):
            self.load_state_dict(torch.load(path, map_location=device))
        else:
            self.load_state_dict(torch.load(path))


def execute_test():
    img = np.ones(shape=(3, 256, 512))
    img = torch.from_numpy(img).to(device)
    img.unsqueeze_(0)
    img = img.float()

    print("Input shape:", img.shape)
    model = TransferLearningModel2(3)
    output = model(img)
    print("Output shape (must be 1×3×256×512):", output.shape)


if __name__ == '__main__':
    execute_test()
