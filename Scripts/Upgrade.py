import fnmatch
import os
import shutil
import sys

def upgrade():
    dir= "Z:\\CodeCorpus\\WPApps\\"
    analyze=False
    for x in os.listdir(dir):
        if not os.path.isdir(dir+x):
            continue
       
        isThereAnySln= False
        if analyze:
            for root, dirnames, filenames in os.walk(dir+x):
                for file in fnmatch.filter(filenames, '*.sln'):
                    isThereAnySln= True
                    path= os.path.join(root,file)
                    print path
                    os.system('devenv "'+ path +'" /upgrade')
            if not isThereAnySln:
                for root, dirnames, filenames in os.walk(dir+x):
                    for file in fnmatch.filter(filenames, '*.csproj'):
                        isThereAnySln= True
                        path= os.path.join(root,file)
                        print path
                        os.system('devenv "'+ path +'" /upgrade')


def executeAnalysis():
    for i in xrange(55):
        os.system('Z:\\asyncifier\\code\\Collector\\bin\\Release\\Collector.exe')

executeAnalysis()
