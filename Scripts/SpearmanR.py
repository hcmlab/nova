import os
import sys
dir_path = os.path.dirname(os.path.realpath(__file__))
print(dir_path)
sys.path.append(dir_path)
from EnumScriptType import ScriptType
from scipy.stats import spearmanr

def correlate(anno1, anno2):
    rho, p_value = spearmanr(anno1, anno2)
    return rho

def getType():
    return ScriptType.CORRELATION.name
