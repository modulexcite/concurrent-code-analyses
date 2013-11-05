import collections
from urllib2 import urlopen
import argparse
import json
import fnmatch
import os
import shutil
import sys
import shutil
import difflib
import ghdiff

import StringIO

def copyfolder(src, dst):
    try:
        shutil.copytree(src, dst)
    except OSError as exc: # python >2.5
        if exc.errno == errno.ENOTDIR:
            shutil.copy(src, dst)
        else: raise
def apmApps():
    
    f= open('apmApps.txt')
       
    for line in f.readlines():
#        if '+' in line:
#            appname = line.replace('+','/').replace('\n','')
#            os.chdir('/Volumes/Data/CodeCorpus/Refactoring/');
#            command = 'git clone git@github.com:'+appname +'.git /Volumes/Data/CodeCorpus/Refactoring/'+ line.replace('\n','')
#            print command 
#            os.system(command)
#            os.chdir('/Volumes/Data/CodeCorpus/Refactoring/'+line.replace('\n',''))
#            checkoutCommand = 'git checkout `git rev-list -n 1 --before="2013-08-10 13:37" master`'
#            print checkoutCommand
#            os.system(checkoutCommand)
        if not '+' in line:
            appname = line.replace('\r\n','')
            print appname
            src= '/Volumes/Data/CodeCorpus/WPApps/'+ appname
            dst= '/Volumes/Data/CodeCorpus/Refactoring/'+appname
            copyfolder(src,dst)

                
            
def removeChanges():
    f= open('apmApps.txt')
    for line in f.readlines():
        if '+' in line:
            appname = line.replace('\r\n','')
            os.chdir('/Volumes/Data/CodeCorpus/Refactoring/'+appname)
            print appname
            os.system("git stash save")

def createFilesForDiffs():
    f= open('success.txt')
    
    oldWrite=False
    newWrite=False
    index=0
    oldFile = 0
    newFile = 0
    for line in f.readlines():

        if '*************************************************************************************************' in line:
            oldWrite=False
            oldFile.close()
            newWrite= True
            newFile = open('refactoring/'+str(index)+"_new.txt" , 'w+')
        elif '=================================================================================================' in line:
            newWrite= False
            newFile.close()
        elif newWrite:
            newFile.write(line)
        elif oldWrite:
            oldFile.write(line)
        elif 'D:\CodeCorpus\Refactoring' in line:
            oldWrite=True
            index+=1
            oldFile= open('refactoring/'+str(index)+"_old.txt" , 'w+')


def calculateDiffs():
    plusTotal=0
    minusTotal=0
    for i in range(398):
        index= i+1
        file1= open('refactoring/'+str(index)+"_old.txt")
        file2= open('refactoring/'+str(index)+"_new.txt")
        d = difflib.Differ()
        diffs = d.compare(file1.readlines(), file2.readlines())
        plus=0
        minus=0
        for line in StringIO.StringIO('\n'.join(diffs)).readlines():
            if line.startswith('+'):
                plus+=1
            if line.startswith('-'):
                minus+=1
        if minus > 20:
            minusTotal+=minus
            plusTotal+=plus
    print plusTotal
    print minusTotal
    
def preconditions():
    file1= open('refactoring/'+str(index)+"_old.txt")

    #if "Refactoring_BatchTool.APMRefactoring - Precondition failed:" in line:
        
calculateDiffs()