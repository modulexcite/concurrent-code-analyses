import fnmatch
import os
import shutil
import sys
import shutil

def main2():
    dir= "/Volumes/Data/CodeCorpus/WPApps/"   
    for x in os.listdir(dir):
        if not os.path.isdir(dir+x):
            continue
        deleted=False
        a=0
        for root, dirnames, filenames in os.walk(dir+x):
            for file in fnmatch.filter(dirnames, '_UpgradeReport_Files'):
                for x in os.listdir(root):
                        if x.startswith('Backup'):
                            print root+'/'+x
                            shutil.rmtree(root+'/'+x)
                            deleted=True



findReleases()