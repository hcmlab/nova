from scipy.stats import spearmanr

def correlate(anno1, anno2):
    r = spearmanr(anno1, anno2)
    return r.correlation
