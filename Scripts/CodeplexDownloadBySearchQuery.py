import os
import re
from urllib2 import urlopen, URLError, HTTPError


dir= "/Volumes/Data/CodeCorpus/"

def dlfile(name):
    # Open the url
    try:
                
                
                project_dir= dir+ name
                
                if not os.path.exists(project_dir):
                        
                        
                        f = urlopen("http://" +name + ".codeplex.com/SourceControl/BrowseLatest");
                        id=0
                        for line in f.readlines():
                                if "\"changesetId\"" in line:
                                        id= line.split("\"changesetId\"")[1].split("\"")[1]
                        
                        if id==0:
                                f = open('undownloaded.txt', 'a')
                                f.write(name+'\n')
                                f.close()
                        else:
                                os.makedirs(project_dir)
                                downloadUrl= "http://download-codeplex.sec.s-msft.com/Download/SourceControlFileDownload.ashx?ProjectName="+ name +"&changeSetId="+ id
                                f= urlopen(downloadUrl)
                                with open(project_dir+"/archive.zip", "wb") as local_file:
                                        local_file.write(f.read())

                else:
                        print "already downloaded"
                
    #handle errors
    except HTTPError, e:
        print "HTTP Error:", e.code, url
    except URLError, e:
        print "URL Error:", e.reason, url



def main():
        address= "http://www.codeplex.com/site/search?query=windows%20phone&sortBy=Relevance&licenses=|&refinedSearch=true&size=100&page="
        f = open("codeplexapps.txt", "wb")
        
        list= []
        a=1
        letsGet=False
        
        nameIsFound= False
        name=""
        desc=""
        updated=""
        for page in range(0,11):
                website = urlopen(address+str(page))
                for line in website.readlines():
                        if "<h3>" in line:
                            name= line.split("\"")[1].split("/")[2].split(".")[0]
                            nameIsFound= True
                            
                        if "<p>" in line and nameIsFound:
                            desc= line.replace('<p>','').replace('</p>','').replace('"','').replace("<span class='HighlightItem'>",'').replace('</span>', '').replace(',',' ')
                            
                        if "<p class" in line and nameIsFound:
                            temp= line.split('class="green">')
                            views= temp[1].split('<')[0]
                            downloads= temp[2].split('<')[0]
                            startDate = line.split('title="')[1].split('"')[0]
                            
                            try:
                                for line2 in urlopen("http://"+name+".codeplex.com/SourceControl/list/changesets").readlines():
                                    if '<span class="smartDate' in line2:
                                        updated = line2.split('title="')[1].split('"')[0]
                                f.write(name + "," + updated + "," + startDate + "," + views + "," + downloads + "," +desc)
                                print str(a) + " " + name
                            except Exception:
                                print str(a) + " " + name + " *******"
                                pass
                            nameIsFound=False
                            a+=1
        
        f.close()
        i=1
        #for url in list:
        #       print str(i)+ "-downloading " + url
        #       dlfile(url)
        #       i=i+1


def read():
    f = open("codeplexapps.txt", "r")
    i=1
    for line in f.readlines():
        name = line.split(",")[0]
        if name != "name":
            print str(i) + " "+ name
            dlfile(name)
            i+=1
read()