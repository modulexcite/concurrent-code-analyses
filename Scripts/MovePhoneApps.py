import os
import shutil

def analyze():
	p= open("summaryPhoneApps.txt")
	    
	c=0
	for line in p.readlines():
		stats= line.split(",")
		if int(stats[16])>0:
			c+=1
			print str(c)+ " "+ stats[0]
			src= "/Volumes/Data/C#PROJECTS/PhoneApps/APMPhoneApps"+stats[0]
			
			dst= "/Volumes/Data/C#PROJECTS/PhoneApps/APMPhoneApps/"+stats[0]
			os.mkdir(dst)
			move(src,dst)


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

analyze()