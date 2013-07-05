import os
from github import Github
from urllib2 import urlopen, URLError, HTTPError

dir= "/Volumes/Data/GithubPhoneApps/"

def dlfile(url):
    # Open the url
    try:
		project_dir= dir+ url.split("/")[3] + "+"+url.split("/")[4]
		if not os.path.exists(project_dir):
			f = urlopen(url)
			print "downloading " + url
			os.makedirs(project_dir)
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
	g = Github("semih.okur@gmail.com", "ker32nel")
	repos= g.legacy_search_repos("windows phone NOT library",language="C#")
	
	f= open("willBeDownloaded.txt","wb")
	c=0
	for repo in repos:
		c+=1
		url=repo.get_archive_link("zipball")
		print str(c)+" :"+ url
		f.write(url+"\n")
		#dlfile(url)
	f.close()
	
def downloadUrls(textFile):
	c=0
	for url in open(textFile).readlines():
		c+=1
		print str(c) + " "+ url
		dlfile(url) 
	
downloadUrls("willBeDownloaded.txt")

	

