import os
from github import Github
from github import GithubException
from urllib2 import urlopen, URLError, HTTPError


	
def main():
	g = Github("semih.okur@gmail.com", "ker32nel")
	for dir in os.listdir("/Volumes/Data/Documents/C#PROJECTS/PhoneApps/"):
		if "+" in dir:
			try:
				name = dir.replace('+','/')
				repo = g.get_repo(name) 
				print name +"," + str(repo.pushed_at) + "," + str(repo.watchers) +"," + str(repo.forks)
			except Exception:
				print "notfound"
			#g.get_repo(dir.replace('+','/'))
	
main()
