from git import *

import time

def isTaskIntroducedFromThreads(list):
	hasThread=False
	hasTask=False
	for line in list:
		if line.startswith('-') and "Thread" in line:
			hasThread=True
		elif line.startswith('+') and "Task" in line:
			hasTask=True
	return hasThread and hasTask

repo = Repo("/Users/semih/Desktop/ravendb/.git/")

prev= 0

#ITERATE ALL COMMITS
for commit in repo.iter_commits():
	if prev!= 0:
		#ITERATE ALL CHANGED FILES IN A COMMIT
		for d in commit.diff( prev.tree).iter_change_type('M'):
			if not (".cs" in d.a_blob.path):
				continue
			print time.strftime("%b %d %Y %H:%M:%S", time.gmtime(commit.authored_date)) + " " + d.a_blob.path
			
			print d
			#left = d.a_blob.data_stream.read().splitlines (True)
			#right = d.b_blob.data_stream.read().splitlines (True)
			#if isTaskIntroducedFromThreads(differ.compare(left,right)):
			#	print time.strftime("%b %d %Y %H:%M:%S", time.gmtime(prev.authored_date)) + " " + d.a_blob.path
	prev= commit
	
	
	
	

	
	
	



