import collections
from urllib2 import urlopen
import argparse
import json
import fnmatch
import os
import shutil
import sys
import shutil




def jsonParser():
    json = json.loads(open("/Users/semih/Desktop/ex.json").read())
    
    first_row = json
    
    def traverse(o):
        for key, value in o.iteritems():
            if isinstance(value, dict):
                for key, value in traverse(value):
                    yield (key, value)
            else:
                yield (key, value)
    
    all_keys = list((key for key, value in traverse(first_row)))
    
    print(",".join(all_keys))
    
    for row in json["rows"]:
        data = dict(traverse(row))
        values = (str(data[key]) for key in all_keys)
        print(",".join(values))
    
def pc():
    
    f=urlopen('http://www.sigsoft.org/fse20/orgCommittee.html')   
    
    lst=list()
    for line in f.readlines():
        if '<tr><td><a href="' in line:
            name = line.split('>')[3].split('<')[0]
            lst.append(name)
            print name
            
    f=urlopen('http://2014.icse-conferences.org/pc')   
     
    for line in f.readlines():
        if '</tr><tr><td><a href="' in line:
            name =  line.split('>')[4].split('<')[0]
            lastname = name.split(',')[0]
            for tmp in lst:
                if lastname in tmp:
                    print name + " -> " + tmp
                    

def move(root_src_dir, root_dst_dir):
    for src_dir, dirs, files in os.walk(root_src_dir):
        dst_dir = src_dir.replace(root_src_dir, root_dst_dir)
        if not os.path.exists(dst_dir):
            os.mkdir(dst_dir)
        for file_ in files:
            src_file = os.path.join(src_dir, file_)
            dst_file = os.path.join(dst_dir, file_)
            if os.path.exists(dst_file):
                os.remove(dst_file)
            shutil.move(src_file, dst_dir)
    shutil.rmtree(root_src_dir)          
              
def finalizeApps():
    f= open('summary.csv')
    
    dir= "/Volumes/Data/CodeCorpus/WPApps/" 
    sum=0
    sum2=0
    wp7apps=0
    wp8apps=0
    apps=0
    both=0
    i=0
    ii=0
    iii=0
    apm=0
    nonasync=0
    
    for line in f.readlines():
        tmp= line.split(',')
        name= tmp[0]
        if name == 'name':
            continue
            
        sloc= int(tmp[7])
        
        if sloc> 499:
            wp7= int(tmp[3])
            wp8= int(tmp[4])
            apps+=1            
            if wp7 > 0:
                wp7apps+=1
            if wp8 > 0:
                wp8apps+=1
            if wp7>0 and wp8>0:
                both+=1
                
            if int(tmp[12])>0:
                i+=1
                
            if int(tmp[13])>0:
                ii+=1  
            if int(tmp[9])>0:
                apm+=int(tmp[9])
                iii+=1
#            isApp= False
#            for root, dirnames, filenames in os.walk(dir+name):
#                for file in fnmatch.filter(filenames, 'App.xaml'):
#                    isApp=True
#            if not isApp:
#                print name  + " "+ tmp[9] + " "+tmp[12]
                
            if ",0,0,0,0,0,0,0,0,0,0,0" in line:
                nonasync+=1
    print "total apps:" +str(apps)            
    print "wp7 apps:"+ str(wp7apps)
    print "wp8 apps:"+str(wp8apps)
    print "both wp7 and wp8 apps:"+str(both)
    print "async/await apps:"+ str(i)
    print "async/await wp7 apps:" + str(ii)
    print "apm apps:"+ str(iii)
    print "apm sum:" + str(apm)
    print "async apps:" + str(apps-nonasync)

    sum=0
    sum2=0
    wp7apps=0
    wp8apps=0
    apps=0
    both=0
    i=0
    ii=0
    iii=0
    apm=0
    nonasync=0
    numsloc=0
    wp8async=0
    purewp7async=0
    bothAsyncAwait=0
    f= open('summary.csv')
    
    for line in f.readlines():
        tmp= line.split(',')
        name= tmp[0]
        if name == 'name':
            continue
            
        sloc= int(tmp[7])
        found=False
        for line2 in open('/Volumes/Data/Dropbox/Asyncifier-Results/CodeCorpusStatistics.csv').readlines():
            tmp2= line2.split(',')
            name2 = tmp2[0].replace('/','+')
            if name == name2:
                date= tmp2[1]
                found=True
                if ('2012' in date or '2013' in date):
                    if sloc> 499:
                        #print name
                        numsloc+=sloc
                        wp7= int(tmp[3])
                        wp8= int(tmp[4])
                        apps+=1            
                        if wp7 > 0:
                            wp7apps+=1
                        if wp8 > 0:
                            wp8apps+=1
                        if wp7>0 and wp8>0:
                            both+=1
                            if int(tmp[13])>0:
                                bothAsyncAwait+=1
                            
                        if int(tmp[12])>0:
                            i+=1
                            if wp7==0:
                                wp8async+=1
                            #print name
                            
                        if int(tmp[13])>0:
                            if wp8==0:
                                purewp7async+=1
                            ii+=1
                            
                        if int(tmp[9])>0:
                            print name
                            
                            apm+=int(tmp[9])
                            iii+=1
                        if ",0,0,0,0,0,0,0,0,0,0,0" in line:
                            nonasync+=1
        if not found:
            print name  
