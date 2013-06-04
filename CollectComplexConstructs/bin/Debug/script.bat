SET /a c=5
SET /a i=1050
SET path=Z:\C#PROJECTS\GithubMostWatched990Projects
:loop
IF %i%==1050 GOTO END
CollectComplexConstructs %c% %path%
SET /a i=%i%+ %c%
GOTO LOOP
:end


SET path=Z:\C#PROJECTS\CodeplexMostDownloaded1000Projects
SET /a i=0
:loop
IF %i%==1050 GOTO END
CollectComplexConstructs %c% %path%
SET /a i=%i%+ %c%
GOTO LOOP
:end


PAUSE