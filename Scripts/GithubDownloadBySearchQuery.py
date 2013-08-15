import os
import cPickle
import codecs
from github3 import login, GitHub
from urllib2 import urlopen,URLError, HTTPError
from github import Github

dir= "/Volumes/Data/CodeCorpus/"

def dlfile(name,g):
    # Open the url
    
    try:
        project_dir = dir+ name.split("/")[0] + "+"+name.split("/")[1]
        if not os.path.exists(project_dir):
            repo = g.get_repo(name)
            url=repo.get_archive_link("zipball")
            f = urlopen(url)
            os.makedirs(project_dir)
            with open(project_dir+"/archive.zip", "wb") as local_file:
                local_file.write(f.read())
        else:
            print "already downloaded"
    except HTTPError, e:
        print "HTTP Error:", e.code, url
    except URLError, e:
        print "URL Error:", e.reason, url


def main():
            
        #repos=g.legacy_search_repos("windows phone",language="C#")
        #repos = g.legacy_search_repos()
        #url=repo.get_archive_link("zipball")

    g= login("semih.okur@gmail.com", "ker32nel")
    appList = dict()
    
    c=0
    for i in range(10):
        repos = g.search_repos("windows phone", language="C#", sort="stars", order="desc", start_page=i+1)
        for repo in repos:
            c+=1
            name= repo.owner + "/" + repo.name
            print str(c) +" " +name
            if name not in appList:
                appList[name]= str(repo.pushed_at) +"," + str(repo.created_at) +"," + str(repo.followers) + "," + str(repo.forks) + "," + repo.description.replace(',',' ')
    for i in range(10):
        repos = g.search_repos("windows phone", language="C#", sort="updated", order="desc", start_page=i+1)
        for repo in repos:
            c+=1
            name= repo.owner + "/" + repo.name
            print str(c) +" " +name
            if name not in appList:
                appList[name]= str(repo.pushed_at) +"," + str(repo.created_at) +"," + str(repo.followers) + "," + str(repo.forks)+ ","+ repo.description.replace(',',' ')
    for i in range(10):
        repos = g.search_repos("windows phone", language="C#", sort="forks", order="desc", start_page=i+1)
        for repo in repos:
            c+=1
            name= repo.owner + "/" + repo.name
            print str(c) +" " +name
            if name not in appList:
                appList[name]= str(repo.pushed_at) +"," + str(repo.created_at) +"," + str(repo.followers) + "," + str(repo.forks)+ ","+ repo.description.replace(',',' ')
    for i in range(10):
        repos = g.search_repos("windows phone", language="C#", sort="updated", order="asc", start_page=i+1)
        for repo in repos:
            c+=1
            name= repo.owner + "/" + repo.name
            print str(c) +" " +name
            if name not in appList:
                appList[name]= str(repo.pushed_at) +"," + str(repo.created_at) +"," + str(repo.followers) + ","  + str(repo.forks)+ ","+ repo.description.replace(',',' ') 
    print len(appList)
    
    

    f = open("githubapps.txt", "wb")
    f.write("name,updated,created,followers,forks\n")   
    for key, value in appList.iteritems():
        try:
            f.write(key + "," +value+ "\n")
        except Exception:
            commas= value.split(',')
            f.write(key+ "," + commas[0]+ "," + commas[1]+ "," + commas[2] + "," +commas[3] + ",\n")
        
    f.close()


def downloadUrls():
    c=1
    g = Github("semih.okur@gmail.com", "ker32nel")
    for line in open("githubapps.txt").readlines():
        
        name = line.split(",")[0]
        if name != "name":
            print str(c) + " "+ name
            c+=1

            dlfile(name,g)

downloadUrls()