import collections

def main():
	file= "/Users/semih/Desktop/syncclassifierlog.txt"
	
	f= open(file)
	c=0
	for line in f.readlines():
		elements= line.split(";")
		if len(elements)>8:
			print "error"
		if "Invoke" in elements[6]:
			c+=1
	print c	
main()