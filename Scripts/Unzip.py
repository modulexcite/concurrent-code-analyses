import zipfile
import os.path


dir= "/Volumes/Data/CodeCorpus/"
for x in os.listdir(dir):
    f= dir+x+"/archive.zip"
    if os.path.isfile(f):
        print x
        a=1
        zfile = zipfile.ZipFile(f)
        zfile.extractall(dir+x)
        zfile.close()
        os.remove(f)
    
    
        


