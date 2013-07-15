import collections

def main():
    log  = "/Users/semih/Desktop/classifierlog.txt"
    table  = "/Users/semih/Desktop/table.txt"
    p= open(log)
    tableDict= {}
	
    for line in p.readlines():
        commas= line.split(",")
        if (len(commas)>4):
            line= commas[0] +","+commas[1]+commas[2]+","+commas[3]+","+commas[4]
        commas= line.split(",")
        element= commas[3].replace('\r\n', '')+","+commas[2]
        tableDict[element]= tableDict.get(element,0)+1
      
    p.close()
	
    tableDict= collections.OrderedDict(sorted(tableDict.items()))
    p = open(table,"w")
    for k, v in tableDict.iteritems():
        p.write(k+","+str(v)+"\n")
    
    p.close()
	
main()