#            isApp= False
#            for root, dirnames, filenames in os.walk(dir+name):
#                for file in fnmatch.filter(filenames, 'App.xaml'):
#                    isApp=True
#            if not isApp:
#                print name  + " "+ tmp[9] + " "+tmp[12]
                
        #if ",0,0,0,0,0,0,0,0,0,0,0" in line:
          #  print name
    print '----------------'
    print "total apps:" +str(apps)            
    print "wp7 apps:"+ str(wp7apps)
    print "wp8 apps:"+str(wp8apps)
    print "both wp7 and wp8 apps:"+str(both)
    print "total async/await apps:"+ str(i)
    print "async/await wp7 apps:" + str(ii)
    print "async/await pure wp7 apps:" + str(purewp7async)
    print "async/await wp8 apps:" + str(wp8async)
    print "apm apps:"+ str(iii)
    print "apm sum:" + str(apm)
    print "async apps:" + str(apps-nonasync)
    print "total sloc:"+ str(numsloc)
    print bothAsyncAwait
#        for line2 in f2.readlines():
#            name2= line2.split(',')[0]
#            sloc2= line2.split(',')[7]
#            if name == name2:
#                if int(sloc)> int(sloc2):
#                    print "Artmis: "+ name + " - " +sloc + " >" + sloc2
#                elif int(sloc)< int(sloc2):
#                    print "Azalmis: "+ name + " - " +sloc + " <" + sloc2
    
def findAppXAML():
    dir= "/Volumes/Data/CodeCorpus/WPApps/" 
    i=0 
    for x in os.listdir(dir):
        if not os.path.isdir(dir+x):
            continue

        isApp= False
        for root, dirnames, filenames in os.walk(dir+x):
            for file in fnmatch.filter(filenames, 'App.xaml'):
                isApp=True
        if not isApp:
            i+=1
            print str(i)+ " "+ x  

def fixCodeplexUpdate():
    for line in open('CodeCorpusStatistics.csv').readlines():
        tmp= line.split(',')
        name = tmp[0]
        if 'name'== name:
            continue
        if not '/' in name:
            try:
                for line2 in urlopen("http://"+name+".codeplex.com/SourceControl/list/changesets").readlines():
                    if '<span class="smartDate' in line2:
                        updated = line2.split('title="')[1].split('"')[0]
                        break
                print name + ","+ updated+","+tmp[2]+","+tmp[3]+","+tmp[4]+","+tmp[5].replace('\n','')
            except Exception:
                print name + " *******"
                pass
        else:
            print line.replace('\n','') 
            
            
            
            
def CallbackType():
    f= open('/Volumes/Data/CodeCorpus/WPApps-Result/templog.txt')
    
    callbacktypeSet= {}
    for line in f.readlines():
        line = line.replace('\r\n','')
        callbacktypeSet[line]= callbacktypeSet.get(line,0)+1
    
    callbacktypeSet= collections.OrderedDict(sorted(callbacktypeSet.items()))
    
    for k, v in callbacktypeSet.iteritems():
        print k+","+str(v)
        
