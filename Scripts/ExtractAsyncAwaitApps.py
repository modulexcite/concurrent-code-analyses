import collections

def main():
	log  = "/Users/semih/Desktop/summaryPhoneApps.txt"
	output  = "/Users/semih/Desktop/output.txt"
	o = open(output,"w")
	p= open(log)
	tableDict= {}
	
	for line in p.readlines():
		commas= line.split(",")

		asyncawait= int(commas[25])+int(commas[26])+int(commas[27])

		element= commas[0];
		
		if(asyncawait>0):
			o.write(element+"\r\n")
      
	p.close()
	o.close()

main()