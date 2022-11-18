import torch
import torch.nn as nn
import torchvision

import os
import numpy as np
from glob import glob
from PIL import Image

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")


class ResNet_18_ASPP(nn.Module):

    def __init__(self, num_classes):
        super().__init__()
        in_channels = 512
        out_channels = 256
        dilation = [4, 8, 12, 16]

        self.backbone = torchvision.models.resnet18(pretrained=True)
        self.backbone.layer3._modules['0'].conv1.stride = (1, 1)
        self.backbone.layer3._modules['0'].downsample._modules['0'].stride = (1, 1)
        self.backbone.layer4._modules['0'].conv1.stride = (1, 1)
        self.backbone.layer4._modules['0' ].downsample._modules['0'].stride = (1, 1)

        self.atrous_conv1 = nn.Conv2d(in_channels, out_channels, kernel_size=(3, 3), padding=dilation[0],
                                      dilation=(dilation[0], dilation[0]), bias=False)
        self.atrous_conv2 = nn.Conv2d(in_channels, out_channels, kernel_size=(3, 3), padding=dilation[1],
                                      dilation=(dilation[1], dilation[1]), bias=False)
        self.atrous_conv3 = nn.Conv2d(in_channels, out_channels, kernel_size=(3, 3), padding=dilation[2],
                                      dilation=(dilation[2], dilation[2]), bias=False)
        self.atrous_conv4 = nn.Conv2d(in_channels, out_channels, kernel_size=(3, 3), padding=dilation[3],
                                      dilation=(dilation[3], dilation[3]), bias=False)

        self.conv_final = nn.Conv2d(in_channels=out_channels, out_channels=num_classes, kernel_size=(1, 1))

        input_size = (out_channels, in_channels)
        self.upsampling = nn.UpsamplingBilinear2d(size=input_size)

        self.relu = nn.ReLU()
        self.to(device)

    def forward(self, x):
        x = self.backbone.conv1(x)
        x = self.backbone.relu(self.backbone.bn1(x))
        x = self.backbone.maxpool(x)
        x = self.backbone.layer1(x)
        x = self.backbone.layer2(x)
        x = self.backbone.layer3(x)
        x = self.backbone.layer4(x)
        atrous_conv1 = self.relu(self.atrous_conv1(x))
        atrous_conv2 = self.relu(self.atrous_conv2(x))
        atrous_conv3 = self.relu(self.atrous_conv3(x))
        atrous_conv4 = self.relu(self.atrous_conv4(x))
        x = torch.sum(torch.stack([atrous_conv1, atrous_conv2, atrous_conv3, atrous_conv4]), dim=0)
        x = self.conv_final(x)
        x = self.upsampling(x)

        return x

    def save_weights(self, path):
        torch.save(self.state_dict(), path)

    def load_weights(self, path):
        if device == torch.device('cpu'):
            self.load_state_dict(torch.load(path, map_location=device))
        else:
            self.load_state_dict(torch.load(path))



def execute_test():
    print("Exercise 1 d) (test) started!")
    img = np.ones(shape=(3, 256, 512))
    img = torch.from_numpy(img).to(device)
    img.unsqueeze_(0)
    img = img.float()

    print("Input shape:", img.shape)
    model = ResNet_18_ASPP(3)
    output = model(img)
    print("Output shape (must be 1×3×256×512):", output.shape)


if __name__ == '__main__':
    execute_test()
