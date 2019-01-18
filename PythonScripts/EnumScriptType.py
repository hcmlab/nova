from enum import Enum

class ScriptType(Enum):
    CORRELATION = 'CORRELATION'
    EXPLAINER = 'EXPLAINER'
    OTHER = 'OTHER'

def getType():
    return ScriptType.OTHER.name
