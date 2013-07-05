import os
import mechanize
import re
from urllib2 import urlopen, URLError, HTTPError


dir= "/Volumes/Data/CodeplexMostDownloaded1000Projects/"

def dlfile(url):
    # Open the url
    try:
		
		name= url.split("/")[2].split(".")[0]
		project_dir= dir+ name
		
		if not os.path.exists(project_dir):
			os.makedirs(project_dir)
			
			f = urlopen(url+ "/SourceControl/BrowseLatest");
			id=0
			for line in f.readlines():
				if "\"changesetId\"" in line:
					id= line.split("\"changesetId\"")[1].split("\"")[1]
			
			if id==0:
				f = open('undownloaded.txt', 'a')
				f.write(name+'\n')
				f.close()
			else:
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
	address= "http://www.codeplex.com/site/search?query=&sortBy=DownloadCount&licenses=|&refinedSearch=true&size=100&page="
	website = urlopen(address) 
	
	list= []
	a=1
	letsGet=False
	
	for page in range(0,10):
		website = urlopen(address+str(page))
		for line in website.readlines():
			if "<h3>" in line:
				project_url= line.split("\"")[1]
				list.append(project_url)
	i=1
	for url in list:
		print str(i)+ "-downloading " + url
		dlfile(url)
		i=i+1


main()