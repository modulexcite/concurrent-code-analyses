import os
from urllib2 import urlopen, URLError, HTTPError


dir= "/Volumes/Data/GithubMostWatched990Projects/"

def dlfile(url):
    # Open the url
    try:
		f = urlopen(url)
		
		project_dir= dir+ url.split("/")[3] + "-"+url.split("/")[4]
		
		if not os.path.exists(project_dir):
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
	address= "https://github.com/search?q=language%3AC%23&type=Repositories&p="
	website = urlopen(address) 
	
	list= []
	a=1
	letsGet=False


	for page in range(1,100):
		website = urlopen(address+str(page))
		for line in website.readlines():
			if letsGet==True:
				letsGet=False
				for line2 in urlopen("http://github.com"+line.split("\"")[1]).readlines():
					if "mini-icon-download" in line2:
						print str(a)
						project_url = "http://github.com"+ line2.split("\"")[1]
						list.append(project_url)
						a+=1
			if "mega-icon-public-" in line or "mega-icon-repo-forked" in line:
				letsGet= True
	c=1
	for url in list:
		print c
		dlfile(url)
		c+=1

main()
	

