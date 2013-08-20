import fnmatch
import os
import shutil
import sys
import shutil

def findReleases():
    dir= "/Volumes/Data/CodeCorpus/WPApps/"   
    for x in os.listdir(dir):
        if not os.path.isdir(dir+x):
            continue
        found=False
        a=0

        for root, dirnames, filenames in os.walk(dir+x):
            for dirname in fnmatch.filter(dirnames, 'Release'):
                if not('Bin' in root) and not('obj' in root) and not('bin' in root):

                    for root2, dirnames2, filenames2 in os.walk(root+"/"+dirname):
                        for file in fnmatch.filter(filenames2, '*.sln'):
                            found=True
                    if found:
                        print root+"/"+dirname
                