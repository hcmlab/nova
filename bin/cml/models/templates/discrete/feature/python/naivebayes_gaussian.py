import sys
if not hasattr(sys, 'argv'):
    sys.argv  = ['']


from sklearn.naive_bayes import GaussianNB
import pickle

def train (X, Y):
    model = GaussianNB()
    print('train_x shape: {} | train_x[0] shape: {}'.format(X.shape, X[0].shape))
    model.fit(X, Y) 
    return model
    
def save (model, path):
    with open(path + '.model', 'wb') as f:
        pickle.dump(model, f)
        print('Model stored at: {}'.format(path))

def predict (model, X):
    Y = model.predict(X)
    return Y


def load (path):
    model = pickle.load( open( path, "rb" ))
    return model