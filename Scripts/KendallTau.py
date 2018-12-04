from scipy.stats import kendalltau

def correlate(anno1, anno2):
    tau, p_value = kendalltau(anno1, anno2)
    return tau