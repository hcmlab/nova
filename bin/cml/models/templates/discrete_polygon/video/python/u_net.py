import torch
import torch.nn as nn

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")


def double_conv(input_channels, output_channels):
    conv = nn.Sequential(
        nn.Conv2d(input_channels, output_channels, kernel_size=(3, 3), padding=1),
        nn.ReLU(inplace=True),
        nn.Conv2d(output_channels, output_channels, kernel_size=(3, 3), padding=1),
        nn.ReLU(inplace=True)
    )

    return conv


def crop_image(tensor, target_tensor):
    target_size_h = target_tensor.size()[2]
    tensor_size_h = tensor.size()[2]
    target_size_w = target_tensor.size()[3]
    tensor_size_w = tensor.size()[3]

    delta_h = tensor_size_h - target_size_h
    delta_w = tensor_size_w - target_size_w
    h_to_add = 0
    w_to_add = 0

    if delta_h % 2 != 0:
        h_to_add = -1
    if delta_w % 2 != 0:
        w_to_add = -1

    delta_h = delta_h // 2
    delta_w = delta_w // 2
    return tensor[:, :, delta_h:tensor_size_h - delta_h + h_to_add, delta_w:tensor_size_w - delta_w + w_to_add]


class UNet(nn.Module):
    def __init__(self, classes):
        super(UNet, self).__init__()
        # Encoder
        self.max_pool_2x2 = nn.MaxPool2d(kernel_size=(2, 2), stride=2)
        self.down_conv_1 = double_conv(input_channels=3, output_channels=16)
        self.down_conv_2 = double_conv(input_channels=16, output_channels=32)
        self.down_conv_3 = double_conv(input_channels=32, output_channels=64)
        self.down_conv_4 = double_conv(input_channels=64, output_channels=128)
        self.down_conv_5 = double_conv(input_channels=128, output_channels=256)

        # Decoder
        self.up_trans_1 = nn.ConvTranspose2d(in_channels=256, out_channels=128, kernel_size=(2, 2), stride=(2, 2))
        self.up_conv_1 = double_conv(256, 128)
        self.up_trans_2 = nn.ConvTranspose2d(in_channels=128, out_channels=64, kernel_size=(2, 2), stride=(2, 2))
        self.up_conv_2 = double_conv(128, 64)
        self.up_trans_3 = nn.ConvTranspose2d(in_channels=64, out_channels=32, kernel_size=(2, 2), stride=(2, 2))
        self.up_conv_3 = double_conv(64, 32)
        self.up_trans_4 = nn.ConvTranspose2d(in_channels=32, out_channels=16, kernel_size=(2, 2), stride=(2, 2))
        self.up_conv_4 = double_conv(32, 16)

        self.out = nn.Conv2d(in_channels=16, out_channels=classes, kernel_size=(1, 1))

    def forward(self, input_image):
        # Encoder part - with expected shape: batch size, channel, height, width
        x1 = self.down_conv_1(input_image)
        x2 = self.max_pool_2x2(x1)
        x3 = self.down_conv_2(x2)
        x4 = self.max_pool_2x2(x3)
        x5 = self.down_conv_3(x4)
        x6 = self.max_pool_2x2(x5)
        x7 = self.down_conv_4(x6)
        x8 = self.max_pool_2x2(x7)
        x9 = self.down_conv_5(x8)

        # Decoder part
        x = self.up_trans_1(x9)
        y = crop_image(x7, x)
        x = self.up_conv_1(torch.cat([x, y], 1))
        x = self.up_trans_2(x)
        y = crop_image(x5, x)
        x = self.up_conv_2(torch.cat([x, y], 1))
        x = self.up_trans_3(x)
        y = crop_image(x3, x)
        x = self.up_conv_3(torch.cat([x, y], 1))
        x = self.up_trans_4(x)
        y = crop_image(x1, x)
        x = self.up_conv_4(torch.cat([x, y], 1))

        x = self.out(x)
        return x

    def save_weights(self, path):
        torch.save(self.state_dict(), path)

    def load_weights(self, path):
        if device == torch.device('cpu'):
            self.load_state_dict(torch.load(path, map_location=device))
        else:
            self.load_state_dict(torch.load(path))


if __name__ == "__main__":
    print("\nUNet test started!\n")
    image = torch.rand((1, 3, 128, 128))
    model = UNet(classes=2)
    print(model(image).size())
    print("\nUNet test done!\n")
