import os
import sys
dir_path = os.path.dirname(os.path.realpath(__file__))
print(dir_path)
sys.path.append(dir_path)
from EnumScriptType import ScriptType
from scipy.stats import kendalltau

def correlate(anno1, anno2):
    tau, p_value = kendalltau(anno1, anno2)
    return tau

def getType():
    return ScriptType.CORRELATION.name
