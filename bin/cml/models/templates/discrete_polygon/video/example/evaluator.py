import torch

device = torch.device("cuda:0" if torch.cuda.is_available() else "cpu")


def get_intersection_and_union(prediction, ground_truth, all_results, labels_count):
    # This ensures that we do not use the predicted values when they should actually be ignored
    prediction = torch.where(ground_truth == 255, 255, prediction).to(device)
    ground_truth = ground_truth.to(device)

    # we ignore the 'background' label (0)
    for label in range(1, labels_count):
        # We create two 'bitmaps'
        # First: Every pixel where our model thought there is the class
        prediction_bitmap = torch.eq(prediction, label)
        # Second: The ground truth
        target_bitmap = torch.eq(ground_truth, label)

        all_results['intersection'][label - 1] += torch.logical_and(prediction_bitmap, target_bitmap).sum()
        all_results['union'][label - 1] += torch.logical_or(prediction_bitmap, target_bitmap).sum()

    return all_results


def calculate_intersection_over_union(all_results):
    ious = torch.div(all_results['intersection'], all_results['union'])
    return torch.nanmean(ious).item() * 100
