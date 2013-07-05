import zipfile
import os.path

dir= "/Volumes/Data/CodeplexPhoneApps/"
for x in os.listdir(dir):
	f= dir+x+"/archive.zip"
	if os.path.isfile(f):		
		zfile = zipfile.ZipFile(f)
		zfile.extractall(dir+x)
		zfile.close()
		os.remove(f)
		


