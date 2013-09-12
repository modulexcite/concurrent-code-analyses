import collections
from urllib2 import urlopen
import argparse
import json
import fnmatch
import os
import shutil
import sys
import shutil


def apmApps():
    
    f= open('apmApps.txt')
       
    for line in f.readlines():
        if '+' in line:
            appname = line.replace('+','/').replace('\n','')
            os.chdir('/Volumes/Data/CodeCorpus/Refactoring/');
            command = 'git clone git@github.com:'+appname +'.git /Volumes/Data/CodeCorpus/Refactoring/'+ line.replace('\n','')
            print command 
            os.system(command)
            os.chdir('/Volumes/Data/CodeCorpus/Refactoring/'+line.replace('\n',''))
            checkoutCommand = 'git checkout `git rev-list -n 1 --before="2013-08-10 13:37" master`'
            print checkoutCommand
            os.system(checkoutCommand)
            
            
            
def removeChanges():
    
    f= open('apmApps.txt')
       
    for line in f.readlines():
        if '+' in line:
            appname = line.replace('\r\n','')
            os.chdir('/Volumes/Data/CodeCorpus/Refactoring/'+appname)
            print appname
            os.system("git stash save")
            
removeChanges()