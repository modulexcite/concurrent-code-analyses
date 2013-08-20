import collections

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
        
ExtractTopAPMAppsWithLatestUpdate()