def ExtractTopAPMAppsWithLatestUpdate():
    path = '/Volumes/Data/Dropbox/Asyncifier-Results/CodeCorpusStatistics.csv'
    apmusagePath= '/Volumes/Data/Dropbox/Asyncifier-Results/08.19-APMDiagnosis-Results/summaryAPMDiagnosis.csv'
    apmFile= open('/Volumes/Data/Dropbox/Asyncifier-Results/APMApps.txt')
    
    for line in apmFile.readlines():
        appName= line.replace('\r\n','')
        
        for line2 in open(path).readlines():
            if appName.replace('+','/') == line2.split(',')[0]:
                for line3 in open(apmusagePath).readlines():
                    if appName == line3.split(',')[0]:
                        print line2.replace('\n','')+","+ line3.split(',')[8] + "," + line3.split(',')[5]  + "," + line3.split(',')[6]

def numAwaits():
    f= open('numawaits.txt')

    max=0
    sum=0
    i=0
    for line in f.readlines():
        if max<int(line):
            max= int(line)
        sum+=int(line)
        i+=1
    print max
    print float(sum)/float(i)
    
def unnecessaryAwaitsApps():
    f= open('unnecessaryawaits.txt')
    path = '/Volumes/Data/Dropbox/Asyncifier-Results/CodeCorpusStatistics.csv'

    myset={}
    for line in f.readlines():
        if 'UnnecessaryAwaits' in line:
            tag=line.split('\\')[3]
            myset[tag]= myset.get(tag,0)+1
    print len(myset)
    for app, v in myset.iteritems():
        for line2 in open(path).readlines():
            if app.replace('+','/') == line2.split(',')[0]:
                print str(v) + ","+line2.replace('\n','')
                
def blockingApps():
    f= open('temp.txt')
    path = '/Volumes/Data/Dropbox/Asyncifier-Results/CodeCorpusStatistics.csv'

    myset={}
    for line in f.readlines():
        if 'D:\\CodeCorpus\\' in line:
            tag=line.split('\\')[3]
            myset[tag]= myset.get(tag,0)+1
    print len(myset)
    for app, v in myset.iteritems():
        for line2 in open(path).readlines():
            if app.replace('+','/') == line2.split(',')[0]:
                print str(v) + ","+line2.replace('\n','')
                
def configureawaitApps():
    f= open('configureawait.txt')
    
    myset={}
    for line in f.readlines():
        if 'D:\\CodeCorpus\\' in line:
            tag=line.split('\\')[3]
            myset[tag]= myset.get(tag,0)+1
    print len(myset)
    for app, v in myset.iteritems():
        print str(app)            
def longrunningApps():
    f= open('temp.txt')
    path = '/Volumes/Data/Dropbox/Asyncifier-Results/CodeCorpusStatistics.csv'

    myset={}
    for line in f.readlines():
        if 'LONGRUNNING True' in line:
            tag=line.split('\\')[3]
            myset[tag]= myset.get(tag,0)+1
    print len(myset)
    for app, v in myset.iteritems():
        for line2 in open(path).readlines():
            if app.replace('+','/') == line2.split(',')[0]:
                print str(v) + ","+line2.replace('\n','')

                                    
                
def popularAPMandTAPCalls():
    f= open('asyncclassifieroriginallog.txt')
    myset={}
    for line in f.readlines():
        tmp = line.split(";")
        type= tmp[2]
        
        construct= tmp[5]+"."+tmp[6]
        
        if 'TAP' in type:
            myset[construct]= myset.get(construct,0)+1
            
    
    for app, v in myset.iteritems():
        print str(app) + "," + str(v)

def extractAPMApps():
    f= open('summary.csv')
    path = '/Volumes/Data/Dropbox/Asyncifier-Results/CodeCorpusStatistics.csv'

    for line in f.readlines():
        tmp = line.split(",")
        name= tmp[0]
        if name=="name":
            continue
        if int(tmp[9])>0:
            for line2 in open(path).readlines():
                if name.replace('+','/') == line2.split(',')[0]:
                    print tmp[9]+","+ tmp[8] +","+ line2.replace('\n','')
            

 
apmApps()           
#longrunningApps()                                       
#blockingApps()
#finalizeApps()
#configureawaitApps()